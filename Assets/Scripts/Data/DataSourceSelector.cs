using Optispeech.Documentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Data {
    
    /// <summary>
    /// This is a UGUI object that represents a data source in the data source list.
    /// It has a toggle to change whether its the active data source or not, and
    /// communicates to the user the current status of this data source
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class DataSourceSelector : MonoBehaviour {

        /// <summary>
        /// A label to show the name of the data source represented by this selector
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI nameLabel = default;
        /// <summary>
        /// An image that will change sprite dependent on the current status of the
        /// data source represented by this selector
        /// </summary>
        [SerializeField]
        private Image statusIndicator = default;

        /// <summary>
        /// The sprite to show when the data source represented by this data selector
        /// is currently available to be selected
        /// </summary>
        [SerializeField]
        private Sprite availableStatusIcon = default;
        /// <summary>
        /// The sprite to show when the data source represented by this data selector
        /// is currently determining its availability
        /// </summary>
        [SerializeField]
        private Sprite waitingStatusIcon = default;
        /// <summary>
        /// The sprite to show when the data source represented by this data selector
        /// is currently not available to be selected
        /// </summary>
        [SerializeField]
        private Sprite failedStatusIcon = default;

        /// <summary>
        /// A reference to the list of data source selectors this selector is apart of.
        /// This is used to communicate back to the list whenever this data source is toggled
        /// </summary>
        private DataSourceList sourceList = default;
        /// <summary>
        /// The description of the data source represented by this selector
        /// </summary>
        private DataSourceDescription description = default;

        /// <summary>
        /// The DataSourceReader component of the prefab of the data source represented by this selector
        /// </summary>
        private DataSourceReader sourceReader = default;
        /// <summary>
        /// The toggle used to change whether the data source represented
        /// by this selector is the active data source or not
        /// </summary>
        private Toggle toggle = default;

        [HideInDocumentation]
        void Awake() {
            toggle = GetComponent<Toggle>();
        }

        /// <summary>
        /// Initializes this selector so it will represent the data source described to it
        /// </summary>
        /// <param name="sourceList">The data source list this selector is apart of</param>
        /// <param name="description">The description of the data source to represent</param>
        public void Init(DataSourceList sourceList, DataSourceDescription description) {
            this.sourceList = sourceList;
            this.description = description;

            // Setup our data source selector
            nameLabel.SetText(description.sourceName);

            sourceReader = description.readerPrefab.GetComponentInChildren<DataSourceReader>();
            sourceReader.statusChangeEvent.AddListener(OnStatusChange);
            OnStatusChange(sourceReader.GetCurrentStatus());

            // If we get clicked, set the active data source and turn the previous one off
            toggle.onValueChanged.AddListener(Select);
        }

        /// <summary>
        /// Callback method given to the data source reader that will update the status icon appropriately
        /// whenever the source's status changes, as well as force the toggle to be off unless the data source
        /// is currently available
        /// </summary>
        /// <param name="status">The new status to represent</param>
        void OnStatusChange(DataSourceReader.DataSourceReaderStatus status) {
            switch (status) {
                case DataSourceReader.DataSourceReaderStatus.AVAILABLE:
                    statusIndicator.sprite = availableStatusIcon;
                    statusIndicator.color = Color.green;
                    break;
                case DataSourceReader.DataSourceReaderStatus.UNKNOWN:
                    statusIndicator.sprite = waitingStatusIcon;
                    statusIndicator.color = Color.yellow;
                    break;
                case DataSourceReader.DataSourceReaderStatus.UNAVAILABLE:
                    statusIndicator.sprite = failedStatusIcon;
                    statusIndicator.color = Color.red;
                    break;
            }

            toggle.interactable = status == DataSourceReader.DataSourceReaderStatus.AVAILABLE;

            // If we were selected and the source isn't available
            if (toggle.isOn && !toggle.interactable) {
                // Turn us off and remove the data source
                toggle.isOn = false;
                DataSourceManager.Instance.SetActiveDataSourceReader(null);
            }
        }

        /// <summary>
        /// Handles the toggle changing state by making the active data source this one or null
        /// </summary>
        /// <param name="isOn">Whether or not the toggle's state is on</param>
        void Select(bool isOn) {
            // We use sourceList.IsActiveToggle because isOn is a bit unpredictable
            // when being set through code, and its more accurate to just track it ourselves
            // TODO use SetIsOnWithoutNotify instead?
            if (sourceList.IsActiveToggle(toggle)) {
                sourceList.SetActiveToggle(null);
                DataSourceManager.Instance.SetActiveDataSourceReader(null);
            } else {
                sourceList.SetActiveToggle(toggle);
                DataSourceManager.Instance.SetActiveDataSourceReader(description.readerPrefab, description.sourceName);
            }
        }
    }
}
