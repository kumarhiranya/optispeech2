using UnityEngine;
using TMPro;
using System.Linq;
using Optispeech.Targets.Controllers;
using Optispeech.Sensors;
using Optispeech.Documentation;
using System.Globalization;

namespace Optispeech.Targets.Configs {

    /// <summary>
    /// Configuration interface for <see cref="LandmarkTargetController"/>s
    /// </summary>
    public class LandmarkTargetConfig : TargetConfig {

        /// <summary>
        /// Reference to en-US locale for making the ID dropdown in title case
        /// </summary>
        static readonly TextInfo local = new CultureInfo("en-US", false).TextInfo;

        /// <summary>
        /// Dropdown for <see cref="LandmarkTargetController.id"/>
        /// </summary>
        [SerializeField]
        private TMP_Dropdown idDropdown = default;

        [HideInDocumentation]
        private void Start() {
            SensorsManager.Instance.onListUpdate.AddListener(SetupDropdown);
        }

        [HideInDocumentation]
        public override void Init(TargetsPanel panel, TargetController controller) {
            base.Init(panel, controller);

            SetupDropdown();

            LandmarkTargetController landmarkController = (LandmarkTargetController)controller;

            idDropdown.value = idDropdown.options.FindIndex(d => int.Parse(d.text.Split(' ')[0]) == landmarkController.id);
            idDropdown.onValueChanged.AddListener(value => {
                landmarkController.id = int.Parse(idDropdown.options[value].text.Split(' ')[0]);
                panel.SaveTargetsToPrefs();
            });
        }

        [HideInDocumentation]
        public override void SetInteractable(bool interactable) {
            base.SetInteractable(interactable);
            idDropdown.interactable = interactable;
        }

        /// <summary>
        /// Sets up the dropdown with options for each active sensor ID
        /// </summary>
        private void SetupDropdown() {
            idDropdown.ClearOptions();
            idDropdown.AddOptions(SensorsManager.Instance.sensors.Select(s => s.id.ToString() + " - " + local.ToTitleCase(s.type.ToString().Replace('_', ' ').ToLower())).ToList());
        }
    }
}
