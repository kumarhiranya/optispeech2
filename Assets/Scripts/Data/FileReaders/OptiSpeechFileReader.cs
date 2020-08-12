using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Optispeech.Targets;
using Optispeech.Sensors;
using Optispeech.Documentation;
using UnityEngine.Events;

namespace Optispeech.Data.FileReaders {

    /// <summary>
    /// This file reader reads XXXX_raw.tsv files from OptiSpeech 2
    /// </summary>
    public class OptiSpeechFileReader : FrameReader.FileReader {

        /// <summary>
        /// The filename of the audio file recorded with this sweep, if it exists
        /// </summary>
        private string audioFile;
        /// <summary>
        /// The sensor configurations stored in the file
        /// </summary>
        SensorConfiguration[] sensorConfigurations;

        /// <summary>
        /// When creating this file reader the stream reader is passed so the entire header can be parsed
        /// </summary>
        /// <param name="file">The file to be read</param>
        /// <param name="loadTargets">Whether or not to add targets in the file</param>
        public OptiSpeechFileReader(StreamReader file, bool loadTargets) {
            string currLine = file.ReadLine();

            do {
                int firstTab = currLine.IndexOf('\t');
                string start = firstTab == -1 ? currLine : currLine.Substring(0, firstTab);
                switch (start) {
                    case "Source":
                    default:
                        break;
                    case "Audio":
                        audioFile = currLine.Split('\t')[1];
                        break;
                    case "-- Source Configurations":
                        // Ignore header row. We already know what's in the file
                        file.ReadLine();
                        List<SensorConfiguration> sensors = new List<SensorConfiguration>();
                        while (true) {
                            currLine = file.ReadLine();
                            if (currLine.Substring(0, 2) == "--") break;

                            string[] values = currLine.Split('\t');

                            SensorType type;
                            if (!Enum.TryParse(values[1], out type))
                                type = SensorType.OTHER;

                            sensors.Add(new SensorConfiguration {
                                id = int.Parse(values[0]),
                                preOffset = new Vector3(float.Parse(values[2]), float.Parse(values[3]), float.Parse(values[4])),
                                postOffset = new Vector3(float.Parse(values[5]), float.Parse(values[6]), float.Parse(values[7])),
                                type = type
                            });
                        }
                        sensorConfigurations = sensors.ToArray();
                        continue;
                    case "-- Targets":
                        // Ignore header row. We already know what's in the file
                        file.ReadLine();
                        while (true) {
                            currLine = file.ReadLine();
                            if (currLine.Substring(0, 2) == "--") break;
                            if (!loadTargets) continue;

                            string[] values = currLine.Split('\t');

                            TargetDescription description = Resources.LoadAll("Target Type Descriptions", typeof(TargetDescription)).Cast<TargetDescription>().Where(t => t.typeName == values[1]).FirstOrDefault();
                            string id = TargetsManager.Instance.AddTarget(description);
                            TargetsManager.Instance.targets[id].ApplyConfigFromString(currLine);
                        }
                        continue;
                }
                currLine = file.ReadLine();
            } while (currLine != "-- Sensor Data");
            // Get rid of header row of sensor data
            file.ReadLine();
        }

        [HideInDocumentation]
        public override void ReadFrames(StreamReader file, UnityAction<DataFrame> addDataFrame, UnityAction<bool> finish) {
            finish(FrameReader.ReadSingleLineFrames(file, addDataFrame, ReadFrame));
        }

        [HideInDocumentation]
        public override SensorConfiguration[] GetSensorConfigurations(DataFrame dataFrame) {
            return sensorConfigurations;
        }

        [HideInDocumentation]
        public override string GetAudioFile(string filename) {
            return audioFile;
        }

        [HideInDocumentation]
        private SensorStatus ParseStatus(string status) {
            if (!Enum.TryParse(status, out SensorStatus result))
                return SensorStatus.UNKNOWN;
            return result;
        }

        [HideInDocumentation]
        private DataFrame ReadFrame(string frame) {
            string[] values = frame.Split('\t');

            DataFrame dataFrame = new DataFrame {
                timestamp = (long)(double.Parse(values[0]))
            };

            // We'll skip values at indices 1 and 2 (MeasId and WavId, respectively).
            // The rest of the values are information for an arbitrary amount of sensors
            // Since only "OK" sensors will have positional and rotational data,
            // we'll read through the values and add them to a dynamic list of SensorData
            List<SensorData> sensorData = new List<SensorData>();
            for (int i = 1; i < values.Length; i += 9) {
                // See if there is a sensor for this "role"
                if (values[i] == "") continue;
                // The first value is the sensor ID
                int id = int.Parse(values[i]);

                // Then the sensor status. In older versions of Wave NDI / OS,
                // it was a number e.g. 55, but in newer versions its a string e.g. OK
                // Because of this complexity, we calculate it in its own function
                SensorStatus status = ParseStatus(values[i + 1]);

                // Stop here if the sensor isn't OK, because NDI Wave only stores data for OK sensors
                if (status != SensorStatus.OK) {
                    sensorData.Add(new SensorData {
                        id = id,
                        status = status
                    });
                    continue;
                }

                // The next three values are the X, Y, and Z components of the sensor's position
                Vector3 position = new Vector3(
                    float.Parse(values[i + 2]),
                    float.Parse(values[i + 3]),
                    float.Parse(values[i + 4]));

                // The last 4 values are the W, X, Y, and Z components of the sensor's rotation
                // In that order. Note that the Quaternion constructor is in X, Y, Z, W order
                Quaternion rotation = new Quaternion(
                    float.Parse(values[i + 8]),
                    float.Parse(values[i + 5]),
                    float.Parse(values[i + 6]),
                    float.Parse(values[i + 7]));

                // Piece everything together!
                sensorData.Add(new SensorData {
                    id = id,
                    status = status,
                    position = position,
                    rotation = rotation
                });
            }
            dataFrame.sensorData = sensorData.ToArray();

            return dataFrame;
        }
    }
}
