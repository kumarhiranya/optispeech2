using UnityEngine;
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;
using Optispeech.Data;
using Optispeech.Targets;
using Optispeech.Profiles;
using Optispeech.Sensors;
using System.Linq;
using Optispeech.Documentation;

namespace Optispeech.Sweeps {

    /// <summary>
    /// Handles writing data from sweeps
    /// </summary>
    public class FileWritingManager : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static FileWritingManager Instance = default;

        /// <summary>
        /// The sweeps panel that starts and stops writing data to file, as well as handles
        /// what to write and where to write it to
        /// </summary>
        [SerializeField]
        private SweepsPanel sweepsPanel = default;

        // Both this and sweepsPanel have several public variables because they are very co-dependent
        // I just didn't want this code mixed with the UI code
        /// <summary>
        /// Flag representing whether or not a sweep is being recorded
        /// </summary>
        [HideInInspector]
        public bool recordingSweep = false;
        /// <summary>
        /// When a sweep ends this is used to track how many frames are remaining in the queue and still
        /// need to be processed before the writing to file can officially stop
        /// </summary>
        [HideInInspector]
        public int remainingFrames = -1;
        /// <summary>
        /// How long has elapsed since the start of the sweep
        /// </summary>
        [HideInInspector]
        public TimeSpan elapsedTime;
        /// <summary>
        /// Event that fires whenever a sweep starts
        /// </summary>
        [HideInInspector]
        public UnityEvent onSweepStart = new UnityEvent();
        /// <summary>
        /// Event that fires whenever a sweep ends
        /// </summary>
        [HideInInspector]
        public UnityEvent onSweepEnd = new UnityEvent();

        /// <summary>
        /// Writer that streams raw data to file
        /// </summary>
        private StreamWriter raw;
        /// <summary>
        /// Writer that streams transformed data to file
        /// </summary>
        private StreamWriter transformed;
        /// <summary>
        /// Writer that streams transformed data, but without post-offsets, to file
        /// </summary>
        private StreamWriter transformedWithoutOffsets;
        /// <summary>
        /// Audioclip that stores any recorded audio during the sweep
        /// </summary>
        new private AudioClip audio;
        /// <summary>
        /// The base filename to write sweep data to
        /// </summary>
        private string filename;

        /// <summary>
        /// Flag used to track when a sweep ends to handle the sweep ending on the main thread
        /// </summary>
        private bool finishing = false;

        [HideInDocumentation]
        private void Awake() {
            if (Instance == null || Instance == this) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }
        }

        [HideInDocumentation]
        private void Update() {
            if (finishing) {
                finishing = false;

                // Update sweeps panel
                sweepsPanel.DisplayFinishing();
            } else if (recordingSweep) {
                sweepsPanel.UpdateDuration(elapsedTime);
            } else if (remainingFrames == 0) {
                remainingFrames = -1;

                // Guaranteed to happen when sweepsPanel.DisplayFinishing() has already finished,
                // with drawback of only starting after the frame queue is empty
                // Ideally this would be on another frame due to its long duration, but you can't access our
                // audio component from another thread
                if (ProfileManager.Instance.ActiveProfile.saveAudio) {
                    Microphone.End(null);
                    // Don't trim silence because we want the audio to sync up with the recorded sweep data
                    SavWav.Save(Path.Combine(ProfileManager.Instance.ActiveProfile.sweepFolder, filename + "_audio.wav"), audio);
                }

                // Reset interface
                sweepsPanel.ResetDisplay();
            }
        }
        
        /// <summary>
        /// Handles starting and stopping a sweep. Sweeps can be toggled to start from the main thread only, but can be stopped from any
        /// </summary>
        public void ToggleSweep() {
            if (recordingSweep) {
                // This may be called to stop the recording from the Record thread, so be aware you can't access many Unity things
                // Instead we set a flag and handle it in the next Update call
                finishing = true;

                // Stop the sweep
                // We tell the thread to finish the rest of the items currently in the queue, and then stop
                remainingFrames = DataSourceManager.Instance.dataSourceReader.dataQueue.Count;
                recordingSweep = false;
                DataSourceManager.Instance.dataSourceReader.StopSweep();
            } else {
                // Update sweeps panel
                sweepsPanel.DisplayRecording();

                // Ensure the output directory exists
                if (!Directory.Exists(ProfileManager.Instance.ActiveProfile.sweepFolder))
                    Directory.CreateDirectory(ProfileManager.Instance.ActiveProfile.sweepFolder);

                // Find appropriate filename by taking the one defined in sweepsPanel and, if it already exists,
                // adding a number to the end until its unique
                filename = ProfileManager.Instance.ActiveProfile.sweepName;
                if (filename == "") filename = "sweep";
                int copy = 0;
                while (Directory.GetFiles(ProfileManager.Instance.ActiveProfile.sweepFolder, $"{filename}_*").Length > 0)
                    filename = ProfileManager.Instance.ActiveProfile.sweepName + (++copy);

                // Start file streams with headers
                // Note we start audio after starting the thread,
                // just to help ensure they're as in-sync as possible
                if (ProfileManager.Instance.ActiveProfile.saveRaw) StartRaw();
                if (ProfileManager.Instance.ActiveProfile.saveTransformed) StartTransformed();
                if (ProfileManager.Instance.ActiveProfile.saveTransformedWithoutOffsets) StartTransformedWithoutOffsets();

                // Empty data queue
                DataFrame ignored;
                while (!DataSourceManager.Instance.dataSourceReader.dataQueue.IsEmpty)
                    DataSourceManager.Instance.dataSourceReader.dataQueue.TryDequeue(out ignored);

                // Start sweep on data source
                DataSourceManager.Instance.dataSourceReader.StartSweep(ProfileManager.Instance.ActiveProfile.sweepFolder, filename);

                // Start recording
                remainingFrames = -1;
                recordingSweep = true;
                elapsedTime = new TimeSpan(0);
                new Thread(new ThreadStart(Record)).Start();
                onSweepStart.Invoke();

                if (ProfileManager.Instance.ActiveProfile.saveAudio) StartAudio();
            }
        }

        [HideInDocumentation]
        private void OnDisable() {
            if (recordingSweep) ToggleSweep();
        }

        [HideInDocumentation]
        private void OnApplicationQuit() {
            if (recordingSweep) ToggleSweep();
        }

        /// <summary>
        /// Creates <see cref="StreamWriter"/> and writes header for the raw sweep data
        /// </summary>
        private void StartRaw() {
            string filepath = Path.Combine(ProfileManager.Instance.ActiveProfile.sweepFolder, filename + "_raw.tsv");
            raw = new StreamWriter(filepath);
            string header = "OptiSpeech 2\t1.0.0\n";
            header += "Source\t" + DataSourceManager.Instance.dataSourceName + "\n";
            if (ProfileManager.Instance.ActiveProfile.saveAudio)
                header += "Audio\t" + MakeRelativePath(filepath, Path.Combine(ProfileManager.Instance.ActiveProfile.sweepFolder, filename + "_audio.wav")) + "\n";
            header += "-- Source Configurations\n";
            // Next the header contains the configurations for each sensor
            header += "Sensor ID\tType\tPre-Offset X\tPre-Offset Y\tPre-Offset Z\tPost-Offset X\tPost-Offset Y\tPost-Offset Z\n";
            foreach (SensorConfiguration sensorConfig in SensorsManager.Instance.sensors) {
                header += sensorConfig.id + "\t" +
                    sensorConfig.type + "\t" +
                    sensorConfig.preOffset.x + "\t" + sensorConfig.preOffset.y + "\t" + sensorConfig.preOffset.z + "\t" +
                    sensorConfig.postOffset.x + "\t" + sensorConfig.postOffset.y + "\t" + sensorConfig.postOffset.z + "\n";
            }
            header += "-- Targets\n";
            header += "Target ID\tTarget Type\tRadius\tType-specific values...\n";
            foreach (KeyValuePair<string, TargetController> kvp in TargetsManager.Instance.targets) {
                // Different types of targets have different data to store so we defer to the TargetController implementations
                header += kvp.Value.ToString() + "\n";
            }
            header += "-- Sensor Data\n";
            // Finally we have a header of each frame's timestamp and raw sensor data
            // Note we don't include targets data per-frame because it can be calculated from the target configuration data above
            header += "Timestamp\t";
            for (int i = 0; i < SensorsManager.Instance.sensors.Length; i++)
                header += "Sensor ID\tSensor Status\tSensor X\tSensor Y\tSensor Z\tQ0\tQx\tQy\tQz\t";
            header += "\n";
            raw.Write(header);
        }

        /// <summary>
        /// Creates <see cref="StreamWriter"/> and writes header for the transformed sweep data
        /// </summary>
        private void StartTransformed() {
            transformed = new StreamWriter(Path.Combine(ProfileManager.Instance.ActiveProfile.sweepFolder, filename + "_transformed.tsv"));
            // Transformed is going to be a tsv formatted to be as close to NDI Wave's data format as possible
            // Targets will similarly have their position stored, and their status will be a 0 or 1 depending on whether the tongue tip is in the target area or not
            // Their IDs will start at 0 and increment for each target, and the last value will be a dummy tab just to make it take up the same number of values as a sensor
            // (technically a single radius value would also be fine, but we'll split it up along all 3 values once again, just to make it use the same number of values)
            // Since the data format we're using doesn't allow us to specify number of sensors, etc. you'll have to parse the header values to get that information
            // Unlike the Wavefront data files, we don't add a unit to the X,Y,Z components of each sensor. That's because its in Unity-units, which don't have a real-life analogue
            string header = "AudioTime[s]\tPlaceholder\tPlaceholder";
            for (int i = 0; i < 9 + SensorsManager.Instance.otherSensorConfigs.Count(); i++)
                header += "\tSensor ID\tSensor Status\tSensor X\tSensor Y\tSensor Z\tQ0\tQx\tQy\tQz";
            foreach (KeyValuePair<string, TargetController> kvp in TargetsManager.Instance.targets)
                header += "\tTarget ID\tTarget X\tTarget Y\tTarget Z\tTongue Tip Distance\tPlaceholder\tPlaceholder\tPlaceholder\tPlaceholder";
            transformed.WriteLine(header);
        }

        /// <summary>
        /// Creates <see cref="StreamWriter"/> and writes header for the transformed sweep data without offsets
        /// </summary>
        private void StartTransformedWithoutOffsets() {
            transformedWithoutOffsets = new StreamWriter(Path.Combine(ProfileManager.Instance.ActiveProfile.sweepFolder, filename + "_transformed_without_offsets.tsv"));
            // This header is the same as for Transformed. The only difference between these two files is whether or not they include the sensor's post-offset
            string header = "AudioTime[s]\tPlaceholder\tPlaceholder";
            for (int i = 0; i < 9 + SensorsManager.Instance.otherSensorConfigs.Count(); i++)
                header += "\tSensor ID\tSensor Status\tSensor X\tSensor Y\tSensor Z\tQ0\tQx\tQy\tQz";
            foreach (KeyValuePair<string, TargetController> kvp in TargetsManager.Instance.targets)
                header += "\tTarget ID\tTarget X\tTarget Y\tTarget Z\tTongue Tip Distance\tPlaceholder\tPlaceholder\tPlaceholder\tPlaceholder";
            transformedWithoutOffsets.WriteLine(header);
        }

        /// <summary>
        /// Starts recording microphone audio to <see cref="audio"/>
        /// </summary>
        private void StartAudio() {
            // TODO consider tracking start/end time and trimming the audio file to be as close as possible to the first and last DataFrame stored
            // I'm not sure that's necessary without more testing. It would only be a problem with a significant lag coming from a DataSourceReader
            // We set the length to 600 (10 minutes). The max is an hour, but the resultant clip is always the length you put here, and trimming can take awhile with larger numbers
            // And we can't even do it in the background thread because audio.clip's samples can only be accessed from the main thread
            audio = Microphone.Start(null, false, 600, 44100);
        }

        /// <summary>
        /// Function that runs in new thread to constantly write frames while a sweep is ongoing
        /// </summary>
        private void Record() {
            // Get the first item to use for timing purposes
            DataFrame firstFrame;
            // Wait until there's a new frame available
            // We also keep checking if the remaining frames is 0, in case the sweep is ended between two frames being added
            while (DataSourceManager.Instance.dataSourceReader.dataQueue.IsEmpty && remainingFrames != 0)
                Thread.Sleep(sweepsPanel.pollFrequency);

            // If the while loop stopped due to there being no more frames, exit early
            if (remainingFrames == 0)
                return;

            // Obtain the first data frame
            DataSourceManager.Instance.dataSourceReader.dataQueue.TryDequeue(out firstFrame);

            // Write to file
            WriteFrameToFile(firstFrame);

            // Start reading rest of the frames
            while (remainingFrames != 0) {
                // Wait until there's a new frame available
                // We also keep checking if the remaining frames is 0, in case the sweep is ended between two frames being added
                while (DataSourceManager.Instance.dataSourceReader.dataQueue.IsEmpty && remainingFrames != 0)
                    Thread.Sleep(sweepsPanel.pollFrequency);

                // If the while loop stopped due to there being no more frames, exit early
                if (remainingFrames == 0)
                    break;

                // Obtain the next data frame
                DataFrame frame;
                DataSourceManager.Instance.dataSourceReader.dataQueue.TryDequeue(out frame);

                // Write to file
                WriteFrameToFile(frame);

                // Update elapsed time
                elapsedTime = new TimeSpan((frame.timestamp - firstFrame.timestamp) * 10000);

                // Check if its time to auto-end the sweep
                if (ProfileManager.Instance.ActiveProfile.autoStopDuration != -1 && elapsedTime.Seconds >= ProfileManager.Instance.ActiveProfile.autoStopDuration) {
                    ToggleSweep();
                    remainingFrames = 0;
                }

                // If the sweep is over and we're finishing remaining items in the queue,
                // decrement how many frames we have left
                if (remainingFrames > 0)
                    remainingFrames--;
            }

            // Close files
            if (ProfileManager.Instance.ActiveProfile.saveRaw) {
                raw.Close();
                raw.Dispose();
            }
            if (ProfileManager.Instance.ActiveProfile.saveTransformed) {
                transformed.Close();
                transformed.Dispose();
            }
            if (ProfileManager.Instance.ActiveProfile.saveTransformedWithoutOffsets) {
                transformedWithoutOffsets.Close();
                transformedWithoutOffsets.Dispose();
            }

            onSweepEnd.Invoke();
        }

        /// <summary>
        /// Writes a frame to file
        /// </summary>
        /// <param name="frame">The data frame to write to file</param>
        private void WriteFrameToFile(DataFrame frame) {
            if (ProfileManager.Instance.ActiveProfile.saveRaw) {
                string row = frame.timestamp.ToString();
                foreach (SensorData sensorData in frame.sensorData) {
                    if (sensorData.status != SensorStatus.OK) row += "\t" + sensorData.id + "\t" + sensorData.status + "\t\t\t\t\t\t\t";
                    else row += "\t" + sensorData.id + "\t" +
                        sensorData.status + "\t" +
                        sensorData.position.x + "\t" + sensorData.position.y + "\t" + sensorData.position.z + "\t" +
                        sensorData.rotation.w + "\t" + sensorData.rotation.x + "\t" + sensorData.rotation.y + "\t" + sensorData.rotation.z;
                }
                raw.WriteLine(row);
            }
            if (ProfileManager.Instance.ActiveProfile.saveTransformed) {
                string row = frame.timestamp + "\t0\t0";
                TransformedData data = frame.transformedData;
                row += GetSensorString(data.forehead);
                row += GetSensorString(data.jaw);
                row += GetSensorString(data.leftEar);
                row += GetSensorString(data.rightEar);
                row += GetSensorString(data.tongueTip);
                row += GetSensorString(data.tongueDorsum);
                row += GetSensorString(data.tongueRight);
                row += GetSensorString(data.tongueLeft);
                row += GetSensorString(data.tongueBack);
                foreach (SensorData sensor in data.otherSensors)
                    row += GetSensorString(sensor);
                foreach (KeyValuePair<string, Vector3> kvp in frame.targetPositions)
                    row += "\t" + kvp.Key + "\t" + kvp.Value.x + "\t" + kvp.Value.y + "\t" + kvp.Value.z + "\t" +
                        (data.tongueTip.HasValue ? Vector3.Distance(kvp.Value, data.tongueTip.Value.position + data.tongueTip.Value.postOffset) : float.NaN) + "\t\t\t\t";
                transformed.WriteLine(row);
            }
            if (ProfileManager.Instance.ActiveProfile.saveTransformedWithoutOffsets) {
                string row = frame.timestamp + "\t0\t0";
                TransformedData data = frame.transformedData;
                row += GetSensorString(data.forehead, false);
                row += GetSensorString(data.jaw, false);
                row += GetSensorString(data.leftEar, false);
                row += GetSensorString(data.rightEar, false);
                row += GetSensorString(data.tongueTip, false);
                row += GetSensorString(data.tongueDorsum, false);
                row += GetSensorString(data.tongueRight, false);
                row += GetSensorString(data.tongueLeft, false);
                row += GetSensorString(data.tongueBack, false);
                foreach (SensorData sensor in data.otherSensors)
                    row += GetSensorString(sensor, false);
                foreach (KeyValuePair<string, Vector3> kvp in frame.targetPositions)
                    row += "\t" + kvp.Key + "\t" + kvp.Value.x + "\t" + kvp.Value.y + "\t" + kvp.Value.z + "\t" +
                        (data.tongueTip.HasValue ? Vector3.Distance(kvp.Value, data.tongueTip.Value.position) : float.NaN) + "\t\t\t\t";
                transformedWithoutOffsets.WriteLine(row);
            }
        }

        /// <summary>
        /// Creates a config string of sensor data in a single frame
        /// </summary>
        /// <param name="sensorData">The sensor data to create a string out of</param>
        /// <param name="includePostOffsets">Whether or not to include post-offsets</param>
        /// <returns>A string representation of this sensor's data this frame</returns>
        private string GetSensorString(SensorData? sensorData, bool includePostOffsets = true) {
            if (!sensorData.HasValue) return "\t\t\t\t\t\t\t\t\t";
            if (sensorData.Value.status != SensorStatus.OK) return "\t" + sensorData.Value.id + "\t" + sensorData.Value.status + "\t\t\t\t\t\t\t";

            string sensorString = "\t" + sensorData.Value.id + "\t" + sensorData.Value.status + "\t";

            Vector3 pos = sensorData.Value.position;
            if (includePostOffsets) pos += sensorData.Value.postOffset;
            Quaternion rot = sensorData.Value.rotation;
            sensorString += pos.x + "\t" + pos.y + "\t" + pos.z + "\t" + rot.w + "\t" + rot.x + "\t" + rot.y + "\t" + rot.z;

            return sensorString;
        }

        // Utility function because the version of .Net we're using doesn't include Path.GetRelativePath for some reason
        // Code taken from https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath) {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase)) {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}
