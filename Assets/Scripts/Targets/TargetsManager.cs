using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using Optispeech.Documentation;
using Optispeech.Data;
using Optispeech.Profiles;
using UnityEngine.UI;
using System.Linq;
using System.Windows.Forms;
using Optispeech.UI;

namespace Optispeech.Targets {

    /// <summary>
    /// Managers the configured targets
    /// </summary>
    public class TargetsManager : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static TargetsManager Instance = default;

        /// <summary>
        /// Dictionary of target IDs to their controllers
        /// </summary>
        [HideInInspector]
        public Dictionary<string, TargetController> targets = new Dictionary<string, TargetController>();

        /// <summary>
        /// The color a target marker should be when the sensor is within the target radius
        /// </summary>
        public Color closeColor = default;
        /// <summary>
        /// The color a target marker should be when the sensor is far away from the target
        /// </summary>
        public Color farColor = default;
        /// <summary>
        /// The targets panel, used for clearing it when clearing all targets
        /// </summary>
        public TargetsPanel panel = default;

        /// <summary>
        /// A list of all the different target type descriptions, which is used to load the targets for the specific
        /// data source whenever its changed
        /// </summary>
        private TargetDescription[] targetDescriptions;

        [HideInDocumentation]
        void Awake() {
            if (Instance == null || Instance == this) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }

            targetDescriptions = Resources.LoadAll("Target Type Descriptions", typeof(TargetDescription)).Cast<TargetDescription>().ToArray();
        }

        /// <summary>
        /// Adds a target with the given description
        /// </summary>
        /// <param name="description">The description of the target to add</param>
        /// <param name="manuallyAdded">Whether this target was loaded or created by the user just now</param>
        /// <returns>The ID of the created target</returns>
        public string AddTarget(TargetDescription description, bool manuallyAdded = true) {
            GameObject target = Instantiate(description.targetPrefab.gameObject);
            TargetController controller = target.GetComponent<TargetController>();
            controller.description = description;
            controller.targetId = description.typeName;
            controller.transform.localScale = new Vector3(controller.radius, controller.radius, controller.radius);
            target.transform.position = controller.GetTargetPosition(0);
            int i = 2;
            while (targets.ContainsKey(controller.targetId))
                controller.targetId = description.typeName + " " + (i++);
            Tooltip tooltip = target.GetComponentInChildren<Tooltip>();
            if (tooltip) tooltip.SetText(controller.targetId);
            targets.Add(controller.targetId, controller);
            panel.AddTarget(controller, manuallyAdded);
            return controller.targetId;
        }

        /// <summary>
        /// Updates targets to the ones in the ActiveProfile
        /// </summary>
        public void ResetTargets() {
            if (DataSourceManager.Instance.dataSourceReader == null) {
                foreach (KeyValuePair<string, TargetController> kvp in targets) {
                    RemoveTarget(kvp.Value);
                }
                targets.Clear();
                return;
            }

            panel.IsInteractable = DataSourceManager.Instance.dataSourceReader.AreTargetsConfigurable();
            if (panel.IsInteractable) {
                if (ProfileManager.Instance.ActiveProfile.targets.TryGetValue(DataSourceManager.Instance.dataSourceName, out string targetsString)) {
                    string[] targets = targetsString.Split('\n');
                    foreach (string target in targets) {
                        if (target == "") continue;
                        
                        string[] values = target.Split('\t');
                        string id = values[0];
                        string typeName = values[1];

                        // Check if a target with this ID already exists
                        if (this.targets.ContainsKey(id)) {
                            // Ignore this target if it hasn't changed
                            if (this.targets[id].ToString() == target) continue;

                            // If its the same type of target, just apply the new config
                            if (this.targets[id].ToString().Split('\t')[1] == typeName) {
                                this.targets[id].ApplyConfigFromString(target);
                                continue;
                            }

                            // Remove this target so the new one can be created
                            RemoveTarget(this.targets[id]);
                        }

                        // Create target
                        TargetDescription description = targetDescriptions.Where(t => t.typeName == typeName).FirstOrDefault();
                        id = AddTarget(description, false);
                        this.targets[id].ApplyConfigFromString(target);
                    }

                    // Remove any other targets currently loaded that aren't in the active profile
                    string[] targetIDs = targets.Select(s => s.Split('\t')[0]).ToArray();
                    foreach (KeyValuePair<string, TargetController> kvp in this.targets.Where(kvp => !targetIDs.Contains(kvp.Key)).ToArray()) {
                        RemoveTarget(kvp.Value);
                        this.targets.Remove(kvp.Key);
                    }
                } else {
                    foreach (KeyValuePair<string, TargetController> kvp in targets) {
                        RemoveTarget(kvp.Value);
                    }
                    targets.Clear();
                }
            }
        }

        /// <summary>
        /// Removes a target from the list of targets and from the targets panel
        /// </summary>
        /// <param name="controller">Target to remove</param>
        private void RemoveTarget(TargetController controller) {
            if (controller.configToggle.GetComponent<Toggle>().isOn)
                panel.CloseConfig();
            Destroy(controller.gameObject);
            Destroy(controller.configToggle);
        }
    }
}
