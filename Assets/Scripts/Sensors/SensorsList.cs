using Optispeech.Documentation;
using Optispeech.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Sensors {

    /// <summary>
    /// This panel displays a list of all the current sensors, with their configuration 
    /// </summary>
    public class SensorsList : MonoBehaviour {

        /// <summary>
        /// This variable is used to track whether the sensors panel can currently
        /// be edited. If not, sensor types cannot be modified by the user;
        /// Usually because there either isn't an active data source, or the data source
        /// has indicated that it'll be controlling the sensors instead of the user
        /// </summary>
        private bool isInteractable = false;

        /// <summary>
        /// Ths should be a prefab with a SensorInformationDisplay MonoBehaviour on it
        /// Each sensor in the list will be represented with one of these.
        /// This class assumes it has a RectTransform with a static height.
        /// </summary>
        [SerializeField]
        private GameObject sensorInfoDisplayPrefab = default;
        /// <summary>
        /// The reset resting position button, used to manually tell the tongue controller
        /// to recalculate the sensor pre-offsets so the jaw knows where resting position is
        /// </summary>
        [SerializeField]
        private Button resetRestingPosition = default;
        /// <summary>
        /// The container for all the sensors, to use as a parent and to change the panel's height appropriately
        /// </summary>
        [SerializeField]
        private RectTransform contentContainer = default;

        /// <summary>
        /// List of sensor displays currently instantiated
        /// </summary>
        private SensorInformationDisplay[] sensors = new SensorInformationDisplay[0];

        /// <summary>
        /// Property that handles updating the interactivity of each sensor display
        /// </summary>
        public bool IsInteractable {
            get => isInteractable;
            set {
                isInteractable = value;

                resetRestingPosition.interactable = value;

                foreach (SensorInformationDisplay display in sensors)
                    display.SetInteractable(value);
            }
        }

        [HideInDocumentation]
        private void Awake() {
            // Add each data source to the list
            sensors = SensorsManager.Instance.sensors
                .Select(s => Instantiate(sensorInfoDisplayPrefab, contentContainer).GetComponent<SensorInformationDisplay>()).ToArray();

            for (int i = 0; i < SensorsManager.Instance.sensors.Length; i++) {
                sensors[i].Init(SensorsManager.Instance.sensors[i]);
                sensors[i].SetInteractable(isInteractable);
            }
        }

        [HideInDocumentation]
        private void Start() {
            resetRestingPosition.onClick.AddListener(() => {
                SensorsManager.Instance.offsetsDirty = true;
            });

            SensorsManager.Instance.onListUpdate.AddListener(RecreateList);
        }

        /// <summary>
        /// Remove all the old sensor displays and re-create them based off the current sensors
        /// </summary>
        private void RecreateList() {
            // Remove any existing children except the first one (our reset resting position button)
            while (contentContainer.childCount > 1)
                DestroyImmediate(contentContainer.GetChild(1).gameObject);

            Awake();

            GetComponentInChildren<TogglePanel>().Refresh();
        }
    }
}
