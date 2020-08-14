using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Optispeech.Targets {

    /// <summary>
    /// Manages a set of target configuration UI elements
    /// </summary>
    public abstract class TargetConfig : MonoBehaviour {

        /// <summary>
        /// Label to show the ID of the target
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI targetIDLabel = default;
        /// <summary>
        /// Button that will copy this target's config string to clipboard
        /// </summary>
        [SerializeField]
        private Button copyTargetButton = default;
        /// <summary>
        /// Button to delete this target
        /// </summary>
        [SerializeField]
        private Button deleteTargetButton = default;
        /// <summary>
        /// Slider to show how visible the target should be
        /// </summary>
        [SerializeField]
        private Slider visibilitySlider = default;
        /// <summary>
        /// Number field for the radius of the target marker
        /// </summary>
        [SerializeField]
        private TMP_InputField radiusField = default;
        /// <summary>
        /// Toggle button for whether to track the x axis
        /// </summary>
        [SerializeField]
        private Toggle trackXToggle = default;
        /// <summary>
        /// Toggle button for whether to track the y axis
        /// </summary>
        [SerializeField]
        private Toggle trackYToggle = default;
        /// <summary>
        /// Toggle button for whether to track the z axis
        /// </summary>
        [SerializeField]
        private Toggle trackZToggle = default;
        // TODO add marker dropdown? (TT, TD, ...)

        /// <summary>
        /// Sets up this config with the given target controller
        /// </summary>
        /// <param name="panel">The panel this config will appear in</param>
        /// <param name="targetToggle">The toggle that opens this config, used so the delete target button can also destroy this</param>
        /// <param name="controller">The target controller this config is for</param>
        public virtual void Init(TargetsPanel panel, TargetController controller) {
            targetIDLabel.text = controller.targetId;

            copyTargetButton.onClick.AddListener(() => {
                GUIUtility.systemCopyBuffer = controller.ToString();
            });

            deleteTargetButton.onClick.AddListener(() => {
                Destroy(TargetsManager.Instance.targets[controller.targetId].gameObject);
                TargetsManager.Instance.targets.Remove(controller.targetId);
                Destroy(controller.configToggle);
                panel.CloseConfig();
                panel.SaveTargetsToPrefs();
            });

            visibilitySlider.value = controller.visibility;
            visibilitySlider.onValueChanged.AddListener(visibility => {
                controller.visibility = visibility;
                Color color = controller.mesh.material.color;
                controller.mesh.material.color = new Color(color.r, color.g, color.b, visibility);
                panel.SaveTargetsToPrefs();
            });

            radiusField.text = controller.radius.ToString();
            radiusField.onValueChanged.AddListener(value => {
                // Try parsing the value, or set the radius to 0 if you can't (probably because value is an empty string)
                if (!float.TryParse(value, out controller.radius) || controller.radius < 0) {
                    radiusField.SetTextWithoutNotify("");
                    controller.radius = 0;
                }
                // Apply the radius to the target object
                controller.transform.localScale = new Vector3(controller.radius, controller.radius, controller.radius);
                panel.SaveTargetsToPrefs();
            });

            trackXToggle.isOn = controller.trackX;
            trackXToggle.onValueChanged.AddListener(value => {
                controller.trackX = value;
                panel.SaveTargetsToPrefs();
            });

            trackYToggle.isOn = controller.trackY;
            trackYToggle.onValueChanged.AddListener(value => {
                controller.trackY = value;
                panel.SaveTargetsToPrefs();
            });

            trackZToggle.isOn = controller.trackZ;
            trackZToggle.onValueChanged.AddListener(value => {
                controller.trackZ = value;
                panel.SaveTargetsToPrefs();
            });
        }

        /// <summary>
        /// Changes the interactivity of the UI elements
        /// </summary>
        /// <param name="interactable">Whether or not the config UI elements should be interactable</param>
        public virtual void SetInteractable(bool interactable) {
            deleteTargetButton.interactable = interactable;
            visibilitySlider.interactable = interactable;
            radiusField.interactable = interactable;
        }

        /// <summary>
        /// Optional function for doing custom things whenever the config menu is opened
        /// </summary>
        /// <remarks>
        /// Note these also fire when opening the targets panel
        /// </remarks>
        public virtual void OnOpen() { }
        /// <summary>
        /// Optional function for doing custom things whenever the config menu is closed
        /// </summary>
        /// <remarks>
        /// Note these also fire when closing the targets panel
        /// </remarks>
        public virtual void OnClose() { }
    }
}
