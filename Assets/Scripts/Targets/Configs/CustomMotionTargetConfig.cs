using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using SFB;
using System.Globalization;
using System;
using Optispeech.Targets.Controllers;
using Optispeech.Sensors;
using Optispeech.Documentation;

namespace Optispeech.Targets.Configs {

    /// <summary>
    /// Configuration interface for <see cref="CustomMotionTargetController"/>s
    /// </summary>
    public class CustomMotionTargetConfig : TargetConfig {

        /// <summary>
        /// Reference to en-US locale for making the ID dropdown in title case
        /// </summary>
        static readonly TextInfo local = new CultureInfo("en-US", false).TextInfo;

        /// <summary>
        /// Button to load a new file to use for the custom motion
        /// </summary>
        [SerializeField]
        private Button loadFile = default;
        /// <summary>
        /// Image that displays the current <see cref="CustomMotionTargetController.status"/>
        /// </summary>
        [SerializeField]
        private Image statusIndicator = default;
        /// <summary>
        /// Sprite to show when <see cref="CustomMotionTargetController.status"/> is <see cref="CustomMotionTargetController.MotionPathStatus.READY"/>
        /// </summary>
        [SerializeField]
        private Sprite readyStatusIcon = default;
        /// <summary>
        /// Sprite to show when <see cref="CustomMotionTargetController.status"/> is <see cref="CustomMotionTargetController.MotionPathStatus.PREPARING"/>
        /// </summary>
        [SerializeField]
        private Sprite preparingStatusIcon = default;
        /// <summary>
        /// Sprite to show when <see cref="CustomMotionTargetController.status"/> is <see cref="CustomMotionTargetController.MotionPathStatus.UNAVAILABLE"/>
        /// </summary>
        [SerializeField]
        private Sprite unavailableStatusIcon = default;

        /// <summary>
        /// Dropdown for <see cref="CustomMotionTargetController.id"/>
        /// </summary>
        [SerializeField]
        private TMP_Dropdown idDropdown = default;
        /// <summary>
        /// Number field for <see cref="CustomMotionTargetController.playbackSpeed"/>
        /// </summary>
        [SerializeField]
        private TMP_InputField playbackSpeedInput = default;
        /// <summary>
        /// Toggle for <see cref="CustomMotionTargetController.alternateDirection"/>
        /// </summary>
        [SerializeField]
        private Toggle alternateDirectionToggle = default;
        /// <summary>
        /// Field for <see cref="CustomMotionTargetController.offset"/>'s x component
        /// </summary>
        [SerializeField]
        private TMP_InputField offsetXInput = default;
        /// <summary>
        /// Field for <see cref="CustomMotionTargetController.offset"/>'s y component
        /// </summary>
        [SerializeField]
        private TMP_InputField offsetYInput = default;
        /// <summary>
        /// Field for <see cref="CustomMotionTargetController.offset"/>'s z component
        /// </summary>
        [SerializeField]
        private TMP_InputField offsetZInput = default;

        /// <summary>
        /// Store the currently active controller so <see cref="CustomMotionTargetController.config"/> can be set to null when this is closed
        /// </summary>
        private CustomMotionTargetController motionController = null;
        /// <summary>
        /// The panel this config is on
        /// </summary>
        private TargetsPanel panel;

        [HideInDocumentation]
        public override void Init(TargetsPanel panel, TargetController controller) {
            base.Init(panel, controller);

            this.panel = panel;
            motionController = (CustomMotionTargetController)controller;
            motionController.config = this;

            SetStatus(motionController.status);
            loadFile.onClick.AddListener(() => {
                if (motionController.loading) {
                    motionController.StopLoading();
                }
                StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", "", false, (string[] paths) => {
                    if (paths.Length != 1) return;
                    motionController.motionPath = paths[0];
                    motionController.LoadMotionPath();
                    panel.SaveTargetsToPrefs();
                });
            });
            idDropdown.value = idDropdown.options.FindIndex(d => int.Parse(d.text.Split(' ')[0]) == motionController.id);
            idDropdown.onValueChanged.AddListener(value => {
                // ID appears before the first space in the text
                if (!int.TryParse(idDropdown.options[value].text.Split(' ')[0], out motionController.id)) {
                    motionController.id = -1;
                }
                panel.SaveTargetsToPrefs();
            });
            playbackSpeedInput.text = motionController.playbackSpeed.ToString();
            playbackSpeedInput.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out motionController.playbackSpeed)) {
                    motionController.playbackSpeed = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            alternateDirectionToggle.isOn = motionController.alternateDirection;
            alternateDirectionToggle.onValueChanged.AddListener(value => {
                motionController.alternateDirection = value;
                panel.SaveTargetsToPrefs();
            });
            offsetXInput.text = motionController.offset.x.ToString();
            offsetXInput.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out motionController.offset.x)) {
                    motionController.offset.x = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            offsetYInput.text = motionController.offset.y.ToString();
            offsetYInput.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out motionController.offset.y)) {
                    motionController.offset.y = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            offsetZInput.text = motionController.offset.z.ToString();
            offsetZInput.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out motionController.offset.z)) {
                    motionController.offset.z = 0;
                }
                panel.SaveTargetsToPrefs();
            });
        }

        [HideInDocumentation]
        public override void SetInteractable(bool interactable) {
            base.SetInteractable(interactable);
            loadFile.interactable = interactable;
            idDropdown.interactable = interactable;
            playbackSpeedInput.interactable = interactable;
            alternateDirectionToggle.interactable = interactable;
            offsetXInput.interactable = interactable;
            offsetYInput.interactable = interactable;
            offsetZInput.interactable = interactable;
        }

        [HideInDocumentation]
        public override void OnClose() {
            motionController.config = null;
        }

        /// <summary>
        /// Handles updating the motion path status indicator
        /// </summary>
        /// <param name="status">The new motion path status</param>
        public void SetStatus(CustomMotionTargetController.MotionPathStatus status) {
            idDropdown.ClearOptions();
            switch (status) {
                case CustomMotionTargetController.MotionPathStatus.READY:
                    statusIndicator.sprite = readyStatusIcon;
                    statusIndicator.color = Color.green;
                    SensorConfiguration[] sensors = motionController.reader.GetSensorConfigurations(motionController.frames[0]);
                    idDropdown.AddOptions(sensors.Select(s => s.id.ToString() + " - " + local.ToTitleCase(s.type.ToString().Replace('_', ' ').ToLower())).ToList());
                    if (motionController.id == -1) {
                        // If we just loaded this file, find our initial ID to use
                        SensorConfiguration initialSelection = sensors.Where(s => s.type == SensorType.TONGUE_TIP).First();
                        if (initialSelection == null)
                            initialSelection = sensors.First();
                        int index = Array.IndexOf(sensors, initialSelection);
                        if (index != -1) {
                            // If the dropdown was already that value, onValueChanged won't trigger
                            // so we set the value without calling onValueChanged and just do it ourselves
                            idDropdown.SetValueWithoutNotify(index);
                            if (!int.TryParse(idDropdown.options[index].text.Split(' ')[0], out motionController.id)) {
                                motionController.id = -1;
                            }
                            panel.SaveTargetsToPrefs();
                        }
                    }
                    break;
                case CustomMotionTargetController.MotionPathStatus.UNAVAILABLE:
                    statusIndicator.sprite = unavailableStatusIcon;
                    statusIndicator.color = Color.red;
                    break;
                default:
                case CustomMotionTargetController.MotionPathStatus.PREPARING:
                    statusIndicator.sprite = preparingStatusIcon;
                    statusIndicator.color = Color.yellow;
                    break;
            }
        }
    }
}
