using Optispeech.Documentation;
using Optispeech.Profiles;
using SFB;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Sweeps {

    /// <summary>
    /// Panel for starting and stopping sweeps and configuring where and what to write during the sweep
    /// </summary>
    public class SweepsPanel : MonoBehaviour {

        /// <summary>
        /// Input field to set the folder to write sweep data into
        /// </summary>
        [SerializeField]
        private TMP_InputField folderPathInput = default;
        /// <summary>
        /// Button that opens a file browser to find a folder to save
        /// </summary>
        [SerializeField]
        private Button browserButton = default;
        /// <summary>
        /// Input field to set the name of the sweep, which is used as part of the filename each sweep data file is written to
        /// </summary>
        [SerializeField]
        private TMP_InputField sweepNameInput = default;
        /// <summary>
        /// Input field to set the duration before a sweep automatically stops
        /// </summary>
        [SerializeField]
        private TMP_InputField autoStopDurationInput = default;
        /// <summary>
        /// Toggle to determine whether or not to save raw sweep data
        /// </summary>
        [SerializeField]
        private Toggle saveRawToggle = default;
        /// <summary>
        /// Toggle to determine whether or not to save transformed sweep data
        /// </summary>
        [SerializeField]
        private Toggle saveTransformedToggle = default;
        /// <summary>
        /// Toggle to determine whether or not to save transformed sweep data without post-offsets
        /// </summary>
        [SerializeField]
        private Toggle saveTransformedWithoutOffsetsToggle = default;
        /// <summary>
        /// Toggle to determine whether or not to save audio data
        /// </summary>
        [SerializeField]
        private Toggle saveSyncedAudioToggle = default;
        /// <summary>
        /// Toggle to determine whether or not to save raw sweep data
        /// </summary>
        [SerializeField]
        private Image toggleSweepImage = default;
        /// <summary>
        /// Button to start and stop sweep recordings
        /// </summary>
        [SerializeField]
        private Button toggleSweepButton = default;
        /// <summary>
        /// Label to display if <see cref="toggleSweepButton"/> will start or stop a sweep
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI toggleSweepText = default;
        /// <summary>
        /// During a sweep, this label will display how long its recorded for
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI durationText = default;

        /// <summary>
        /// How long to wait (in ms) before checking for a new data frame while recording sweeps
        /// if set too high then it'll process frames until it catches up
        /// </summary>
        public int pollFrequency = 25;

        [HideInDocumentation]
        private void Start() {
            // Set initial values onto our input fields
            LoadProfile(ProfileManager.Instance.ActiveProfile);
            // Make it load new profiles when they're loaded
            ProfileManager.Instance.onProfileChange.AddListener(LoadProfile);

            // Add listeners to each of our input fields to save settings back to player prefs
            // As well as listeners to change the variable
            folderPathInput.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((string value, ref ProfileManager.Profile profile) => profile.sweepFolder = value));
            sweepNameInput.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((string value, ref ProfileManager.Profile profile) => profile.sweepName = value));
            autoStopDurationInput.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((int value, ref ProfileManager.Profile profile) => profile.autoStopDuration = value));
            saveRawToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.saveRaw = value));
            saveTransformedToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.saveTransformed = value));
            saveTransformedWithoutOffsetsToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.saveTransformedWithoutOffsets = value));
            saveSyncedAudioToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.saveAudio = value));

            // Add listener so the folder icon opens a folder selection panel
            browserButton.onClick.AddListener(BrowseFolderPath);

            // Add button to start/stop sweeps
            toggleSweepButton.onClick.AddListener(FileWritingManager.Instance.ToggleSweep);
            toggleSweepImage.color = Color.green;
            toggleSweepText.text = "Start Sweep";

            // Disable save audio toggle if no microphones are detected
            saveSyncedAudioToggle.interactable = Microphone.devices.Length > 0;
        }

        [HideInDocumentation]
        private void LoadProfile(ProfileManager.Profile profile) {
            folderPathInput.SetTextWithoutNotify(profile.sweepFolder);
            sweepNameInput.SetTextWithoutNotify(profile.sweepName);
            autoStopDurationInput.SetTextWithoutNotify(profile.autoStopDuration == -1 ? "" : profile.autoStopDuration.ToString());
            saveRawToggle.SetIsOnWithoutNotify(profile.saveRaw);
            saveTransformedToggle.SetIsOnWithoutNotify(profile.saveTransformed);
            saveTransformedWithoutOffsetsToggle.SetIsOnWithoutNotify(profile.saveTransformedWithoutOffsets);
            saveSyncedAudioToggle.SetIsOnWithoutNotify(profile.saveAudio);
        }

        /// <summary>
        /// Updates the time display with the new amount of elapsed time
        /// </summary>
        /// <param name="elapsedTime">The amount of time since the start of the sweep</param>
        public void UpdateDuration(TimeSpan elapsedTime) {
            durationText.text = elapsedTime.Minutes + ":" + elapsedTime.Seconds + "." + elapsedTime.Milliseconds;
        }

        /// <summary>
        /// Updates the panel to display that we're finishing a sweep
        /// </summary>
        public void DisplayFinishing() {
            toggleSweepImage.color = Color.yellow;
            toggleSweepText.text = "Finishing...";
        }

        /// <summary>
        /// Updates the panel to display that we're currently recording
        /// </summary>
        public void DisplayRecording() {
            folderPathInput.interactable = false;
            browserButton.interactable = false;
            sweepNameInput.interactable = false;
            autoStopDurationInput.interactable = false;
            saveRawToggle.interactable = false;
            saveTransformedToggle.interactable = false;
            saveTransformedWithoutOffsetsToggle.interactable = false;
            saveSyncedAudioToggle.interactable = false;
            toggleSweepImage.color = Color.red;
            toggleSweepText.text = "Stop Sweep";
        }

        /// <summary>
        /// Resets the panel to non-recording, non-finishing state
        /// </summary>
        public void ResetDisplay() {
            folderPathInput.interactable = true;
            browserButton.interactable = true;
            sweepNameInput.interactable = true;
            autoStopDurationInput.interactable = true;
            saveRawToggle.interactable = true;
            saveTransformedToggle.interactable = true;
            saveTransformedWithoutOffsetsToggle.interactable = true;
            saveSyncedAudioToggle.interactable = Microphone.devices.Length > 0;
            toggleSweepImage.color = Color.green;
            toggleSweepText.text = "Start Sweep";
        }

        /// <summary>
        /// Opens a file browser window and sets <see cref="folderPathInput"/> to the chosen folder path
        /// </summary>
        private void BrowseFolderPath() {
            string[] selected = StandaloneFileBrowser.OpenFolderPanel("Open Folder", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), false);
            if (selected.Length > 0)
                folderPathInput.text = selected[0];
        }
    }
}
