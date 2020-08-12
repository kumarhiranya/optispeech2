using UnityEngine;
using System.Collections.Generic;
using Optispeech.Sensors;
using Optispeech.Documentation;
using UnityEngine.Events;
using System.IO;
using System;

namespace Optispeech.Data.FileReaders {

    /// <summary>
    /// This file reader reads tsv files from NDI WaveFront
    /// </summary>
    public class WaveFrontFileReader : FrameReader.FileReader {

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
                switch (dataFrame.sensorData[i].id) {
                    case 0: type = SensorType.FOREHEAD; break;
                    case 1: type = SensorType.LEFT_EAR; break;
                    case 2: type = SensorType.RIGHT_EAR; break;
                    case 3: type = SensorType.TONGUE_TIP; break;
                    case 4: type = SensorType.TONGUE_DORSUM; break;
                    case 5: type = SensorType.TONGUE_RIGHT; break;
                    case 6: type = SensorType.TONGUE_LEFT; break;
                    case 7: type = SensorType.TONGUE_BACK; break;
                    case 8: type = SensorType.JAW; break;
                    case 9: type = SensorType.OTHER; break;
                    default: type = SensorType.IGNORED; break;
                }

                sensors[i] = new SensorConfiguration {
                    id = dataFrame.sensorData[i].id,
                    postOffset = Vector3.zero,
                    display = null,
                    status = dataFrame.sensorData[i].status,
                    type = type
                };
            }

            return sensors;
        }

        [HideInDocumentation]
        private SensorStatus ParseStatus(string status) {
            // This information was gathered from an email with NDI. There is no official documentation on
            // old status codes, but the contact tested each situation that would prompt a string status code
            // on the new one, and told us what status code it gave in the old one. It's possible there are
            // other statuses that were dropped in the transition to the new software, but these are the
            // only known status codes
            // I've seen a 311 but I don't know what it represents
            switch (status) {
                case "55":
                case "OK":
                    return SensorStatus.OK;
                case "183":
                case "Out of Volume":
                    return SensorStatus.OUT_OF_VOLUME;
                case "119":
                case "Processing Error":
                    return SensorStatus.PROCESSING_ERROR;
                case "BAD_FIT":
                    return SensorStatus.BAD_FIT;
                default:
                    return SensorStatus.UNKNOWN;
            }
        }

        [HideInDocumentation]
        private DataFrame ReadFrame(string frame) {
            string[] values = frame.Split('\t');

            DataFrame dataFrame = new DataFrame {
                // WaveFront gives us the timestamp in seconds, so we have to convert to ms
                timestamp = (long)(double.Parse(values[0]) * 1000)
            };

            // We'll skip values at indices 1 and 2 (MeasId and WavId, respectively).
            // The rest of the values are information for an arbitrary amount of sensors
            // Since only "OK" sensors will have positional and rotational data,
            // we'll read through the values and add them to a dynamic list of SensorData
            List<SensorData> sensorData = new List<SensorData>();
            for (int i = 3; i < values.Length; i += 9) {
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

                // Sometimes the sensor status is OK but the rest of the values are still empty :/
                // We'll set the status to "UNKNOWN" and continue to the next sensor
                bool skip = false;
                for (int j = i + 2; j <= i + 8; j++)
                    if (values[j] == "") {
                        sensorData.Add(new SensorData {
                            id = id,
                            status = SensorStatus.UNKNOWN
                        });
                        skip = true;
                        break;
                    }
                if (skip) continue;

                // The next three values are the X, Y, and Z components of the sensor's position
                Vector3 position = new Vector3(
                    float.Parse(values[i + 4]),
                    float.Parse(values[i + 2]),
                    float.Parse(values[i + 3]));

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
