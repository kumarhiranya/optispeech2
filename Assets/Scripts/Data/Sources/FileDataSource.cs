using Optispeech.Data.FileReaders;
using Optispeech.Documentation;
using Optispeech.Advanced;
using Optispeech.Sensors;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Optispeech.Data.Sources {

    /// <summary>
    /// A data source reader that reads from a local file.
    /// This reader will read a file in a variety of different formats, load the whole file,
    /// and then allow the user to playback that data, including a panel where they can pause
    /// or jump around the file
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FileDataSource : DataSourceReader {

        /// <summary>
        /// A button to open the file dialog to choose a file to load
        /// </summary>
        [SerializeField]
        private Button loadFile = default;
        /// <summary>
        /// The label on the load file button, used to change its text when its currently loading a file
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI loadFileLabel = default;
        /// <summary>
        /// The button used to toggle whether the file is being played back at the moment
        /// </summary>
        [SerializeField]
        private Button togglePauseButton = default;
        /// <summary>
        /// The sprite to show on the <see cref="togglePauseButton"/> when the button will pause the playback
        /// </summary>
        [SerializeField]
        private Sprite pauseSprite = default;
        /// <summary>
        /// The sprite to show on the <see cref="togglePauseButton"/> when the button will resume the playback
        /// </summary>
        [SerializeField]
        private Sprite resumeSprite = default;
        /// <summary>
        /// A progress slider used to show how far along the file the current playback is, which can be changed
        /// by the user to move forward or back along the file
        /// </summary>
        [SerializeField]
        private Slider progressSlider = default;
        /// <summary>
        /// A label used to show how long the loaded file is
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI durationLabel = default;

        /// <summary>
        /// Whether or not the playback is currently paused
        /// </summary>
        private bool paused = true;
        /// <summary>
        /// The frames read from the file
        /// </summary>
        private List<DataFrame> frames = new List<DataFrame>();
        /// <summary>
        /// The index in <see cref="frames"/> of the next data frame
        /// </summary>
        private int nextFrame;
        /// <summary>
        /// The file reader used to read the currently selected file
        /// </summary>
        private FrameReader.FileReader reader;

        /// <summary>
        /// An audio source used to playback any loaded audio files
        /// </summary>
        private new AudioSource audio;

        /// <summary>
        /// The image on <see cref="togglePauseButton"/>, used to change what sprite its using based on the state of <see cref="paused"/>
        /// </summary>
        private Image togglePauseImage;

        [HideInDocumentation]
        private new void OnEnable() {
            base.OnEnable();
            audio = GetComponent<AudioSource>();
            togglePauseImage = togglePauseButton.GetComponent<Image>();
            togglePauseButton.interactable = false;
            togglePauseButton.onClick.AddListener(TogglePause);
            progressSlider.interactable = false;
            progressSlider.onValueChanged.AddListener(SetTime);
        }

        [HideInDocumentation]
        private void Update() {
            // We update the progress slider here isntead of on ReadFrame because we can't update
            // UI objects from other threads
            if (!paused && frames.Count > 0 && nextFrame < frames.Count) {
                DataFrame frame = frames[nextFrame];
                progressSlider.SetValueWithoutNotify(frame.timestamp - frames[0].timestamp);

                // Re-create our sensors list if necessary
                if (frame.sensorData.Length != SensorsManager.Instance.sensors.Length) {
                    SensorsManager.Instance.SetSensors(reader.GetSensorConfigurations(frame));
                }
            }
        }

        [HideInDocumentation]
        protected override void StartThread() {
            base.StartThread();
            loadFile.onClick.AddListener(ReadFile);
        }

        [HideInDocumentation]
        public override DataSourceReaderStatus GetCurrentStatus() {
            // The file data source isn't dependent on anything so its always available
            return DataSourceReaderStatus.AVAILABLE;
        }

        [HideInDocumentation]
        public override bool AreTargetsConfigurable() {
            // We don't want to save/restore targets from our profile,
            // but rather from the loaded files instead
            return true;
        }

        [HideInDocumentation]
        public override bool AreSensorsConfigurable() {
            // We don't want to save/restore sensors from our profile,
            // but rather from the loaded files instead
            return false;
        }

        [HideInDocumentation]
        protected override DataFrame ReadFrame() {
            while (paused || frames.Count == 0 || nextFrame >= frames.Count)
                Thread.Sleep(100);

            DataFrame frame = frames[nextFrame];

            // If its not the first frame of this file,
            // sleep the elapsed duration between the two frames
            if (nextFrame > 0)
                Thread.Sleep((int)(frame.timestamp - frames[nextFrame - 1].timestamp));

            nextFrame++;
            return frame;
        }

        /// <summary>
        /// Toggles whether or not ReadFrame is currently stalling or returning frames
        /// </summary>
        private void TogglePause() {
            if (paused) {
                paused = false;
                togglePauseImage.sprite = pauseSprite;
                audio.Play();
            } else {
                paused = true;
                togglePauseImage.sprite = resumeSprite;
                audio.Pause();
            }
        }

        /// <summary>
        /// Sets how far along the file playback is
        /// </summary>
        /// <param name="value">The time in ms to change playback to</param>
        private void SetTime(float value) {
            if (frames.Count == 0) return;
            float targetTime = frames[0].timestamp + value;
            if (audio.clip != null) {
                if (!audio.isPlaying) audio.Play();
                audio.time = value / 1000f;
            }
            for (int i = 0; i < frames.Count; i++) {
                if (frames[i].timestamp >= targetTime) {
                    nextFrame = i;
                    break;
                }
            }
            FilterManager.Instance.previousFrame = null;
        }

        /// <summary>
        /// Opens a file dialog and reads the selected file
        /// </summary>
        private void ReadFile() {
            StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", "", false, (string[] paths) => {
                if (paths.Length != 1) return;

                // Disable parts of interface while we're loading the file
                paused = true;
                audio.Stop();
                loadFile.interactable = false;
                togglePauseImage.sprite = resumeSprite;
                togglePauseButton.interactable = false;
                progressSlider.interactable = false;
                loadFileLabel.text = "Loading file...";
                durationLabel.text = "";

                StartCoroutine(FrameReader.Instance.ReadFile(paths[0], true, (reader, frames) => StartCoroutine(StartPlayback(paths[0], reader, frames)), Reset));
            });
        }

        /// <summary>
        /// Starts playing back a file. Returns a coroutine so it can attempt to load the audio file for this data file
        /// </summary>
        /// <param name="filename">The location of the file to play back</param>
        /// <param name="reader">The reader used to read the file</param>
        /// <param name="frames">The data frames read from the file</param>
        /// <returns>A coroutine that will attempt to load the audio file for this file</returns>
        private IEnumerator StartPlayback(string filename, FrameReader.FileReader reader, List<DataFrame> frames) {
            // Load wav audio if it exists
            string audioFile = reader.GetAudioFile(filename);
            if (File.Exists(audioFile)) {
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioFile, AudioType.WAV)) {
                    // Send request
                    yield return www.SendWebRequest();
                    // Wait until the whole file is downloaded
                    while (!www.isDone) yield return null;
                    // Put the downloaded AudioClip in our AudioSource
                    audio.clip = DownloadHandlerAudioClip.GetContent(www);
                    // Play it back
                    audio.Play();
                }
            }

            // Reset interface
            loadFileLabel.text = "Load File";
            loadFile.interactable = true;

            if (frames.Count > 0) {
                // Set initial sensors configuration
                SensorsManager.Instance.SetSensors(reader.GetSensorConfigurations(frames[0]));
                // Start playback
                paused = false;
                nextFrame = 0;
                FilterManager.Instance.previousFrame = null;
                this.reader = reader;
                this.frames = frames;
                togglePauseImage.sprite = pauseSprite;
                togglePauseButton.interactable = true;
                progressSlider.interactable = true;
                // Create a timespan by multiplying the ms by 10000 to get ticks
                TimeSpan duration = new TimeSpan((frames[frames.Count - 1].timestamp - frames[0].timestamp) * 10000);
                // Make the progrss slider's max value the duration in ms
                progressSlider.maxValue = (float)duration.TotalMilliseconds;
                // Make the duration label should minutes and seconds
                durationLabel.text = Mathf.FloorToInt((float)duration.TotalMinutes) + ":" + duration.Seconds.ToString().PadLeft(2, '0');
            } else {
                durationLabel.text = "0:00";
            }
        }

        /// <summary>
        /// Resets the loaded file so a new one can be loaded
        /// </summary>
        private void Reset() {
            paused = true;
            audio.Stop();
            loadFile.interactable = true;
            togglePauseImage.sprite = resumeSprite;
            togglePauseButton.interactable = false;
            progressSlider.interactable = false;
            loadFileLabel.text = "Load File";
            durationLabel.text = "";
        }
    }
}
