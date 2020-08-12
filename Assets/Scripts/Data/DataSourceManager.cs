using Optispeech.Documentation;
using Optispeech.Profiles;
using Optispeech.Sensors;
using Optispeech.Targets;
using Optispeech.UI;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Optispeech.Data {

    /// <summary>
    /// Tracks the active data source and handles changing or deselecting the active data source
    /// </summary>
    public class DataSourceManager : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static DataSourceManager Instance = default;

        /// <summary>
        /// The active data source reader
        /// </summary>
        [HideInInspector]
        public DataSourceReader dataSourceReader = default;
        /// <summary>
        /// The name of the active data source reader, as specified by its DataSourceDescription
        /// </summary>
        [HideInInspector]
        public string dataSourceName = default;
        /// <summary>
        /// Event that fires whenever a data frame is read by the active data source
        /// </summary>
        [HideInInspector]
        public UnityEvent<DataFrame> onFrameRead = new UnityEvent<DataFrame>();
        /// <summary>
        /// Event that fires whenever the active data source changes
        /// </summary>
        [HideInInspector]
        public UnityEvent<DataSourceReader> onSourceChanged = new UnityEvent<DataSourceReader>();

        /// <summary>
        /// Technically this doesn't need to be a canvas. This is an object that will be set active or inactive
        /// to tell the player when there is NO active data source. This is intended to notify someone why
        /// the tongue model is not moving, so they know what they need to do to change that. This is additionally
        /// important for new users, so they know what the first thing they need to do is. To this end, the object
        /// may also want to provide basic getting started information to the user.
        /// </summary>
        [SerializeField]
        private GameObject noSourceSelectedCanvas = default;
        /// <summary>
        /// Some settings are only useful to have visible when there is an active data source, so this accordion appears
        /// on the opposite side of the screen as the universal setting accordion. The active data source reader will be
        /// instantiated as a child of this accordion, so it may add additional panels with source-specific settings.
        /// </summary>
        [SerializeField]
        private Accordion sourceSettingsAccordion = default;

        /// <summary>
        /// The instantiated object of the active data source reader
        /// </summary>
        private GameObject activeDataSource = default;

        [HideInDocumentation]
        void Awake() {
            if (Instance == null || Instance == this) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }
        }

        [HideInDocumentation]
        void Start() {
            sourceSettingsAccordion.gameObject.SetActive(false);
            ProfileManager.Instance.onProfileChange.AddListener(ResetTargets);
        }

        /// <summary>
        /// Changes the active data source readerto the specified one, or just deselects the active data source if null is passed
        /// </summary>
        /// <param name="dataSourceReader">The data source to make active, or null</param>
        /// <param name="sourceName">The name of the data source, used to load targets for this data source. Ignored if <paramref name="dataSourceReader"/> is null</param>
        public void SetActiveDataSourceReader(DataSourceReader dataSourceReader, string sourceName = "") {
            if (activeDataSource != null) {
                // Get rid of the existing data source
                Destroy(activeDataSource);
                // Note we turn this off even if we're just switching sources,
                // to get OnEnable to fire again and re-setup everything
                sourceSettingsAccordion.gameObject.SetActive(false);
            }

            dataSourceName = sourceName;
            // input can be null if we don't have an active data source
            if (dataSourceReader == null) {
                // No source selected
                this.dataSourceReader = null;
                noSourceSelectedCanvas.SetActive(true);
                TargetsManager.Instance.panel.IsInteractable = false;
                onSourceChanged.Invoke(null);
            } else {
                // Select new source
                activeDataSource = Instantiate(dataSourceReader.gameObject, sourceSettingsAccordion.transform);
                this.dataSourceReader = activeDataSource.GetComponent<DataSourceReader>();
                SensorsManager.Instance.Reset();
                noSourceSelectedCanvas.SetActive(false);

                // We want to ensure OnDisable gets called on our accordion,
                // so we use a co-routine to wait a single frame before making the accordion active again
                StartCoroutine(DelayAccordion(sourceName));
            }

            // Reset targets
            TargetsManager.Instance.ResetTargets();
        }

        /// <summary>
        /// Clears all targets and then recreates the targets (and sensors) panels later, if applicable
        /// </summary>
        /// <param name="profile"></param>
        private void ResetTargets(ProfileManager.Profile profile) {
            TargetsManager.Instance.ResetTargets();
            if (dataSourceReader != null) {
                StartCoroutine(DelayAccordion(dataSourceName));
            }
        }

        /// <summary>
        /// Waits a frame and then sets up the sensors panel, and open the source settings accordion
        /// </summary>
        /// <param name="sourceName">The name of the data source, used to load sensors configurations for that source specifically</param>
        /// <returns>A coroutine that waits a frame before actually setting up the sensors panel</returns>
        private IEnumerator DelayAccordion(string sourceName) {
            yield return null;

            // Load sensors
            SensorsManager.Instance.panel.IsInteractable = this.dataSourceReader.AreSensorsConfigurable();
            if (SensorsManager.Instance.panel.IsInteractable) {
                if (ProfileManager.Instance.ActiveProfile.sensors.TryGetValue(sourceName, out string sensorsString)) {
                    SensorsManager.Instance.SetSensors(sensorsString.Split('\n').Select(s => {
                        if (s == "") return null;
                        SensorConfiguration config = new SensorConfiguration();
                        string[] values = s.Split('\t');
                        if (
                            int.TryParse(values[0], out config.id) &&
                            Enum.TryParse(values[1], out config.type) &&
                            float.TryParse(values[2], out config.preOffset.x) &&
                            float.TryParse(values[3], out config.preOffset.y) &&
                            float.TryParse(values[4], out config.preOffset.z) &&
                            float.TryParse(values[5], out config.postOffset.x) &&
                            float.TryParse(values[6], out config.postOffset.y) &&
                            float.TryParse(values[7], out config.postOffset.z)
                        ) {
                            config.status = SensorStatus.UNKNOWN;
                            config.display = null;
                            return config;
                        }
                        return null;
                    }).Where(s => s != null).ToArray());
                } else {
                    SensorsManager.Instance.SetSensors(this.dataSourceReader.GetDefaultSensorConfigurations());
                }
            }

            // Setup accordion
            onSourceChanged.Invoke(dataSourceReader);
            TogglePanel panel = dataSourceReader.GetComponentInChildren<TogglePanel>();
            if (panel)
                panel.startOpen = true;
            sourceSettingsAccordion.gameObject.SetActive(true);
        }
    }
}
