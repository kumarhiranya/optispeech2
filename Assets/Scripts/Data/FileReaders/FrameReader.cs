using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System.IO;
using System.Collections.Generic;
using System;
using Optispeech.Sensors;
using Optispeech.Documentation;
using System.Threading;

namespace Optispeech.Data.FileReaders {

    /// <summary>
    /// This class reads sweep data from a file
    /// </summary>
    /// <remarks>
    /// While originally written for the FileDataSource this code can also be used to
    /// read sweep data elsewhere, such as for the custom motion target type
    /// </remarks>
    public class FrameReader : MonoBehaviour {

        /// <summary>
        /// Delegate used for creating utility functions for common patterns across FrameReaders
        /// </summary>
        /// <param name="frame">A string to read a data frame from</param>
        /// <returns>The data frame containing the data from <paramref name="frame"/></returns>
        public delegate DataFrame ReadFrameFromString(string frame);

        /// <summary>
        /// Abstract class for reading sweep data in different file formats
        /// </summary>
        public abstract class FileReader {
            /// <summary>
            /// This function will read a file and call a delegate on every data frame parsed from the file, and eventually
            /// call the finish delegate when done working, returning a bool representing whether or not the file was
            /// successfully read
            /// </summary>
            /// <param name="file">The file to read data frames from</param>
            /// <param name="addDataFrame">A delegate to call with any data frames read from the file</param>
            /// <param name="finish">A delegate to call when done working, returning true if successful</param>
            public abstract void ReadFrames(StreamReader file, UnityAction<DataFrame> addDataFrame, UnityAction<bool> finish);

            /// <summary>
            /// When reading files, the sensor configurations are generally stored in the file, or can be assumed
            /// based on the file format. This function should return the sensor configurations present in the
            /// passed data frame
            /// </summary>
            /// <param name="dataFrame">The data frame to find the sensor configurations for</param>
            /// <returns>The sensor configurations present or assumed from the passed data frame</returns>
            public abstract SensorConfiguration[] GetSensorConfigurations(DataFrame dataFrame);

            /// <summary>
            /// Takes a filename and returns a file that will hold the corresponding audio for this file, if it exists
            /// Note that it doesn't need to check if the file exists, just return the filename it would be at if it does exist
            /// </summary> 
            public virtual string GetAudioFile(string filename) {
                return Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".wav");
            }
        }

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static FrameReader Instance = default;

        [HideInDocumentation]
        void Awake() {
            if (Instance == null || Instance == this) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Creates a coroutine that reads the specific file and then calls the appropriate callback.
        /// This function will determine what kind of file it is and attempt to read it.
        /// </summary>
        /// <param name="filename">The filename of the file to load</param>
        /// <param name="loadTargets">Whether or not to load any targets present in the file</param>
        /// <param name="onSuccess">Callback that's called when the file is successfully read, with the FileReader used and the frames read</param>
        /// <param name="onFailure">Callback that's called when the file is not successfully read</param>
        /// <returns>Coroutine</returns>
        public IEnumerator ReadFile(string filename, bool loadTargets = true, UnityAction<FileReader, List<DataFrame>> onSuccess = default, UnityAction onFailure = default) {
            if (filename == "") {
                onFailure.Invoke();
                yield break;
            }

            StreamReader file = new StreamReader(filename);
            if (file.EndOfStream) {
                onFailure.Invoke();
                yield break;
            }

            FileReader reader = GetFileReader(file, loadTargets);
            if (reader == null) {
                onFailure.Invoke();
                yield break;
            }

            // The reader will be affecting these
            bool active = true;
            bool successful = false;
            List<DataFrame> frames = new List<DataFrame>();

            // Create the delegates to manipulate the above values
            UnityAction<bool> finish = (value) => { successful = value; active = false; };
            UnityAction<DataFrame> addDataFrame = (value) => { frames.Add(value); };

            Thread thread = new Thread(() => reader.ReadFrames(file, addDataFrame, finish));
            thread.Start();

            // We wait until active is false because handling it in finish would be in the FileReader's thread instead of Unity's main thread
            while (active) {
                // Wait until next update
                yield return null;
            }

            file.Close();

            if (successful)
                onSuccess.Invoke(reader, frames);
            else
                onFailure.Invoke();
        }

        /// <summary>
        /// Takes a file and determines which kind of file it has and returns the appropriate <see cref="FileReader"/> implementation
        /// </summary>
        /// <param name="file">The file to be read</param>
        /// <param name="loadTargets">Whether or not to add any targets present in the file</param>
        /// <returns>The appropriate FileReader implementation for this type of file, or null if none found</returns>
        // TODO find a to avoid editing this function whenever adding a new FileReader implementation?
        private FileReader GetFileReader(StreamReader file, bool loadTargets) {
            // Read header row to determine what kind of input file it is
            string headerRow = file.ReadLine();
            // If the header row has a cell called "Target", then it is an export from the old version of optispeech
            // int firstTargetIndex = Array.IndexOf(headerRow.Split('\t'), "Target Number");
            bool containsTarget = headerRow.Contains("Target");
            FileReader reader;
            if (containsTarget) {
                Debug.Log("Detected Legacy file, using LegacyFileReader. " + containsTarget.ToString());
                reader = new LegacyFileReader(headerRow, loadTargets);
            } else if (headerRow.StartsWith("OptiSpeech 2")) {
                Debug.Log("Detected Optispeech 2 file, using OptiSpeechFileReader. " + containsTarget.ToString());
                reader = new OptiSpeechFileReader(file, loadTargets);
            } else {
                Debug.Log("Detected Wavefront file, using WaveFrontFileReader. " + containsTarget.ToString());
                reader = new WaveFrontFileReader();
            }

            return reader;
        }

        /// <summary>
        /// Utility function for handling errors reading from the file this will tick the number of failures and
        /// return a bool over whether to continue processing that file
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="numFailures">The number of failures this file has</param>
        /// <returns>Whether or not to cancel reading the file</returns>
        public static bool HandleFailure(string message, ref int numFailures) {
            numFailures++;
            Debug.Log("[FileDataSource] " + message);
            if (numFailures > 3) {
                Debug.Log("[FileDataSource] Too many errors! Ignoring rest of file...");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Utility function for frame readers that want to read a single data frame from each line until the end of the file
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="addDataFrame">The addDataFrame delegate given to the frame reader</param>
        /// <param name="readFrame">Delegate to handle reading a data frame from a single line of the file</param>
        /// <returns>Whether or not the file was successfully read</returns>
        public static bool ReadSingleLineFrames(StreamReader file, UnityAction<DataFrame> addDataFrame, ReadFrameFromString readFrame) {
            int numFailures = 0;
            while (!file.EndOfStream) {
                string frame = file.ReadLine();

                try {
                    DataFrame dataFrame = readFrame(frame);
                    addDataFrame(dataFrame);
                    numFailures = 0;
                } catch (Exception e) {
                    if (HandleFailure("Failed to parse frame \"" + frame + "\". Error: " + e.Message + "\n" + e.StackTrace, ref numFailures)) continue;
                    else return false;
                }
            }

            return true;
        }
    }
}
