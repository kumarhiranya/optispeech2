using Optispeech.Advanced;
using Optispeech.Data;
using Optispeech.Documentation;
using Optispeech.Sensors;
using Optispeech.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Optispeech.Avatar {
    
    /// <summary>
    /// Controls the avatar and handles updating it based on the current dataframe
    /// </summary>
    public class AvatarController : MonoBehaviour {

        /// <summary>
        /// Technically this class doesn't need to be a singleton, but dataframes will use the positions
        /// of some avatar's reference points (forehead, ears, and jaw) to calculate the transformation
        /// matrix to map raw data onto the avatar, so this is a static member to access the "main" instance
        /// (as opposed to the singleton instance, which won't be enforced)
        /// </summary>
        public static AvatarController Main = default;

        /// <summary>
        /// The tongue has its own controller due to its own complexity,
        /// and this reference is used to give it the data it needs to update
        /// </summary>
        [SerializeField]
        private TongueController tongueController = default;

        /// <summary>
        /// This container is where any markers for sensors with type "OTHER"
        /// will be instantiated into
        /// </summary>
        [Header("Sensor Markers")]
        [SerializeField]
        private Transform markersParent = default;
        /// <summary>
        /// This prefab is used to instantiate a dynamic number of sensor markers
        /// for each sensor with type "OTHER"
        /// </summary>
        [SerializeField]
        private GameObject markerPrefab = default;

        /// <summary>
        /// A reference point at the front of the head. If the patient is given glasses
        /// this sensor can be attached to the bridge of the glasses
        /// </summary>
        [Header("Head Rig Reference Points")]
        [SerializeField]
        private Transform forehead = default;
        /// <summary>
        /// A reference point on the left side of the head on the same plane as the forehead
        /// sensor. If the patient is given glasses this sensor can be attached above the left ear
        /// </summary>
        [SerializeField]
        private Transform leftEar = default;
        /// <summary>
        /// A reference point on the right side of the head on the same plane as the forehead
        /// sensor. If the patient is given glasses this sensor can be attached above the right ear
        /// </summary>
        [SerializeField]
        private Transform rightEar = default;
        /// <summary>
        /// A reference point on the bottom of the head. Since heads have different sizes the jaw is used to offset
        /// the points so the tongue appears in the right place relative to the avatar's jaw
        /// </summary>
        [SerializeField]
        private Transform jaw = default;

#if UNITY_EDITOR
        /// <summary>
        /// Only when inside the editor in the "Scene" panel, this property will control the size of spherical
        /// gizmos at each of the sensor
        /// </summary>
        /// <remarks>Probably not useful at this stage</remarks>
        [Header("Debug Options")]
        [SerializeField]
        private float debugSphereRadius = .05f;
#endif

        /// <summary>
        /// Reference to the timestamp of the most recently displayed data frame. Since
        /// a new Update call doesn't necessarily correspond to a new data frame, and since
        /// its not useful to update for a data frame more than once per Update, this is used to 
        /// check if the current dataframe is a new one, in the Update method. There is a slight possibility
        /// of this not updating when it should if two data frames have the exact same timestamp, which
        /// would require them to have been recorded in the same millisecond, or more likely be a result from 
        /// changing data sources that coincidentally gives a frame with the same timestamp. In those cases,
        /// the problem will only last until the next data frame comes and fixes it, though
        /// </summary>
        private long lastTimestamp = 0;

        /// <summary>
        /// Pre-calculated rotation looking from the left ear to the right ear. This will also be calculated
        /// for each dataframe to create the transform matrix to place the raw data onto the avatar.
        /// </summary>
        private Quaternion rigLookDir;
        /// <summary>
        /// Pre-"calculated" position of the forehead reference point, used when creating the transformation matrix
        /// </summary>
        private Vector3 foreheadPosition;
        /// <summary>
        /// Pre-"calculated" position of the left ear reference point, used when creating the transformation matrix
        /// </summary>
        private Vector3 leftEarPosition;
        /// <summary>
        /// Pre-"calculated" position of the right ear reference point, used when creating the transformation matrix
        /// </summary>
        private Vector3 rightEarPosition;
        /// <summary>
        /// Pre-"calculated" position of the jaw reference point, used when creating the transformation matrix
        /// </summary>
        private Vector3 jawPosition;

        /// <summary>
        /// List of currently instantiated markers for sensors with
        /// type "OTHER". Will be recreated whenever the number
        /// of such sensors changes
        /// </summary>
        private GameObject[] otherSensorMarkers = new GameObject[0];

        [HideInDocumentation]
        void OnEnable() {
            if (Main == null || Main == this) {
                Main = this;
                DontDestroyOnLoad(gameObject);
            }

            // Calculate look vector for our rig for comparison with incoming
            // sensor data, to calculate our transformation matrix
            rigLookDir = Quaternion.LookRotation(forehead.position - leftEar.position, forehead.position - rightEar.position);
            foreheadPosition = forehead.position;
            leftEarPosition = leftEar.position;
            rightEarPosition = rightEar.position;
            jawPosition = jaw.position;
        }

        [HideInDocumentation]
        void Update() {
            // If there isn't an active data source or there is but it hasn't produced a data frame yet,
            // then just ensure our avatar is in its default state and exit
            if (!DataSourceManager.Instance.dataSourceReader ||
                DataSourceManager.Instance.dataSourceReader.lastFrame.timestamp == 0) {
                tongueController.Reset();
                return;
            }

            // Copy value of last frame to new instance
            // No need for locks because structs copy themselves when
            //  assigned, as opposed to using references
            // Therefore this line of code ensures we're using the same data for the entire
            //  Update method
            // Applies low pass filter as well; doing it here so that it only affects the visuals
            DataFrame currentFrame = FilterManager.Instance.ApplyFilter(DataSourceManager.Instance.dataSourceReader.lastFrame);

            // Check if we should use the current frame to calculate our resting position 
            if (SensorsManager.Instance.offsetsDirty) {
                SetRestingPosition(currentFrame.sensorData);
            }

            // Check if we have a new data frame
            if (lastTimestamp != currentFrame.timestamp) {
                // Mark this data frame as our most recent
                // (structs are pass by value so we use the timestamp to identify them)
                lastTimestamp = currentFrame.timestamp;

                // Update our tongue rig
                tongueController.UpdateRig(currentFrame.transformedData);

                // Check if we need to recreate our list of markers for sensors with type "OTHER"
                // This is necessary since the number of sensors with that type can be any amount
                // (whereas most other types can have at most one)
                int currentAmount = otherSensorMarkers.Length;
                int newAmount = SensorsManager.Instance.otherSensorConfigs.Count();
                if (currentAmount != newAmount) {
                    // Delete any extra sensor markers
                    for (int i = currentAmount - 1; i >= newAmount; i--)
                        Destroy(otherSensorMarkers[i]);

                    // Resize our array
                    // (now that we've destroyed any that may have been trimmed)
                    // (and before we try to add any more to the array)
                    GameObject[] oldMarkers = otherSensorMarkers;
                    otherSensorMarkers = new GameObject[newAmount];
                    Array.Copy(oldMarkers, 0, otherSensorMarkers, 0, Math.Min(currentAmount, newAmount));

                    // Create any additional sensor markers we need
                    for (int i = currentAmount; i < newAmount; i++)
                        otherSensorMarkers[i] = Instantiate(markerPrefab, markersParent);
                }

                // Update marker positions for any sensor with type "OTHER"
                int j = 0;
                foreach (SensorData sensor in currentFrame.transformedData.otherSensors) {
                    otherSensorMarkers[j].transform.position = sensor.position;
                    j++;
                }

                // Update target positions
                foreach (KeyValuePair<string, Vector3> kvp in currentFrame.targetPositions) {
                    // Check if key is still a valid target,
                    // since they can be added/removed independently from data frames being collected
                    // (this means changing a target may not update its position for a frame)
                    if (TargetsManager.Instance.targets.ContainsKey(kvp.Key)) {
                        TargetsManager.Instance.targets[kvp.Key].UpdateTarget(kvp.Value, currentFrame.transformedData.tongueTip);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the offset needed to make the tongue appear the correct distance from the avatar's jaw,
        /// to account for the size or shape of the patient not matching the avatar
        /// </summary>
        /// <param name="sensorData">The raw data from one data frame</param>
        private void SetRestingPosition(SensorData[] sensorData) {
            // Set every sensors' pre-offset to Vector3.zero
            foreach (SensorConfiguration config in SensorsManager.Instance.sensors)
                config.preOffset = Vector3.zero;

            TransformedData data = DataFrame.GetTransformedData(sensorData);

            // TODO how to handle not having a jaw sensor?
            if (!data.jaw.HasValue) return;

            // Calculate what the offset should be,
            // and calculate what that would be before applying the transform matrix
            Vector3 transformedOffset = jaw.position - data.jaw.Value.position;
            Vector3 preOffset = -data.transformMatrix.inverse.MultiplyVector(transformedOffset);

            // Set our (non-jaw) reference sensors' pre-offsets to the calculated value,
            //  checking for null values because these sensors may not be present
            if (SensorsManager.Instance.foreheadSensorConfig != null)
                SensorsManager.Instance.foreheadSensorConfig.preOffset = preOffset;
            if (SensorsManager.Instance.leftEarSensorConfig != null)
                SensorsManager.Instance.leftEarSensorConfig.preOffset = preOffset;
            if (SensorsManager.Instance.rightEarSensorConfig != null)
                SensorsManager.Instance.rightEarSensorConfig.preOffset = preOffset;

            // Mark our offsets clean
            SensorsManager.Instance.offsetsDirty = false;
        }

        /// <summary>
        /// Create transformation matrix using the reference markers on the left, right, and/or front sides of the head
        /// The matrix will transform points such that the tongue sensors' raw positions can map onto the avatar's tongue
        /// </summary>
        /// <param name="foreheadSensor">The sensor at the front of the head, if exists</param>
        /// <param name="jawSensor">The sensor at the bottom of the head, if exists</param>
        /// <param name="leftEarSensor">The sensor on the left side of the head, if exists</param>
        /// <param name="rightEarSensor">The sensor on the right side of the head, if exists</param>
        /// <returns>A transformation matrix that will map sensor data onto the avatar</returns>
        public Matrix4x4 GetReal2RigMatrix(SensorData? foreheadSensor, SensorData? jawSensor, SensorData? leftEarSensor, SensorData? rightEarSensor) {
            // TODO Implement handling situations where the jaw sensor isn't available
            if (!jawSensor.HasValue)
                return Matrix4x4.identity;

            // If any of our forehead or ear sensors are invalid we can still create a matrix from just the jaw,
            // assuming the data is all properly rotated and the forehead is at (0, 0, 0)
            if (!foreheadSensor.HasValue || !leftEarSensor.HasValue || !rightEarSensor.HasValue) {
                float ratio = Vector3.Distance(foreheadPosition, jawPosition) / Vector3.Distance(Vector3.zero, jawSensor.Value.position);
                return Matrix4x4.Translate(foreheadPosition) *
                    Matrix4x4.Scale(new Vector3(-ratio, ratio, -ratio));
            }

            // We use an arbitrary look direction that is made by standing at the forehead and looking at the left ear,
            // tilting our head so that "up" is the right ear. What's important is that it uses three points and it uses
            // the same three points on the rig and the real sensor data
            Quaternion realLookDir = Quaternion.LookRotation(
                foreheadSensor.Value.position - leftEarSensor.Value.position,
                foreheadSensor.Value.position - rightEarSensor.Value.position);

            // Shrink or Stretch each axis to match the proportions of the avatar
            // Calculate z using ear positions (note x and z are opposite what you'd expect, because our avatar is facing right instead of forward)
            float zRatio = Vector3.Distance(leftEarPosition, rightEarPosition) /
                Vector3.Distance(leftEarSensor.Value.position, rightEarSensor.Value.position);
            // Calculate y using the forehead and jaw positions
            float yRatio = -Vector3.Distance(foreheadPosition, jawPosition) /
                Vector3.Distance(foreheadSensor.Value.position, jawSensor.Value.position);
            // Calculate x using the average ear positions and the forehead position
            float xRatio = Vector3.Distance((leftEarPosition + rightEarPosition) / 2, foreheadPosition) /
                           Vector3.Distance((leftEarSensor.Value.position + rightEarSensor.Value.position) / 2, foreheadSensor.Value.position);

            // Note we don't use Matrix4x4.TRS because we explicitly want to rotate before we scale to reduce sensor "tilt"
            // I believe any remaining tilt is probably due to the ear sensors not being at the exact same y and z as each other,
            // or not at the same y as the forehead sensor. You can see the ear sensors relative to the forehead in the Scene panel
            return Matrix4x4.Translate(foreheadPosition) *
                Matrix4x4.Scale(new Vector3(xRatio, yRatio, zRatio)) *
                Matrix4x4.Rotate(rigLookDir * Quaternion.Inverse(realLookDir));
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draw spheres where each sensor is in the editor's Scene panel
        /// </summary>
        private void OnDrawGizmos() {
            // Early exit if we don't have a data frame to draw
            if (DataSourceManager.Instance == null ||
                DataSourceManager.Instance.dataSourceReader == null ||
                DataSourceManager.Instance.dataSourceReader.lastFrame.sensorData == null)
                return;

            TransformedData data = DataSourceManager.Instance.dataSourceReader.lastFrame.transformedData;

            // Draw front of head sensor
            Gizmos.color = Color.white;
            if (data.forehead.HasValue) Gizmos.DrawSphere(data.forehead.Value.position, debugSphereRadius);

            // Draw left and right sides of head sensors
            Gizmos.color = Color.blue;
            if (data.leftEar.HasValue) Gizmos.DrawSphere(data.leftEar.Value.position, debugSphereRadius);
            if (data.rightEar.HasValue) Gizmos.DrawSphere(data.rightEar.Value.position, debugSphereRadius);

            // Draw tongue sensors
            Gizmos.color = Color.yellow;
            if (data.tongueTip.HasValue) Gizmos.DrawSphere(data.tongueTip.Value.position, debugSphereRadius);
            if (data.tongueDorsum.HasValue) Gizmos.DrawSphere(data.tongueDorsum.Value.position, debugSphereRadius);
            if (data.tongueLeft.HasValue) Gizmos.DrawSphere(data.tongueLeft.Value.position, debugSphereRadius);
            if (data.tongueRight.HasValue) Gizmos.DrawSphere(data.tongueRight.Value.position, debugSphereRadius);
            if (data.tongueBack.HasValue) Gizmos.DrawSphere(data.tongueBack.Value.position, debugSphereRadius);

            // Draw jaw
            Gizmos.color = Color.green;
            if (data.jaw.HasValue) Gizmos.DrawSphere(data.jaw.Value.position, debugSphereRadius);

            // Draw extra sensors
            Gizmos.color = Color.gray;
            foreach (SensorData sensor in data.otherSensors) {
                Gizmos.DrawSphere(sensor.position, debugSphereRadius);
            }
        }
#endif
    }
}
