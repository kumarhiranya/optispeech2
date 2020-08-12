using UnityEngine;
using Optispeech.Data;
using Optispeech.Profiles;
using Optispeech.Documentation;

namespace Optispeech.Advanced {

    /// <summary>
    /// Manager singleton for applying a low pass filter to DataFrame objects as they're read
    /// </summary> 
    public class FilterManager : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static FilterManager Instance = default;

        /// <summary>
        /// Used to store the previous frame if it exists.
        /// Whenever we process a data frame we'll set this, and set it to null whenever switching sources.
        /// Additionally, a data source implementation may choose to reset the previousFrame to null when
        /// they want, such as when its config changes
        /// </summary>
        public DataFrame? previousFrame = null;

        /// <summary>
        /// Store initial strength to use as default for new profiles. 0 is no affect
        /// </summary>
        public int defaultStrength;

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

        /// <summary>
        /// Applies the low pass filter to the given data frame based on the previous frame.
        /// Optionally, a previous frame can be passed to override <see cref="previousFrame"/>.
        /// If overidden, then <see cref="previousFrame"/> will NOT be set to <paramref name="frame"/>.
        /// </summary>
        /// <param name="frame">The frame to have the filter applied to</param>
        /// <param name="prevFrame">The previous data frame if overidding <see cref="previousFrame"/></param>
        /// <returns><paramref name="frame"/>, after aplying the filter</returns>
        public DataFrame ApplyFilter(DataFrame frame, DataFrame? prevFrame = null) {
            DataFrame? previousFrame = null;

            if (prevFrame.HasValue)
                previousFrame = prevFrame;

            if (!previousFrame.HasValue && !this.previousFrame.HasValue) {
                this.previousFrame = frame;
                return frame;
            } else
                previousFrame = this.previousFrame;

            // Calculate alpha
            float dt = frame.timestamp - previousFrame.Value.timestamp;
            // If no time has passed, we don't want to divide by zero when the LPF is 0
            // Fortunately, if dt = 0 we can actually just return frame, because that would be the appropriate
            // behavior with any value of lpfStrength
            if (dt == 0) return frame;
            float alpha = dt / (ProfileManager.Instance.ActiveProfile.lpfStrength + dt);

            // Apply filter to each sensor
            TransformedData previousData = previousFrame.Value.transformedData;
            frame.transformedData.forehead = ApplyFilter(frame.transformedData.forehead, previousData.forehead, alpha);
            frame.transformedData.jaw = ApplyFilter(frame.transformedData.jaw, previousData.jaw, alpha);
            frame.transformedData.leftEar = ApplyFilter(frame.transformedData.leftEar, previousData.leftEar, alpha);
            frame.transformedData.rightEar = ApplyFilter(frame.transformedData.rightEar, previousData.rightEar, alpha);
            frame.transformedData.tongueBack = ApplyFilter(frame.transformedData.tongueBack, previousData.tongueBack, alpha);
            frame.transformedData.tongueDorsum = ApplyFilter(frame.transformedData.tongueDorsum, previousData.tongueDorsum, alpha);
            frame.transformedData.tongueLeft = ApplyFilter(frame.transformedData.tongueLeft, previousData.tongueLeft, alpha);
            frame.transformedData.tongueRight = ApplyFilter(frame.transformedData.tongueRight, previousData.tongueRight, alpha);
            frame.transformedData.tongueTip = ApplyFilter(frame.transformedData.tongueTip, previousData.tongueTip, alpha);

            for (int i = 0; i < frame.transformedData.otherSensors.Length; i++) {
                // It'd be slow to search for each sensor in previousFrame by ID and they should have the same indices most of the time,
                //  so we'll just handle the cases where the indices are the same - whenever the indices change the filter should kick
                //  back in for them after a frame
                if (i >= previousData.otherSensors.Length || frame.transformedData.otherSensors[i].id != previousData.otherSensors[i].id) continue;
                frame.transformedData.otherSensors[i] = ApplyFilter(frame.transformedData.otherSensors[i], previousData.otherSensors[i], alpha).Value;
            }

            if (!prevFrame.HasValue)
                this.previousFrame = frame;

            return frame;
        }

        /// <summary>
        /// Applies a low pass filter to a specific sensor data
        /// </summary>
        /// <param name="rawData">The sensor data to apply the filter to</param>
        /// <param name="previousData">The previous data for the same sensor</param>
        /// <param name="alpha">The strength of the filter to apply</param>
        /// <returns>The filtered sensor data</returns>
        private SensorData? ApplyFilter(SensorData? rawData, SensorData? previousData, float alpha) {
            if (!rawData.HasValue || !previousData.HasValue) return rawData;

            return new SensorData {
                // copy most values from rawData
                id = rawData.Value.id,
                postOffset = rawData.Value.postOffset,
                status = rawData.Value.status,
                // filter position and rotation
                position = GetFilteredValue(rawData.Value.position, previousData.Value.position, alpha),
                rotation = GetFilteredValue(rawData.Value.rotation, previousData.Value.rotation, alpha)
            };
        }

        /// <summary>
        /// Applies a low pass filter to a vector value
        /// </summary>
        /// <param name="rawValue">The vector to apply the filter to</param>
        /// <param name="previousValue">The previous data for that vector</param>
        /// <param name="alpha">The strength of the filter to apply</param>
        /// <returns>The filtered vector value</returns>
        private Vector3 GetFilteredValue(Vector3 rawValue, Vector3 previousValue, float alpha) {
            float x = GetFilteredValue(rawValue.x, previousValue.x, alpha);
            float y = GetFilteredValue(rawValue.y, previousValue.y, alpha);
            float z = GetFilteredValue(rawValue.z, previousValue.z, alpha);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Applies a low pass filter to a quaternion value
        /// </summary>
        /// <param name="rawValue">The quaternion to apply the filter to</param>
        /// <param name="previousValue">The previous data for that quaternion</param>
        /// <param name="alpha">The strength of the filter to apply</param>
        /// <returns>The filtered quaternion value</returns>
        private Quaternion GetFilteredValue(Quaternion rawValue, Quaternion previousValue, float alpha) {
            float x = GetFilteredValue(rawValue.x, previousValue.x, alpha);
            float y = GetFilteredValue(rawValue.y, previousValue.y, alpha);
            float z = GetFilteredValue(rawValue.z, previousValue.z, alpha);
            float w = GetFilteredValue(rawValue.w, previousValue.w, alpha);
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Applies a low pass filter to a number
        /// </summary>
        /// <param name="rawValue">The current number to apply the filter to</param>
        /// <param name="previousValue">The previous value for that number</param>
        /// <param name="alpha">The strength of the filter to apply</param>
        /// <returns>The filtered number</returns>
        private float GetFilteredValue(float rawValue, float previousValue, float alpha) {
            return alpha * rawValue + (1 - alpha) * previousValue;
        }
    }
}
