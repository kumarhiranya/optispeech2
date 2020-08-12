using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using Optispeech.Data;
using Optispeech.Profiles;
using Optispeech.Sweeps;
using Optispeech.Targets;
using Optispeech.Documentation;

namespace Optispeech.Accuracy {

    /// <summary>
    /// Tracks how closely the data matches any registered targets over
    /// the last cycle and the current sweep.
    /// A cycle is a period of time equal to the Lowest Common Multiple
    /// of the durations of each target.
    /// For example if you have a target that takes 2 seconds to loop and
    /// one that takes 3, then the cycle would be each 6 seconds
    /// </summary>
    public class AccuracyManager : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static AccuracyManager Instance = default;

        /// <summary>
        /// Event that fires whenever a cycle ends
        /// </summary>
        [HideInInspector]
        public UnityEvent<float> onCycleEnd = new UnityEvent<float>();

        /// <summary>
        /// How long one cycle lasts
        /// </summary>
        private long cycleDuration = 0;
        /// <summary>
        /// How far we are in the current cycle
        /// </summary>
        private long currTime = 0;
        /// <summary>
        /// A sum of the accuracies of all targets thus far in the cycle
        /// </summary>
        private float sumAccuracy = 0;
        /// <summary>
        /// How many accuracies have been added into sumAccuracy. We store this
        /// so we can divide the sumAccuracy to get our average accuracy,
        /// even though we don't know how many measurements we'll have until
        /// the cycle ends
        /// </summary>
        private int measurementsCount = 0;
        /// <summary>
        /// Reference to the previous frame, to calculate how much time has passed
        /// (note that the current data source's "lastFrame" is effectively our current frame)
        /// </summary>
        private DataFrame? lastFrame = null;

        /// <summary>
        /// A sum of the accuracies of all targets thus far in the sweep
        /// </summary>
        private float sweepSumAccuracy = 0;
        /// <summary>
        /// How many accuracies have been added into sweepSumAccuracy.
        /// </summary>
        /// <see cref="measurementsCount"/>
        private int sweepMeasurementsCount = 0;

        /// <summary>
        /// Since frames are processed in a different thread, this is used tostore
        /// the fact the cycle has ended until the next Update, where the cycle ending 
        /// can be handled properly
        /// </summary>
        private bool cycleEnded = false;

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
        private void Start() {
            ProfileManager.Instance.onProfileChange.AddListener(profile => CalculateDurationLCM());
            DataSourceManager.Instance.onSourceChanged.AddListener(source => CalculateDurationLCM());
            FileWritingManager.Instance.onSweepStart.AddListener(StartSweep);
            DataSourceManager.Instance.onFrameRead.AddListener(ProcessFrame);
        }

        [HideInDocumentation]
        private void Update() {
            if (cycleEnded) {
                cycleEnded = false;
                onCycleEnd.Invoke(measurementsCount > 0 ? sumAccuracy / measurementsCount : 0);
                currTime = 0;
                sumAccuracy = 0;
                measurementsCount = 0;
            }
        }

        /// <summary>
        /// Calculates and updates the the duration of a cycle by finding the
        /// Lowest Common Multiple of all the targets' cycles
        /// </summary>
        public void CalculateDurationLCM() {
            if (TargetsManager.Instance.targets.Count == 0) {
                currTime = 0;
                // Approximately 10 million years
                cycleDuration = long.MaxValue;
                return;
            }

            // Cycle duration is determined by the lowest common multiple
            // of each target's cycle duration.
            // We actually ignore the profile because we don't want to parse
            // individual targets, we just use this event because any time
            // the targets change this will be called
            long newCycleDuration = TargetsManager.Instance.targets
                .Select(kvp => kvp.Value.GetCycleDuration())
                .Aggregate((a, b) => a <= 0 ? b : b <= 0 ? a : Math.Abs(a * b) / GCD(a, b));
            newCycleDuration = Math.Max(0, newCycleDuration);

            // If the targets changed then reset the cycle
            if (newCycleDuration != cycleDuration) {
                currTime = 0;
                cycleDuration = newCycleDuration;
            }
        }

        /// <summary>
        /// Utility function for safely calculating sweep accuracy even if there is no data
        /// </summary>
        /// <returns>The average sweep accuracy from 0 to 1</returns>
        public float GetSweepAccuracy() {
            return sweepMeasurementsCount > 0 ? sweepSumAccuracy / sweepMeasurementsCount : 0;
        }

        /// <summary>
        /// "Processes" a frame by calculating the accuracy for each target and adding them to the current
        /// sweep and cycle sumAccuracy counts, as well as updating the measurementsCount for each
        /// </summary>
        /// <param name="frame">The data frame to process</param>
        private void ProcessFrame(DataFrame frame) {
            if (lastFrame.HasValue) {
                // update currTime if this isn't the first frame
                // and don't go backwards in time within a cycle
                currTime += Math.Max(0, frame.timestamp - lastFrame.Value.timestamp);
                if (currTime >= cycleDuration) cycleEnded = true;
            }

            if (!frame.transformedData.tongueTip.HasValue)
                // can't process frame without a tongue tip sensor
                // TODO allow targets to use other sensors?
                return;

            // add the accuracy of each target to our overall accuracy
            foreach (KeyValuePair<string, Vector3> kvp in frame.targetPositions) {
                // 1 means the sensor is within the target's radius
                // 0 means the sensor is outside the target's radius * 2
                float radius = TargetsManager.Instance.targets[kvp.Key].radius;
                Vector3 tipPosition = frame.transformedData.tongueTip.Value.position + frame.transformedData.tongueTip.Value.postOffset;
                float distance = Vector3.Distance(tipPosition, kvp.Value) - radius;
                float accuracy = 1 - Mathf.Clamp01(distance / radius);
                // Sanity check for improper data
                if (float.IsNaN(accuracy)) continue;
                sumAccuracy += accuracy;
                sweepSumAccuracy += accuracy;
            }

            measurementsCount += frame.targetPositions.Count;
            sweepMeasurementsCount += frame.targetPositions.Count;
            lastFrame = frame;
        }

        [HideInDocumentation]
        private void StartSweep() {
            sweepSumAccuracy = 0;
            sweepMeasurementsCount = 0;
            lastFrame = null;
        }

        /// <summary>
        /// Utility function for calculating the Greatest Common Denominator
        /// of two numbers, which is used when calculating LCM
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>The greatest common denominator of <paramref name="a"/> and <paramref name="b"/></returns>
        private long GCD(long a, long b) {
            return b == 0 ? a : GCD(b, a % b);
        }
    }
}
