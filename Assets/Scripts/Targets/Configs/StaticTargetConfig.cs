using UnityEngine;
using TMPro;
using Optispeech.Targets.Controllers;
using Optispeech.Documentation;

namespace Optispeech.Targets.Configs {

    /// <summary>
    /// Configuration interface for <see cref="StaticTargetController"/>s
    /// </summary>
    public class StaticTargetConfig : TargetConfig {

        /// <summary>
        /// Field for <see cref="StaticTargetController.position"/>'s x component
        /// </summary>
        [SerializeField]
        private TMP_InputField xPosField = default;
        /// <summary>
        /// Field for <see cref="StaticTargetController.position"/>'s y component
        /// </summary>
        [SerializeField]
        private TMP_InputField yPosField = default;
        /// <summary>
        /// Field for <see cref="StaticTargetController.position"/>'s z component
        /// </summary>
        [SerializeField]
        private TMP_InputField zPosField = default;

        [HideInDocumentation]
        public override void Init(TargetsPanel panel, TargetController controller) {
            base.Init(panel, controller);

            StaticTargetController staticController = (StaticTargetController)controller;

            Vector3 pos = staticController.position;
            xPosField.text = pos.x.ToString();
            xPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out staticController.position.x)) {
                    staticController.position.x = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            yPosField.text = pos.y.ToString();
            yPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out staticController.position.y)) {
                    staticController.position.y = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            zPosField.text = pos.z.ToString();
            zPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out staticController.position.z)) {
                    staticController.position.z = 0;
                }
                panel.SaveTargetsToPrefs();
            });
        }

        [HideInDocumentation]
        public override void SetInteractable(bool interactable) {
            base.SetInteractable(interactable);
            xPosField.interactable = interactable;
            yPosField.interactable = interactable;
            zPosField.interactable = interactable;
        }
    }
}
