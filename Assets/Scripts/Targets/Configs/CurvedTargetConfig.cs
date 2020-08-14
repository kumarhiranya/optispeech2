using UnityEngine;
using TMPro;
using Optispeech.Targets.Controllers;
using Optispeech.Documentation;

namespace Optispeech.Targets.Configs{

    /// <summary>
    /// Configuration interface for <see cref="CurvedTargetController"/>s
    /// </summary>
    public class CurvedTargetConfig : TargetConfig    {

        /// <summary>
        /// Field for <see cref="CurvedTargetController.startPosition"/>'s x component
        /// </summary>
        [SerializeField]
        private TMP_InputField startXPosField = default;
        /// <summary>
        /// Field for <see cref="CurvedTargetController.startPosition"/>'s y component
        /// </summary>
        [SerializeField]
        private TMP_InputField startYPosField = default;
        /// <summary>
        /// Field for <see cref="CurvedTargetController.startPosition"/>'s z component
        /// </summary>
        [SerializeField]
        private TMP_InputField startZPosField = default;
        /// <summary>
        /// Field for <see cref="CurvedTargetController.vAmp"/>, vertical amplitude
        /// </summary>
        [SerializeField]
        private TMP_InputField vAmpField = default;
        /// <summary>
        /// Field for <see cref="CurvedTargetController.hAmp"/>, horizontal amplitude
        /// </summary>
        [SerializeField]
        private TMP_InputField hAmpField = default;
        /// <summary>
        /// Field for <see cref="CurvedTargetController.frequency"/>, frequency
        /// </summary>
        [SerializeField]
        private TMP_InputField frequencyField = default;
        

        [HideInDocumentation]
        public override void Init(TargetsPanel panel, TargetController controller){
            base.Init(panel, controller);

            CurvedTargetController curvedTargetController = (CurvedTargetController)controller;

            Vector3 pos = curvedTargetController.startPosition;
            startXPosField.text = pos.x.ToString();
            startXPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out curvedTargetController.startPosition.x))
                {
                    curvedTargetController.startPosition.x = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            startYPosField.text = pos.y.ToString();
            startYPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out curvedTargetController.startPosition.y))
                {
                    curvedTargetController.startPosition.y = 0;
                }
                panel.SaveTargetsToPrefs();
            });
            startZPosField.text = pos.z.ToString();
            startZPosField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out curvedTargetController.startPosition.z))
                {
                    curvedTargetController.startPosition.z = 0;
                }
                panel.SaveTargetsToPrefs();
            });

            vAmpField.text = curvedTargetController.vAmp.ToString();
            vAmpField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out curvedTargetController.vAmp))
                {
                    curvedTargetController.vAmp = 0;
                }
                panel.SaveTargetsToPrefs();
            });

            hAmpField.text = curvedTargetController.hAmp.ToString();
            hAmpField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out curvedTargetController.hAmp))
                {
                    curvedTargetController.hAmp = 0;
                }
                panel.SaveTargetsToPrefs();
            });

            frequencyField.text = curvedTargetController.frequency.ToString();
            frequencyField.onValueChanged.AddListener(value => {
                if (!float.TryParse(value, out curvedTargetController.frequency))
                {
                    curvedTargetController.frequency = 0;
                }
                panel.SaveTargetsToPrefs();
            });
        }

        [HideInDocumentation]
        public override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            startXPosField.interactable = interactable;
            startYPosField.interactable = interactable;
            startZPosField.interactable = interactable;
            vAmpField.interactable = interactable;
            hAmpField.interactable = interactable;
            frequencyField.interactable = interactable;
        }
    }
}
