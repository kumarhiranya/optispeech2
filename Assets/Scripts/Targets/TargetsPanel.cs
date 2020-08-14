using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Optispeech.UI;
using Optispeech.Profiles;
using Optispeech.Data;
using Optispeech.Documentation;

namespace Optispeech.Targets {

    /// <summary>
    /// Panel that shows the current targets and allows them to be added, removed, and edited
    /// </summary>
    public class TargetsPanel : MonoBehaviour {

        /// <summary>
        /// This variable is used to track whether the targets panel can currently
        /// be edited. If not, targets cannot be added, removed, or modified by the user;
        /// Usually because there either isn't an active data source, or the data source
        /// has indicated that it'll be controlling the targets instead of the user
        /// </summary>
        private bool isInteractable = false;

        /// <summary>
        /// Container to place the active config prefab inside
        /// </summary>
        [SerializeField]
        private RectTransform configContainer = default;
        /// <summary>
        /// Dropdown to select the type of the new target to add
        /// </summary>
        [SerializeField]
        private TMP_Dropdown addTargetDropdown = default;
        /// <summary>
        /// Each target gets a toggle to open/close its config; this is the container all those toggles are placed in
        /// </summary>
        [SerializeField]
        private ToggleGroup targetToggleContainer = default;
        /// <summary>
        /// Prefab that is instantiated for each target. Each toggles which target is currently being configured
        /// </summary>
        [SerializeField]
        private Toggle targetTogglePrefab = default;

        /// <summary>
        /// List of all the current target descriptions
        /// </summary>
        private TargetDescription[] targets;
        /// <summary>
        /// Which target config is currently open
        /// </summary>
        private GameObject activeConfig = null;
        /// <summary>
        /// When <see cref="activeConfig"/> is not null, this will be the controller that config is associated with
        /// </summary>
        private TargetController activeController = null;

        /// <summary>
        /// This panel
        /// </summary>
        private TogglePanel panel;
        /// <summary>
        /// Flag to ensure the panelToggledEvent event listener is only added once
        /// </summary>
        private bool addedPanelEvent = false;

        /// <summary>
        /// Property that handles updating the interactivity of the active target config
        /// </summary>
        public bool IsInteractable {
            get => isInteractable;
            set {
                isInteractable = value;

                // Enable/Disable adding new targets
                addTargetDropdown.interactable = value;

                // Enable/Disable removing or modifying currently active config
                if (activeConfig != null)
                    activeConfig.GetComponent<TargetConfig>().SetInteractable(value);
            }
        }

        [HideInDocumentation]
        private void Awake() {
            targets = Resources.LoadAll("Target Type Descriptions", typeof(TargetDescription)).Cast<TargetDescription>().ToArray();

            // Add each target to the dropdown
            // Note it starts with an initial, and hidden, placeholder option we use so they can select any type any number of times
            addTargetDropdown.onValueChanged.AddListener(AddTarget);
            for (int i = 0; i < targets.Length; i++) {
                addTargetDropdown.options.Add(new TMP_Dropdown.OptionData(targets[i].typeName));
            }
            addTargetDropdown.options.Add(new TMP_Dropdown.OptionData("Add from clipboard"));
            addTargetDropdown.interactable = isInteractable;
        }

        [HideInDocumentation]
        private void OnEnable() {
            panel = configContainer.GetComponentInParent<TogglePanel>();

            if (addedPanelEvent) return;
            panel.panelToggledEvent.AddListener((isOpen, heightDifference) => {
                if (activeConfig == null) return;
                if (isOpen) activeConfig.GetComponent<TargetConfig>().OnOpen();
                else activeConfig.GetComponent<TargetConfig>().OnClose();
            });
            addedPanelEvent = true;
        }

        /// <summary>
        /// Adds a target with the type specified by the given index to the targets manager
        /// </summary>
        /// <param name="index">The index in <see cref="targets"/> of the target type this new target should have</param>
        private void AddTarget(int index) {
            // If its the first item then we're pasting the target from clipboard
            if (index == addTargetDropdown.options.Count - 1) {
                string[] values = GUIUtility.systemCopyBuffer.Split('\t');
                if (values.Length >= 2) {
                    string id = values[0];
                    string typeName = values[1];

                    // Create target
                    TargetDescription description = targets.Where(t => t.typeName == typeName).FirstOrDefault();
                    id = TargetsManager.Instance.AddTarget(description);
                    values[0] = id;
                    TargetsManager.Instance.targets[id].ApplyConfigFromString(string.Join("\t", values));
                }
            } else {
                // We subtract 1 from index to account for the placeholder item
                TargetsManager.Instance.AddTarget(targets[index - 1]);
            }
            // Set the placeholder item as our selected value again, so any type target
            // can be selected with it registering as a change
            addTargetDropdown.SetValueWithoutNotify(0);
        }

        /// <summary>
        /// Adds a target toggle to the list
        /// </summary>
        /// <param name="controller">Target this toggle belongs to</param>
        /// <param name="manuallyAdded">Whether or not this target has been loaded or created</param>
        public void AddTarget(TargetController controller, bool manuallyAdded = true) {
            GameObject targetToggle = Instantiate(targetTogglePrefab.gameObject, targetToggleContainer.transform);
            controller.configToggle = targetToggle;
            Toggle toggle = targetToggle.GetComponent<Toggle>();
            toggle.group = targetToggleContainer;
            toggle.onValueChanged.AddListener(value => {
                if (value) OpenConfig(controller);
                else CloseConfig();
            });
            targetToggle.GetComponentInChildren<TextMeshProUGUI>().text = controller.targetId;
            if (manuallyAdded) {
                toggle.isOn = true;
                SaveTargetsToPrefs();
            }
        }

        /// <summary>
        /// Opens a specified target config
        /// </summary>
        /// <param name="targetToggle">The toggle for the target being opened</param>
        /// <param name="controller">The target to open the config for</param>
        private void OpenConfig(TargetController controller) {
            CloseConfig();
            StartCoroutine(DelayedOpen(controller));
        }

        /// <summary>
        /// Closes the currently open target config, if any
        /// </summary>
        public void CloseConfig() {
            if (activeConfig != null) {
                activeController.OnClose();
                activeConfig.GetComponent<TargetConfig>().OnClose();
                DestroyImmediate(activeConfig);
                activeConfig = null;
                panel.Refresh();
                StartCoroutine(DelayedRefresh());
            }
        }

        /// <summary>
        /// Creates a coroutine that waits a frame then refreshes this panel a second time,
        /// when all the UI elements have finished updating
        /// (for some reason it wouldn't update the height correctly before a frame passed)
        /// </summary>
        /// <returns>A coroutine</returns>
        private IEnumerator DelayedRefresh() {
            yield return null;
            panel.Refresh();
        }

        /// <summary>
        /// Creates a coroutine that waits a frame before opening a target config, so this panel can start closing if necessary,
        /// because otherwise there are issues with the height being slightly miscalculated
        /// </summary>
        /// <param name="targetToggle">The toggle for the target being opened</param>
        /// <param name="controller">The target to open the config for</param>
        /// <returns>A coroutine</returns>
        private IEnumerator DelayedOpen(TargetController controller) {
            yield return null;
            controller.OnOpen();
            activeController = controller;
            // TODO use one game object per target type and just set it active and Init() the appropriate one here
            activeConfig = Instantiate(controller.description.targetConfigPrefab.gameObject, configContainer);
            TargetConfig config = activeConfig.GetComponent<TargetConfig>();
            config.Init(this, controller);
            config.SetInteractable(isInteractable);
            config.OnOpen();
            panel.Refresh();
            yield return DelayedRefresh();
        }

        /// <summary>
        /// Create config strings for each target and save it to the active profile
        /// </summary>
        public void SaveTargetsToPrefs() {
            if (!isInteractable) return;

            // Construct string of all targets, one target per line
            string targets = "";
            foreach (KeyValuePair<string, TargetController> kvp in TargetsManager.Instance.targets)
                targets += "\n" + kvp.Value.ToString();
            // Remove first newline character
            if (TargetsManager.Instance.targets.Count > 0)
                targets = targets.Substring(1);

            // Save to profile
            ProfileManager.Instance.ActiveProfile.targets[DataSourceManager.Instance.dataSourceName] = targets;
            ProfileManager.Instance.UpdateProfile(ProfileManager.Instance.ActiveProfile);
        }
    }
}
