using UnityEngine;
using TMPro;
using Optispeech.Documentation;
using Optispeech.Sweeps;
using Optispeech.Profiles;
using Optispeech.Data;
using Optispeech.Targets;

namespace Optispeech.Accuracy {

    /// <summary>
    /// Renders the accuracy in the last cycle and sweep.
    /// </summary>
    public class AccuracyDisplayController : MonoBehaviour {

        /// <summary>
        /// Label used to display the accuracy in the last cycle
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI cycleLabel = default;
        /// <summary>
        /// Label used to display the accuracy in the last sweep
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI sweepLabel = default;

        /// <summary>
        /// Flag used to track whether a sweep is currently in progress.
        /// This information is used to determine whether to update the sweep
        /// label, like its supposed to every frame to show the accuracy up to
        /// this point in the sweep
        /// </summary>
        private bool inSweep = false;
        /// <summary>
        /// Since sweeps end on a separate thread, this flag is used to store the
        /// fact that a sweep has ended until the next Update, at which point the
        /// sweep animator can be told to start fading out the sweep accuracy label
        /// </summary>
        private bool sweepEnded = false;

        [HideInDocumentation]
        private void Start() {
            // Add listener for cycles ending so we can update the cycle accuracy
            // label and tell it to re-start its fade out animation
            AccuracyManager.Instance.onCycleEnd.AddListener(accuracy => UpdateAccuracyLabel(cycleLabel, accuracy));
            // Add listeners for sweeps starting and ending so we can determine whether to
            // be updating the sweep accuracy
            FileWritingManager.Instance.onSweepStart.AddListener(StartSweep);
            FileWritingManager.Instance.onSweepEnd.AddListener(StopSweep);
            DataSourceManager.Instance.onSourceChanged.AddListener(reader => UpdateVisibility());
            ProfileManager.Instance.onProfileChange.AddListener(profile => UpdateVisibility());
            UpdateVisibility();
        }

        [HideInDocumentation]
        private void Update() {
            if (inSweep) {
                UpdateAccuracyLabel(sweepLabel, AccuracyManager.Instance.GetSweepAccuracy());
            } else if (sweepEnded) {
                // We handle the sweep ending here because otherwise it happens on the file writing thread
                sweepEnded = false;
            }
        }

        /// <summary>
        /// Utility function used to update an accuracy label with a given accuracy. Handles formatting
        /// the accuracy and changing the label's text color depending on how good of an accuracy was given
        /// </summary>
        /// <param name="label">
        /// The accuracy label to update
        /// </param>
        /// <param name="accuracy">
        /// The accuracy to display in said label, from 0 to 1 where 1 means perfectly in the target the entire duration
        /// </param>
        private void UpdateAccuracyLabel(TextMeshProUGUI label, float accuracy) {
            // TODO make serialized array of thresholds and colors?
            if (accuracy > .9)
                label.color = Color.green;
            else if (accuracy > .8)
                label.color = Color.yellow;
            else if (accuracy > .6)
                label.color = new Color(1, .5f, 0);
            else
                label.color = Color.red;
            // the multiply, round, and division makes it so our accuracy is to 2 decimal places,
            // and the accuracy which is from 0 - 1 becomes stretched to 0 - 100
            int roundedAcc = Mathf.RoundToInt(accuracy * 100);
            label.text = $"{roundedAcc}%";
        }

        [HideInDocumentation]
        private void StartSweep() {
            inSweep = true;
        }

        [HideInDocumentation]
        private void StopSweep() {
            inSweep = false;
            sweepEnded = true;
        }

        [HideInDocumentation]
        private void UpdateVisibility() {
            gameObject.SetActive(TargetsManager.Instance.targets.Count > 0);
        }
    }
}
