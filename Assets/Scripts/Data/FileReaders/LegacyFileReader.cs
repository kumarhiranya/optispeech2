using UnityEngine;
using System;
using System.Linq;
using Optispeech.Targets;
using Optispeech.Sensors;
using Optispeech.Targets.Controllers;
using Optispeech.Documentation;
using System.IO;
using UnityEngine.Events;

namespace Optispeech.Data.FileReaders {

    /// <summary>
    /// This file reader reads tsv files from the previous iteration of OptiSpeech
    /// </summary>
    public class LegacyFileReader : FrameReader.FileReader {

        /// <summary>
        /// When parsing the header row, this is the index of the first target
        /// </summary>
        private int firstTargetIndex;
        /// <summary>
        /// When parsing the header row, this is the index of the oscillating target
        /// </summary>
        private int oscillatingTargetIndex;
        /// <summary>
        /// When parsing the header row, this is the index of the landmark target
        /// </summary>
        private int landmarkTargetIndex;
        /// <summary>
        /// Cached value of the static target type, which we use to display the different targets.
        /// </summary>
        private TargetDescription staticTargetType;
        /// <summary>
        /// Whether to add targets from the file or not
        /// </summary>
        private bool loadTargets;

        /// <summary>
        /// When creating the file reader it takes a header row to determine which types of targets are in the file,
        /// and wherethey are in each frame of data
        /// </summary>
        /// <param name="headerRow">The first line of the file to read</param>
        /// <param name="loadTargets">Whether or not to add the targets in the file</param>
        /// <remarks>
        /// Note that there are various different versions of the legacy optispeech program, and if more target
        /// types are found they'll need to be handled in here as well
        /// </remarks>
        public LegacyFileReader(string headerRow, bool loadTargets) {
            string[] values = headerRow.Split('\t');

            // Store this for later
            // We'll add the actual target data on the first data frame
            firstTargetIndex = Array.IndexOf(values, "Target");
            oscillatingTargetIndex = Array.IndexOf(values, "OTarget");
            landmarkTargetIndex = Array.IndexOf(values, "OTarget");

            // Cache target types for adding them later
            TargetDescription[] targetTypes = Resources.LoadAll("Target Type Descriptions", typeof(TargetDescription)).Cast<TargetDescription>().ToArray();
            // TODO Fix hard-coded type name
            staticTargetType = targetTypes.Where(t => t.typeName == "Static Target").FirstOrDefault();

            this.loadTargets = loadTargets;
        }

        [HideInDocumentation]
        public override void ReadFrames(StreamReader file, UnityAction<DataFrame> addDataFrame, UnityAction<bool> finish) {
            finish(FrameReader.ReadSingleLineFrames(file, addDataFrame, ReadFrame));
        }

        [HideInDocumentation]
        public override SensorConfiguration[] GetSensorConfigurations(DataFrame dataFrame) {
            SensorConfiguration[] sensors = new SensorConfiguration[dataFrame.sensorData.Length];
            for (int i = 0; i < sensors.Length; i++) {
                SensorType type;
                // We determine default sensor types based on its ID
                // Note: The Legacy optispeech program used the same IDs as WaveFront, but added one to the ID
                // when writing to file, so this switch statement seems shifted up by one in comparison
                switch (dataFrame.sensorData[i].id) {
                    case 1: type = SensorType.FOREHEAD; break;
                    case 2: type = SensorType.LEFT_EAR; break;
                    case 3: type = SensorType.RIGHT_EAR; break;
                    case 4: type = SensorType.TONGUE_TIP; break;
                    case 5: type = SensorType.TONGUE_DORSUM; break;
                    case 6: type = SensorType.TONGUE_RIGHT; break;
                    case 7: type = SensorType.TONGUE_LEFT; break;
                    case 8: type = SensorType.TONGUE_BACK; break;
                    case 9: type = SensorType.JAW; break;
                    case 10: type = SensorType.OTHER; break;
                    default: type = SensorType.IGNORED; break;
                }

                sensors[i] = new SensorConfiguration {
                    id = dataFrame.sensorData[i].id,
                    // For some reason the jaw sensor and tongue sensors are super far apart,
                    // so we add this offset to fix that. They're arbitrarily picked because the output looks good
                    postOffset = new Vector3(0, -.2f, -.2f),
                    display = null,
                    status = dataFrame.sensorData[i].status,
                    type = type
                };
            }

            return sensors;
        }
        
        [HideInDocumentation]
        private DataFrame ReadFrame(string frame) {
            string[] values = frame.Split('\t');

            DataFrame dataFrame = new DataFrame {
                timestamp = long.Parse(values[0])
            };

            // We'll skip values at indices 1 and 2
            // (unused placeholders, probably put in place to more closely match the tsv exports from WaveFront)
            // The next n * 9 values are the data for n sensors, 
            // followed by m * 9 values for m targets,
            // potentially 9 values for an O target,
            // 9 values for the landmark,
            // and finally 2 or 3 values for timestamping purposes
            // Different versions of the legacy Optispeech program had different values for those. Due to that,
            // we use the header row from the constructor to determine what data is present in this file

            // First calculate how many sensors by taking the number of values before the first target,
            // subtracting the first 3 values which are the timestamp and placeholder values,
            // and divide by 9 because that's how many values we have per sensor
            int numSensors = (firstTargetIndex - 3) / 9;

            // Fill in the data for each sensor
            dataFrame.sensorData = new SensorData[numSensors];
            for (int i = 0; i < numSensors; i++) {
                int sensorIndex = 3 + i * 9;

                // The first value is the sensor ID
                int id = int.Parse(values[sensorIndex]);

                // We'll skip the status value because its always 55, and then
                // the next three values are the Y, Z, and X components of the sensor's position
                // Note the weird positional ordering
                Vector3 position = new Vector3(
                    float.Parse(values[sensorIndex + 4]),
                    float.Parse(values[sensorIndex + 2]),
                    float.Parse(values[sensorIndex + 3]));

                // The last 4 values are the W, X, Y, and Z components of the sensor's rotation
                // In that order. Note that the Quaternion constructor is in X, Y, Z, W order
                Quaternion rotation = new Quaternion(
                    float.Parse(values[sensorIndex + 8]),
                    float.Parse(values[sensorIndex + 5]),
                    float.Parse(values[sensorIndex + 6]),
                    float.Parse(values[sensorIndex + 7]));

                // Piece everything together!
                // Note the sensor status is always OK,
                // that's because the old optispeech program always wrote "55"
                // to file for the sensor status, which is the legacy WaveFront program's
                // status code for OK.
                dataFrame.sensorData[i] = new SensorData {
                    id = id,
                    status = SensorStatus.OK,
                    position = position,
                    rotation = rotation
                };
            }

            if (loadTargets) {
                // Update targets
                for (int i = firstTargetIndex; i < values.Length && i < oscillatingTargetIndex; i += 9) {
                    if (!TargetsManager.Instance.targets.ContainsKey(values[i])) {
                        // Add target
                        string id = TargetsManager.Instance.AddTarget(staticTargetType);
                        TargetsManager.Instance.targets[id].ApplyConfigFromString(values[i] + "\tStatic Target\t" + values[i + 5] + "\t" + values[i + 2] + "\t" + values[i + 3] + "\t" + values[i + 4]);
                    }
                }

                // Update oscillating targets
                // We actually use a static target and just change its position each frame,
                // since the legacy program implemented oscillation in a different way and didn't store the properties we need to file
                // TODO could we make a sensor and give it the oscillating target's data, and then create an actual oscillating target instead of a static one?
                if (oscillatingTargetIndex >= 0) {
                    string x = values[oscillatingTargetIndex + 2];
                    string y = values[oscillatingTargetIndex + 3];
                    string z = values[oscillatingTargetIndex + 4];
                    if (TargetsManager.Instance.targets.ContainsKey(values[oscillatingTargetIndex])) {
                        // Update target's position
                        if (float.TryParse(x, out float xf) && float.TryParse(y, out float yf) && float.TryParse(z, out float zf))
                            ((StaticTargetController)TargetsManager.Instance.targets[values[oscillatingTargetIndex]]).position = new Vector3(xf, yf, zf);
                    } else {
                        // Add target
                        string id = TargetsManager.Instance.AddTarget(staticTargetType);
                        TargetsManager.Instance.targets[id].ApplyConfigFromString(values[oscillatingTargetIndex] + "\tStatic Target\t" + values[oscillatingTargetIndex + 5] + "\t" + x + "\t" + y + "\t" + z);
                    }
                }

                // Update landmark targets
                // Once again the legacy program doesn't actually tell us the "right" data (in this case the sensor acting as the landmark),
                // so we create a static target and update its position each frame
                // TODO could we make a sensor and give it the landmark's data, and then create an actual landmark target instead of a static one?
                if (landmarkTargetIndex >= 0) {
                    string x = values[landmarkTargetIndex + 2];
                    string y = values[landmarkTargetIndex + 3];
                    string z = values[landmarkTargetIndex + 4];
                    if (TargetsManager.Instance.targets.ContainsKey(values[landmarkTargetIndex])) {
                        // Update target's position
                        if (float.TryParse(x, out float xf) && float.TryParse(y, out float yf) && float.TryParse(z, out float zf))
                            ((StaticTargetController)TargetsManager.Instance.targets[values[landmarkTargetIndex]]).position = new Vector3(xf, yf, zf);
                    } else {
                        // Add target
                        string id = TargetsManager.Instance.AddTarget(staticTargetType);
                        TargetsManager.Instance.targets[id].ApplyConfigFromString(values[landmarkTargetIndex] + "\tStatic Target\t" + values[landmarkTargetIndex + 5] + "\t" + x + "\t" + y + "\t" + z);
                    }
                }
            }

            return dataFrame;
        }
    }
}
