using UnityEngine;
using TMPro;
using Optispeech.Targets.Controllers;
using Optispeech.Documentation;

namespace Optispeech.Targets.Configs {

    /// <summary>
    /// Configuration interface for <see cref="OscillatingTargetController"/>s
    /// </summary>
    public class OscillatingTargetConfig : TargetConfig {

        /// <summary>
        /// Field for <see cref="OscillatingTargetController.startPosition"/>'s x component
        /// </summary>
        [SerializeField]
        private TMP_InputField startXPosField = default;
        /// <summary>
        /// Field for <see cref="OscillatingTargetController.startPosition"/>'s y component
        /// </summary>
        [SerializeField]
        private TMP_InputField startYPosField = default;
        /// <summary>
        /// Field for <see cref="OscillatingTargetController.startPosition"/>'s z component
        /// </summary>
        [SerializeField]
        private TMP_InputField startZPosField = default;
        /// <summary>
        /// Field for <see cref="OscillatingTargetController.endPosition"/>'s x component
        /// </summary>
        [SerializeField]
        private TMP_InputField endXPosField = default;
        /// <summary>
        /// Field for <see cref="OscillatingTargetController.endPosition"/>'s y component
        /// </summary>
        [SerializeField]
        private TMP_InputField endYPosField = default;
        /// <summary>
        /// Field for <see cref="OscillatingTargetController.endPosition"/>'s z component
        /// </summary>
        [SerializeField]
        private TMP_InputField endZPosField = default;
        /// <summary>
        /// Field for <see cref="OscillatingTargetController.frequency"/>
        /// </summary>
        [SerializeField]
        private TMP_InputField frequencyField = default;

        [HideInDocumentation]
        public override void Init(TargetsPanel panel, TargetController controller) {
            base.Init(panel, controller);

            OscillatingTargetController oscillatingController = (OscillatingTargetController)controller;

            Vector3 pos = oscillatingController.startPosition;
            startXPosField.text = pos.x.ToString();
            startXPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.startPosition.x)) {
                    oscillatingController.startPosition.x = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            startYPosField.text = pos.y.ToString();
            startYPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.startPosition.y)) {
                    oscillatingController.startPosition.y = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            startZPosField.text = pos.z.ToString();
            startZPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.startPosition.z)) {
                    oscillatingController.startPosition.z = 0;
                }
                panel.SaveTargetsToPrefs();
            });

            pos = oscillatingController.endPosition;
            endXPosField.text = pos.x.ToString();
            endXPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.endPosition.x)) {
                    oscillatingController.endPosition.x = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            endYPosField.text = pos.y.ToString();
            endYPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.endPosition.y)) {
                    oscillatingController.endPosition.y = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            endZPosField.text = pos.z.ToString();
            endZPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.endPosition.z)) {
                    oscillatingController.endPosition.z = 0;
                }
                panel.SaveTargetsToPrefs();
            });

            frequencyField.text = oscillatingController.frequency.ToString();
            frequencyField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out oscillatingController.frequency)) {
                    oscillatingController.frequency = 0;
                }
                panel.SaveTargetsToPrefs();
            });
        }

        [HideInDocumentation]
        public override void SetInteractable(bool interactable) {
            base.SetInteractable(interactable);
            startXPosField.interactable = interactable;
            startYPosField.interactable = interactable;
            startZPosField.interactable = interactable;
            endXPosField.interactable = interactable;
            endYPosField.interactable = interactable;
            endZPosField.interactable = interactable;
            frequencyField.interactable = interactable;
        }
    }
}
