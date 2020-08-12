using Optispeech.Avatar;
using Optispeech.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Optispeech.Data {

    /// <summary>
    /// Different data sources may have different ways of reporting sensor statuses,
    /// so we map them as best as we can to these different status categories (based
    /// on the WaveFront statuses). With these we can display sensor statuses to the user
    /// without any data source-specific code.
    /// </summary>
    public enum SensorStatus {
        OK,
        OUT_OF_VOLUME,
        PROCESSING_ERROR,
        BAD_FIT,
        UNKNOWN
    }

    /// <summary>
    /// The data for a single sensor inside of a data frame
    /// </summary>
    [Serializable]
    public struct SensorData {
        /// <summary>
        /// The ID of the sensor this data is for
        /// </summary>
        public int id;
        /// <summary>
        /// The status of this sensor in this frame
        /// </summary>
        public SensorStatus status;
        /// <summary>
        /// The recorded rotation for this sensor this frame
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// The recorded position for this sensor this frame
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The offset configured by the user. This gets stored separately from the
        /// position because the position may be changed while processing the data frame,
        /// and the post offset should be applied after all that
        /// </summary>
        public Vector3 postOffset;
    }

    /// <summary>
    /// Struct to hold the transformed data for a given data frame. Stores specific sensors by role,
    /// and includes the transformation matrix used to map the raw sensors to their transformed versions
    /// </summary>
    public struct TransformedData {
        /// <summary>
        /// The transformation matrix used to map this frame's raw sensor data to fit onto the avatar model
        /// </summary>
        public Matrix4x4 transformMatrix;

        /// <summary>
        /// The sensor data for the first sensor configured as the forehead, if any
        /// </summary>
        public SensorData? forehead;
        /// <summary>
        /// The sensor data for the first sensor configured as the jaw, if any
        /// </summary>
        public SensorData? jaw;
        /// <summary>
        /// The sensor data for the first sensor configured as the left ear, if any
        /// </summary>
        public SensorData? leftEar;
        /// <summary>
        /// The sensor data for the first sensor configured as the right ear, if any
        /// </summary>
        public SensorData? rightEar;
        /// <summary>
        /// The sensor data for the first sensor configured as the tip of the tongue, if any
        /// </summary>
        public SensorData? tongueTip;
        /// <summary>
        /// The sensor data for the first sensor configured as the center of the tongue, if any
        /// </summary>
        public SensorData? tongueDorsum;
        /// <summary>
        /// The sensor data for the first sensor configured as the right of the tongue, if any
        /// </summary>
        public SensorData? tongueRight;
        /// <summary>
        /// The sensor data for the first sensor configured as the left of the tongue, if any
        /// </summary>
        public SensorData? tongueLeft;
        /// <summary>
        /// The sensor data for the first sensor configured as the back of the tongue, if any
        /// </summary>
        public SensorData? tongueBack;

        /// <summary>
        /// The sensor data for all sensors configured as "Other"
        /// </summary>
        public SensorData[] otherSensors;
    }

    /// <summary>
    /// A single "frame" of data from a data source. A "frame" is a single reading from the data source,
    /// and contains the timestamp it was either given by the data source or when it was received by this program,
    /// as well as the data recorded for each sensor in this frame. The frame will also be processed and then given
    /// the data once transformed to fit on the avatar model as well as the positions of each of the targets at this timestamp
    /// </summary>
    [Serializable]
    public struct DataFrame {
        /// <summary>
        /// The timestamp for this data frame. This will either be given by the data source directly if possible,
        /// or calculated directly by this program.
        /// </summary>
        public long timestamp;
        /// <summary>
        /// An array of raw sensor data directly from the data source
        /// </summary>
        public SensorData[] sensorData;
        /// <summary>
        /// A processed version of the sensor data that's been mapped onto the avatar model
        /// </summary>
        public TransformedData transformedData;
        /// <summary>
        /// A dictionary of positions for each active target. Since many targets can have different positions
        /// over time, this is the position that target has at this frame's timestamp
        /// </summary>
        public Dictionary<string, Vector3> targetPositions;

        /// <summary>
        /// Static method for processing raw sensor data to map them onto an avatar model. This will use the raw data
        /// to construct an appropriate transformation matrix, find the first sensor data for each "role", and store
        /// each of their transformed versions
        /// </summary>
        /// <param name="sensorData">The raw sensor data from a data source</param>
        /// <returns>A struct of processed data mapping each sensor role onto the avatar model</returns>
        public static TransformedData GetTransformedData(SensorData[] sensorData) {
            SensorData? forehead = GetSensorData(sensorData, SensorsManager.Instance.foreheadSensorConfig);
            SensorData? jaw = GetSensorData(sensorData, SensorsManager.Instance.jawSensorConfig);
            SensorData? leftEar = GetSensorData(sensorData, SensorsManager.Instance.leftEarSensorConfig);
            SensorData? rightEar = GetSensorData(sensorData, SensorsManager.Instance.rightEarSensorConfig);
            SensorData? tongueTip = GetSensorData(sensorData, SensorsManager.Instance.tongueTipSensorConfig);
            SensorData? tongueDorsum = GetSensorData(sensorData, SensorsManager.Instance.tongueDorsumSensorConfig);
            SensorData? tongueRight = GetSensorData(sensorData, SensorsManager.Instance.tongueRightSensorConfig);
            SensorData? tongueLeft = GetSensorData(sensorData, SensorsManager.Instance.tongueLeftSensorConfig);
            SensorData? tongueBack = GetSensorData(sensorData, SensorsManager.Instance.tongueBackSensorConfig);

            Matrix4x4 transformMatrix = AvatarController.Main.GetReal2RigMatrix(forehead, jaw, leftEar, rightEar);

            Vector3 foreheadOffset = forehead.HasValue ? forehead.Value.position : Vector3.zero;
            TransformedData transformedData = new TransformedData {
                transformMatrix = transformMatrix,
                forehead = GetTransformedSensorData(forehead, foreheadOffset, transformMatrix),
                jaw = GetTransformedSensorData(jaw, foreheadOffset, transformMatrix),
                leftEar = GetTransformedSensorData(leftEar, foreheadOffset, transformMatrix),
                rightEar = GetTransformedSensorData(rightEar, foreheadOffset, transformMatrix),
                tongueTip = GetTransformedSensorData(tongueTip, foreheadOffset, transformMatrix),
                tongueDorsum = GetTransformedSensorData(tongueDorsum, foreheadOffset, transformMatrix),
                tongueRight = GetTransformedSensorData(tongueRight, foreheadOffset, transformMatrix),
                tongueLeft = GetTransformedSensorData(tongueLeft, foreheadOffset, transformMatrix),
                tongueBack = GetTransformedSensorData(tongueBack, foreheadOffset, transformMatrix)
            };

            transformedData.otherSensors = SensorsManager.Instance.otherSensorConfigs
                .Select(sensorConfig => GetSensorData(sensorData, sensorConfig))
                .Where(sensor => sensor.HasValue)
                .Select(sensor => GetTransformedSensorData(sensor.Value, foreheadOffset, transformMatrix).Value)
                .ToArray();

            return transformedData;
        }

        /// <summary>
        /// Utility function used while processing a data frame that finds the first sensor that fills a specified role, if any.
        /// Also applies the pre-offset configured for that sensor, and adds the configured post offset to the sensor data.
        /// </summary>
        /// <param name="sensorData">The raw sensor data from a data source</param>
        /// <param name="sensorConfig">The configuration for the sensor role to search for</param>
        /// <returns>The found sensor data, if any</returns>
        private static SensorData? GetSensorData(SensorData[] sensorData, SensorConfiguration sensorConfig) {
            if (sensorConfig == null) return null;

            // Note our sensor data is a struct, which can't be null, so we cast it to a nullable struct so that if
            // there isn't a sensor with the needed ID we can figure that out by checking if sensor == null
            // Also note if there are multiple sensors with the same ID this'll always pick the first one
            SensorData? sensor = sensorData.Where(s => s.id == sensorConfig.id).Cast<SensorData?>().FirstOrDefault();

            if (!sensor.HasValue) return sensor;

            // If the sensor does exist, apply our sensorConfig's offset to it
            // Note that, being a struct, this process copies the sensordata and leaves the original untouched
            SensorData processedSensor = sensor.Value;
            processedSensor.position += sensorConfig.preOffset;
            processedSensor.postOffset = sensorConfig.postOffset;

            // If there's anything wrong with this data, ignore it and return null
            // TODO NaN values still seem to get through sometimes
            if (float.IsNaN(processedSensor.position.x) || float.IsInfinity(processedSensor.position.x) ||
                float.IsNaN(processedSensor.position.y) || float.IsInfinity(processedSensor.position.y) ||
                float.IsNaN(processedSensor.position.z) || float.IsInfinity(processedSensor.position.z))
                return null;

            return processedSensor;
        }

        /// <summary>
        /// Utility function used while processing a data frame that takes a sensor data as well as the forehead position and the transformation
        /// matrix to map sensors onto the avatar model, and applies the matrix to the sensor data to return the transformed data
        /// </summary>
        /// <param name="sensor">The sensor data to transform</param>
        /// <param name="foreheadOffset">The forehead position, used to position the sensor relative to the forehead</param>
        /// <param name="transformMatrix">The transformation matrix that maps sensors onto the avatar model</param>
        /// <returns>The sensor data but mapped onto the avatar model</returns>
        private static SensorData? GetTransformedSensorData(SensorData? sensor, Vector3 foreheadOffset, Matrix4x4 transformMatrix) {
            if (!sensor.HasValue) return null;

            return new SensorData {
                id = sensor.Value.id,
                status = sensor.Value.status,
                position = transformMatrix.MultiplyPoint3x4(sensor.Value.position - foreheadOffset),
                postOffset = sensor.Value.postOffset,
                // The sensor is rotated 180 degrees because the avatar head is technically facing backwards
                rotation = Quaternion.Euler(transformMatrix.MultiplyVector(sensor.Value.rotation.eulerAngles) - new Vector3(0, 180, 0))
            };
        }
    }
}
