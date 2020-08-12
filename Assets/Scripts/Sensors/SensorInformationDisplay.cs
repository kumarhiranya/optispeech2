using Optispeech.Data;
using Optispeech.Documentation;
using Optispeech.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Sensors {

    /// <summary>
    /// UI element that shows the current status for a single sensor, as well as configuration settings
    /// for that sensor
    /// </summary>
    public class SensorInformationDisplay : MonoBehaviour {

        /// <summary>
        /// Image whose sprite will indicate the current status of the sensor
        /// </summary>
        [SerializeField]
        private Image statusIndicator = default;
        /// <summary>
        /// Tooltip that displays the name of the status on hover
        /// </summary>
        [SerializeField]
        private Tooltip statusTooltip = default;
        /// <summary>
        /// Label that shows the ID of this sensor
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI idLabel = default;
        // Note: TMP Dropdown doesn't support recompilation during play mode
        // Only affects development, but might be worth looking for other solutions?
        /// <summary>
        /// Dropdown of all different sensor "roles", and allows the user to change which this sensor fills
        /// </summary>
        [SerializeField]
        private TMP_Dropdown typeDropdown = default;
        /// <summary>
        /// Number field for changing the sensor's post-offset x value
        /// </summary>
        [SerializeField]
        private TMP_InputField xOffsetField = default;
        /// <summary>
        /// Number field for changing the sensor's post-offset y value
        /// </summary>
        [SerializeField]
        private TMP_InputField yOffsetField = default;
        /// <summary>
        /// Number field for changing the sensor's post-offset z value
        /// </summary>
        [SerializeField]
        private TMP_InputField zOffsetField = default;

        /// <summary>
        /// The configuration for this sensor
        /// </summary>
        private SensorConfiguration configuration;
        /// <summary>
        /// Whether or not this sensor is a reference sensor (Jaw, Forehead, and Ears).
        /// If true, the offset fields are made non-interactive
        /// </summary>
        private bool isReferenceSensor;

        /// <summary>
        /// Sets up this display with the given sensor
        /// </summary>
        /// <param name="configuration">The sensor this display will represent</param>
        public void Init(SensorConfiguration configuration) {
            this.configuration = configuration;
            configuration.display = this;

            // Set name
            idLabel.text = "Sensor " + configuration.id.ToString();

            // Set type
            typeDropdown.value = (int)configuration.type;
            isReferenceSensor = configuration.type == SensorType.FOREHEAD ||
                                configuration.type == SensorType.JAW ||
                                configuration.type == SensorType.LEFT_EAR ||
                                configuration.type == SensorType.RIGHT_EAR;

            // If we're a forehead, ear, or jaw sensor our offsets can't be edited
            if (isReferenceSensor) {
                xOffsetField.interactable = false;
                yOffsetField.interactable = false;
                zOffsetField.interactable = false;

                xOffsetField.text = "";
                yOffsetField.text = "";
                zOffsetField.text = "";
            } else {
                // For each of these we make sure it defaults to the placeholder (a 0 but grey and italicized)
                xOffsetField.text = configuration.postOffset.x == 0 ? "" : configuration.postOffset.x.ToString();
                yOffsetField.text = configuration.postOffset.y == 0 ? "" : configuration.postOffset.y.ToString();
                zOffsetField.text = configuration.postOffset.z == 0 ? "" : configuration.postOffset.z.ToString();
            }

            // Set status
            SetSensorStatus(configuration.status);

            // Add event listener callbacks
            typeDropdown.onValueChanged.AddListener(SetSensorType);
            xOffsetField.onValueChanged.AddListener(s => SetXOffset(s == "" ? 0 : float.Parse(s)));
            yOffsetField.onValueChanged.AddListener(s => SetYOffset(s == "" ? 0 : float.Parse(s)));
            zOffsetField.onValueChanged.AddListener(s => SetZOffset(s == "" ? 0 : float.Parse(s)));
        }

        /// <summary>
        /// Changes the interactivity of the various input fields as appropriate
        /// </summary>
        /// <param name="interactable">Whether or not these fields should be interactable</param>
        public void SetInteractable(bool interactable) {
            typeDropdown.interactable = interactable;

            xOffsetField.interactable = interactable && !isReferenceSensor;
            yOffsetField.interactable = interactable && !isReferenceSensor;
            zOffsetField.interactable = interactable && !isReferenceSensor;
        }

        /// <summary>
        /// Changes the displayed status for this sensor
        /// </summary>
        /// <param name="status">The new status for this sensor</param>
        public void SetSensorStatus(SensorStatus status) {
            switch (status) {
                case SensorStatus.OK:
                    statusIndicator.color = Color.green;
                    statusTooltip.SetText("OK");
                    break;
                case SensorStatus.BAD_FIT:
                    statusIndicator.color = Color.red;
                    statusTooltip.SetText("Bad Fit");
                    break;
                case SensorStatus.OUT_OF_VOLUME:
                    statusIndicator.color = Color.yellow;
                    statusTooltip.SetText("Out of Range");
                    break;
                case SensorStatus.PROCESSING_ERROR:
                    statusIndicator.color = Color.blue;
                    statusTooltip.SetText("Processing Error");
                    break;
                case SensorStatus.UNKNOWN:
                    statusIndicator.color = Color.black;
                    statusTooltip.SetText("Unknown Status");
                    break;
                default:
                    return;
            }
        }

        [HideInDocumentation]
        private void SetSensorType(int type) {
            SensorsManager.Instance.ChangeSensorType(configuration, (SensorType)type);
            bool isReferenceSensor = (SensorType)type == SensorType.FOREHEAD ||
                                     (SensorType)type == SensorType.JAW ||
                                     (SensorType)type == SensorType.LEFT_EAR ||
                                     (SensorType)type == SensorType.RIGHT_EAR;

            // If switching between a reference sensor and a different type,
            // we need to recalculate our resting position
            if (isReferenceSensor == xOffsetField.interactable)
                SensorsManager.Instance.offsetsDirty = true;

            xOffsetField.interactable = !isReferenceSensor;
            yOffsetField.interactable = !isReferenceSensor;
            zOffsetField.interactable = !isReferenceSensor;

            if (isReferenceSensor) {
                xOffsetField.text = "";
                yOffsetField.text = "";
                zOffsetField.text = "";
            }
        }

        [HideInDocumentation]
        private void SetXOffset(float xOffset) {
            configuration.postOffset.x = xOffset;
            SensorsManager.Instance.SaveSensors();
        }

        [HideInDocumentation]
        private void SetYOffset(float yOffset) {
            configuration.postOffset.y = yOffset;
            SensorsManager.Instance.SaveSensors();
        }

        [HideInDocumentation]
        private void SetZOffset(float zOffset) {
            configuration.postOffset.z = zOffset;
            SensorsManager.Instance.SaveSensors();
        }
    }
}
