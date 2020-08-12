using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Optispeech.Data;
using Optispeech.Targets.Configs;
using Optispeech.Data.FileReaders;
using Optispeech.Advanced;
using Optispeech.Accuracy;
using Optispeech.Documentation;

namespace Optispeech.Targets.Controllers {

    /// <summary>
    /// Controls a custom motion target, which loads sweep data and plays it back as a target
    /// </summary>
    public class CustomMotionTargetController : TargetController {

        /// <summary>
        /// Defines the different statuses possible for the motion path. Initially <see cref="MotionPathStatus.UNAVAILABLE"/>,
        /// <see cref="MotionPathStatus.PREPARING"/> while loading the path, and finally <see cref="MotionPathStatus.READY"/> once successfully
        /// loaded, or back to <see cref="MotionPathStatus.UNAVAILABLE"/> if unsuccessful
        /// </summary>
        public enum MotionPathStatus {
            READY,
            UNAVAILABLE,
            PREPARING
        };

        /// <summary>
        /// The file path of the sweep data to load the motion path from
        /// </summary>
        public string motionPath = "";
        /// <summary>
        /// The ID of the sensor from the sweep data to load the motion path from
        /// </summary>
        public int id = -1;
        /// <summary>
        /// The speed at which to playback the motion data
        /// </summary>
        public float playbackSpeed = 1;
        /// <summary>
        /// Whether or not the loaded motion should loop, or "ping pong", where it plays
        /// back in reverse then normal again, and so on
        /// </summary>
        public bool alternateDirection = true;
        /// <summary>
        /// A static offset to add to the motion path
        /// </summary>
        public Vector3 offset = Vector3.zero;

        // The fact the config needs to access so much info from the controller is telling
        // as to how much more complicated this target type is compared to the others lol

        /// <summary>
        /// The active config, when its for this controller, to display the current motion path status
        /// </summary>
        [HideInInspector]
        public CustomMotionTargetConfig config = null;
        /// <summary>
        /// The current status of the motion path
        /// </summary>
        [HideInInspector]
        public MotionPathStatus status = MotionPathStatus.UNAVAILABLE;
        /// <summary>
        /// The data frames read from the loaded sweep data
        /// </summary>
        /// <remarks>
        /// Note: when alternateDirection is false, the last frame is ignored and only used
        /// to determine how long the second to last frame should last before looping back
        /// </remarks>
        [HideInInspector]
        public List<DataFrame> frames = new List<DataFrame>();
        /// <summary>
        /// The file reader used to read the sweep data. Stored because it can provide the sensor configuration
        /// to add the appropriate data into the ID dropdown
        /// </summary>
        [HideInInspector]
        public FrameReader.FileReader reader;
        /// <summary>
        /// Whether or not sweep data is currently being loaded
        /// </summary>
        [HideInInspector]
        public bool loading = false;

        /// <summary>
        /// The duration of the loaded sweep data
        /// </summary>
        private long cycleDuration = -1;
        /// <summary>
        /// The previous data frame, if it exists. Used for applying a low pass filter to the sweep data being read
        /// </summary>
        private DataFrame? prevFrame;

        [HideInDocumentation]
        public override Vector3 GetTargetPosition(long currTime) {
            if (cycleDuration <= 0 || id == -1) return Vector3.zero;

            // Default to zeroed out sensor data
            SensorData sensorData = default;

            // Find our current frame based on currTime
            long time = currTime % GetCycleDuration();
            // Handle alternating direction
            if (time > cycleDuration) time = (2 * cycleDuration) - time;
            long targetTime = frames[0].timestamp + time;
            DataFrame frame = frames.Find(f => f.timestamp >= targetTime);

            // Search through our current data frame for our sensor
            TransformedData data = frame.transformedData = DataFrame.GetTransformedData(frame.sensorData);
            FilterManager.Instance.ApplyFilter(frame, prevFrame);
            prevFrame = frame;
            if (data.forehead.HasValue && data.forehead.Value.id == id) sensorData = data.forehead.Value;
            else if (data.jaw.HasValue && data.jaw.Value.id == id) sensorData = data.jaw.Value;
            else if (data.leftEar.HasValue && data.leftEar.Value.id == id) sensorData = data.leftEar.Value;
            else if (data.rightEar.HasValue && data.rightEar.Value.id == id) sensorData = data.rightEar.Value;
            else if (data.tongueBack.HasValue && data.tongueBack.Value.id == id) sensorData = data.tongueBack.Value;
            else if (data.tongueDorsum.HasValue && data.tongueDorsum.Value.id == id) sensorData = data.tongueDorsum.Value;
            else if (data.tongueLeft.HasValue && data.tongueLeft.Value.id == id) sensorData = data.tongueLeft.Value;
            else if (data.tongueRight.HasValue && data.tongueRight.Value.id == id) sensorData = data.tongueRight.Value;
            else if (data.tongueTip.HasValue && data.tongueTip.Value.id == id) sensorData = data.tongueTip.Value;
            else sensorData = data.otherSensors.Where(s => s.id == id).FirstOrDefault();

            // Return sensor data's position
            return sensorData.position + sensorData.postOffset + offset;
        }

        [HideInDocumentation]
        public override long GetCycleDuration() {
            // Note we double the duration if alternating direction because, if we do,
            // then one cycle is really one forwards then one in reverse
            return (alternateDirection ? 2 : 1) * cycleDuration;
        }

        [HideInDocumentation]
        public override void ApplyConfigFromString(string config) {
            Debug.Log(config);
            base.ApplyConfigFromString(config);
            string[] values = config.Split('\t');
            if (values.Length < NUM_BASE_CONFIG_VALUES + 7)
                return;
            motionPath = values[NUM_BASE_CONFIG_VALUES];
            int.TryParse(values[NUM_BASE_CONFIG_VALUES + 1], out id);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 2], out playbackSpeed);
            bool.TryParse(values[NUM_BASE_CONFIG_VALUES + 3], out alternateDirection);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 4], out offset.x);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 5], out offset.y);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 6], out offset.z);

            LoadMotionPath(false);
        }

        [HideInDocumentation]
        public override string ToString() {
            return base.ToString() + "\t" + motionPath + "\t" + id +
                "\t" + playbackSpeed + "\t" + alternateDirection +
                "\t" + offset.x + "\t" + offset.y + "\t" + offset.z;
        }

        /// <summary>
        /// Attempts to load sweep data
        /// </summary>
        /// <param name="resetId">Whether or not to set id to -1 before attempting to read the sweep data</param>
        public void LoadMotionPath(bool resetId = true) {
            // Stop loading motion if we're in the process of doing so
            if (loading) {
                StopLoading();
                return;
            }

            cycleDuration = -1;
            if (resetId) id = -1;
            frames.Clear();
            reader = null;
            prevFrame = null;
            loading = true;
            status = MotionPathStatus.PREPARING;
            if (config != null)
                config.SetStatus(status);

            // Read in coroutine so we don't lock the main thread
            // We'll leave cycleDuration as -1 and disable loading another motion path
            //  until this is complete
            StartCoroutine(FrameReader.Instance.ReadFile(motionPath, false, (reader, frames) => {
                if (frames.Count != 0) {
                    this.frames = frames;
                    cycleDuration = frames[frames.Count - 1].timestamp - frames[0].timestamp;
                    this.reader = reader;
                    Finish(MotionPathStatus.READY);
                } else {
                    Finish(MotionPathStatus.UNAVAILABLE);
                }
            }, () => Finish(MotionPathStatus.UNAVAILABLE)));
        }
         
        /// <summary>
        /// Stops loading any sweep data and resets motion path
        /// </summary>
        public void StopLoading() {
            StopAllCoroutines();
            Finish(MotionPathStatus.UNAVAILABLE);
        }

        /// <summary>
        /// Handles loading sweep data ending, either successfully or not
        /// </summary>
        /// <param name="status">The resulting status of the load attempt</param>
        private void Finish(MotionPathStatus status) {
            loading = false;
            this.status = status;
            if (config != null)
                config.SetStatus(status);

            // Update accuracy display with our new cycle duration
            AccuracyManager.Instance.CalculateDurationLCM();
        }
    }
}
