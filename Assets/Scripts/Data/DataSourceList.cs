using Optispeech.Documentation;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Data {

    /// <summary>
    /// This panel shows a list of data sources, whether or not they can be made active,
    /// and allows the user to see and change which data source is active
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DataSourceList : MonoBehaviour {

        /// <summary>
        /// A prefab with a DataSourceSelector MonoBehaviour on it that represents a single
        /// data source in the list. This class should have a RectTransform with a static height
        /// </summary>
        [SerializeField]
        private GameObject dataSourceSelectorPrefab = default;
        /// <summary>
        /// A button that will tell each data source to re-check whether or not it can be made active
        /// </summary>
        [SerializeField]
        private Button refreshSourcesButton = default;
        /// <summary>
        /// A container to place each data source selector inside of
        /// </summary>
        [SerializeField]
        private RectTransform contentContainer = default;

        /// <summary>
        /// The toggle on the data source selector that represents the currently active data source.
        /// This is stored so when the active data source changes the toggle's isOn property can be updated
        /// </summary>
        private Toggle activeToggle = null;

        [HideInDocumentation]
        void OnEnable() {
            refreshSourcesButton.onClick.AddListener(Refresh);
            Refresh();
        }

        [HideInDocumentation]
        void OnDisable() {
            refreshSourcesButton.onClick.RemoveListener(Refresh);
        }

        /// <summary>
        /// A function intended to be called by the data source selectors that updates
        /// the active toggle, while turning off the current active toggle if it exists
        /// </summary>
        /// <param name="newToggle">The toggle to store as the active toggle</param>
        public void SetActiveToggle(Toggle newToggle) {
            if (activeToggle != null)
                activeToggle.isOn = false;
            activeToggle = newToggle;
        }

        /// <summary>
        /// A function intended to be called by the data source selectors to check
        /// whether they are currently the active toggle
        /// </summary>
        /// <param name="newToggle">The toggle to check</param>
        /// <returns>Whether or not <paramref name="newToggle"/> is the active toggle</returns>
        public bool IsActiveToggle(Toggle newToggle) {
            return activeToggle == newToggle;
        }

        /// <summary>
        /// Removes the list of data source selectors if it exists and re-creates it
        /// </summary>
        public void Refresh() {
            // Remove any existing selectors
            // We start at index 1 to skip over the refresh button
            while (contentContainer.childCount > 1) {
                DestroyImmediate(contentContainer.GetChild(1).gameObject);
            }

            // This list will is populated by searching for
            // assets inside of "Resources/Data Source Descriptions", where Resources
            // is any folder named "Resources" inside of "Assets". That is, it will search
            // through ALL folders named Resources, including nested folders with that name
            DataSourceDescription[] dataSources = Resources.LoadAll("Data Source Descriptions", typeof(DataSourceDescription)).Cast<DataSourceDescription>().ToArray();

            // Add each data source to the list
            for (int i = 0; i < dataSources.Length; i++) {
                GameObject dataSourceSelector = Instantiate(dataSourceSelectorPrefab, contentContainer);
                dataSourceSelector.GetComponent<DataSourceSelector>().Init(this, dataSources[i]);
            }

            // Ensure that no data source is currently considered active
            DataSourceManager.Instance.SetActiveDataSourceReader(null);
        }
    }
}
