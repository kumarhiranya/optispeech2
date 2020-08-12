using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using Optispeech.Data;
using Optispeech.Profiles;
using Optispeech.Documentation;

namespace Optispeech.Sensors {

    /// <summary>
    /// Enumeration of all the different "types", or "roles" of sensors
    /// </summary>
    public enum SensorType {
        IGNORED,
        FOREHEAD,
        LEFT_EAR,
        RIGHT_EAR,
        JAW,
        TONGUE_TIP,
        TONGUE_DORSUM,
        TONGUE_LEFT,
        TONGUE_RIGHT,
        TONGUE_BACK,
        OTHER
    }

    /// <summary>
    /// Stores information about each active sensor
    /// </summary>
    /// <remarks>
    /// Note that this is a class: This way it will pass by reference
    /// so it can be changed and compared appropriately.
    /// </remarks>
    public class SensorConfiguration {
        /// <summary>
        /// ID of this sensor
        /// </summary>
        public int id;
        /// <summary>
        /// This sensor's role
        /// </summary>
        public SensorType type;
        /// <summary>
        /// Pre offset is calculated and applied to the forehead and ear sensors'
        /// raw positions such that the jaw sensor will line up with the avatar's
        /// jaw when the patient is in "resting position"
        /// For other sensors it will be equal to <see cref="Vector3.zero"/>
        /// </summary>
        public Vector3 preOffset;
        /// <summary>
        /// Post offset is configured by the user and applied after the transform
        /// matrix is applied to the sensor.
        /// Can only be applied to non-reference sensors and for reference sensors will
        /// be equal to <see cref="Vector3.zero"/>. Intended use is for making a sensor appear further
        /// back in the mouth than it really is and similar purposes
        /// </summary>
        public Vector3 postOffset;
        /// <summary>
        /// The current status of this sensor, which is potentially updated at each data frame
        /// </summary>
        public SensorStatus status;
        /// <summary>
        /// Reference to this sensor's display in the <see cref="SensorsList"/> panel
        /// </summary>
        public SensorInformationDisplay display;
    }

    /// <summary>
    /// Manages the current sensors and their roles
    /// </summary>
    public class SensorsManager : MonoBehaviour {

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static SensorsManager Instance = default;

        /// <summary>
        /// List of all current sensors in the most recent data frame
        /// </summary>
        public SensorConfiguration[] sensors = new SensorConfiguration[0];

        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.FOREHEAD"/> type
        /// </summary>
        public SensorConfiguration foreheadSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.LEFT_EAR"/> type
        /// </summary>
        public SensorConfiguration leftEarSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.RIGHT_EAR"/> type
        /// </summary>
        public SensorConfiguration rightEarSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.JAW"/> type
        /// </summary>
        public SensorConfiguration jawSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.TONGUE_TIP"/> type
        /// </summary>
        public SensorConfiguration tongueTipSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.TONGUE_DORSUM"/> type
        /// </summary>
        public SensorConfiguration tongueDorsumSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.TONGUE_LEFT"/> type
        /// </summary>
        public SensorConfiguration tongueLeftSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.TONGUE_RIGHT"/> type
        /// </summary>
        public SensorConfiguration tongueRightSensorConfig;
        /// <summary>
        /// If it exists, the first sensor with <see cref="SensorType.TONGUE_BACK"/> type
        /// </summary>
        public SensorConfiguration tongueBackSensorConfig;
        /// <summary>
        /// All sensors with <see cref="SensorType.OTHER"/> type
        /// </summary>
        public IEnumerable<SensorConfiguration> otherSensorConfigs;

        /// <summary>
        /// The panel with a list of all sensors, used to handle displaying and updating sensor configurations
        /// </summary>
        public SensorsList panel;

        /// <summary>
        /// Flag to tell tongue controller whether or not to re-calculate pre-offsets
        /// </summary>
        // TODO make tongue controller subscribe to an event instead?
        [HideInInspector]
        public bool offsetsDirty = false;
        /// <summary>
        /// Event that fires whenever the sensors list changes
        /// </summary>
        [HideInInspector]
        public UnityEvent onListUpdate;

        /// <summary>
        /// Flag representing whether the sensors have changed
        /// </summary>
        private bool sensorsDirty = false;

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
        void Update() {
            if (sensorsDirty) {
                onListUpdate.Invoke();
                sensorsDirty = false;
            } else if (DataSourceManager.Instance.dataSourceReader != null) {
                DataFrame frame = DataSourceManager.Instance.dataSourceReader.lastFrame;
                for (int i = 0; i < frame.sensorData.Length; i++) {
                    if (frame.sensorData[i].status != sensors[i].status) {
                        sensors[i].display.SetSensorStatus(frame.sensorData[i].status);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all sensor configurations
        /// </summary>
        public void Reset() {
            // Remove all sensor configurations
            // Typically called when activating a data source
            sensors = new SensorConfiguration[0];
            foreheadSensorConfig = null;
            leftEarSensorConfig = null;
            rightEarSensorConfig = null;
            jawSensorConfig = null;
            tongueTipSensorConfig = null;
            tongueDorsumSensorConfig = null;
            tongueLeftSensorConfig = null;
            tongueRightSensorConfig = null;
            tongueBackSensorConfig = null;
            otherSensorConfigs = new SensorConfiguration[0];
        }

        // TODO make this a property with a setter?
        /// <summary>
        /// Updates list of sensors and assigns them to the appropriate roles
        /// </summary>
        /// <remarks>
        /// Note: A flag is used to know when to recreate the sensors list in Update because this method is intended to be called
        /// on a data source reader's thread, which will throw an error upon accessing a game object, transform, etc.
        /// </remarks>
        /// <param name="sensors">The new array of sensors</param>
        public void SetSensors(SensorConfiguration[] sensors) {
            this.sensors = sensors;
            sensorsDirty = true;
            offsetsDirty = true;

            foreheadSensorConfig = sensors.Where(s => s.type == SensorType.FOREHEAD).FirstOrDefault();
            leftEarSensorConfig = sensors.Where(s => s.type == SensorType.LEFT_EAR).FirstOrDefault();
            rightEarSensorConfig = sensors.Where(s => s.type == SensorType.RIGHT_EAR).FirstOrDefault();
            jawSensorConfig = sensors.Where(s => s.type == SensorType.JAW).FirstOrDefault();
            tongueTipSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_TIP).FirstOrDefault();
            tongueDorsumSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_DORSUM).FirstOrDefault();
            tongueLeftSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_LEFT).FirstOrDefault();
            tongueRightSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_RIGHT).FirstOrDefault();
            tongueBackSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_BACK).FirstOrDefault();
            otherSensorConfigs = sensors.Where(s => s.type == SensorType.OTHER);
        }

        /// <summary>
        /// Changes the type of a sensor and re-calculates the relevant roles
        /// </summary>
        /// <param name="sensor">The sensor whose type has changed</param>
        /// <param name="newType">The new type for this sensor</param>
        public void ChangeSensorType(SensorConfiguration sensor, SensorType newType) {
            // Change the sensor type to the new one
            SensorType oldType = sensor.type;
            sensor.type = newType;

            // Remove sensor from old configuration slot
            switch (oldType) {
                case SensorType.FOREHEAD:
                    if (foreheadSensorConfig == sensor)
                        foreheadSensorConfig = sensors.Where(s => s.type == SensorType.FOREHEAD).FirstOrDefault();
                    break;
                case SensorType.LEFT_EAR:
                    if (leftEarSensorConfig == sensor)
                        leftEarSensorConfig = sensors.Where(s => s.type == SensorType.LEFT_EAR).FirstOrDefault();
                    break;
                case SensorType.RIGHT_EAR:
                    if (rightEarSensorConfig == sensor)
                        rightEarSensorConfig = sensors.Where(s => s.type == SensorType.RIGHT_EAR).FirstOrDefault();
                    break;
                case SensorType.JAW:
                    if (jawSensorConfig == sensor)
                        jawSensorConfig = sensors.Where(s => s.type == SensorType.JAW).FirstOrDefault();
                    break;
                case SensorType.TONGUE_TIP:
                    if (tongueTipSensorConfig == sensor)
                        tongueTipSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_TIP).FirstOrDefault();
                    break;
                case SensorType.TONGUE_DORSUM:
                    if (tongueDorsumSensorConfig == sensor)
                        tongueDorsumSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_DORSUM).FirstOrDefault();
                    break;
                case SensorType.TONGUE_LEFT:
                    if (tongueLeftSensorConfig == sensor)
                        tongueLeftSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_LEFT).FirstOrDefault();
                    break;
                case SensorType.TONGUE_RIGHT:
                    if (tongueRightSensorConfig == sensor)
                        tongueRightSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_RIGHT).FirstOrDefault();
                    break;
                case SensorType.TONGUE_BACK:
                    if (tongueBackSensorConfig == sensor)
                        tongueBackSensorConfig = sensors.Where(s => s.type == SensorType.TONGUE_BACK).FirstOrDefault();
                    break;
                case SensorType.OTHER:
                    otherSensorConfigs = otherSensorConfigs.Where(s => s != sensor);
                    break;
                case SensorType.IGNORED:
                default:
                    break;
            }

            // Set sensor to new configuration slot
            switch (newType) {
                case SensorType.FOREHEAD: foreheadSensorConfig = sensor; break;
                case SensorType.LEFT_EAR: leftEarSensorConfig = sensor; break;
                case SensorType.RIGHT_EAR: rightEarSensorConfig = sensor; break;
                case SensorType.JAW: jawSensorConfig = sensor; break;
                case SensorType.TONGUE_TIP: tongueTipSensorConfig = sensor; break;
                case SensorType.TONGUE_DORSUM: tongueDorsumSensorConfig = sensor; break;
                case SensorType.TONGUE_LEFT: tongueLeftSensorConfig = sensor; break;
                case SensorType.TONGUE_RIGHT: tongueRightSensorConfig = sensor; break;
                case SensorType.TONGUE_BACK: tongueBackSensorConfig = sensor; break;
                case SensorType.OTHER:
                    otherSensorConfigs = otherSensorConfigs.Concat(new[] { sensor });
                    break;
                case SensorType.IGNORED:
                default:
                    break;
            }

            SaveSensors();
        }

        /// <summary>
        /// Saves the sensor configurations to the active profile, so they can be re-loaded
        /// when switching to the same data source reader
        /// </summary>
        public void SaveSensors() {
            // Construct string of all sensors, one sensor per line
            string sensorsString = "";
            foreach (SensorConfiguration config in sensors)
                sensorsString += "\n" + config.id + "\t" + config.type + "\t" +
                    config.preOffset.x + "\t" + config.preOffset.y + "\t" + config.preOffset.z + "\t" +
                    config.postOffset.x + "\t" + config.postOffset.y + "\t" + config.postOffset.z;
            // Remove first newline character
            if (sensors.Length > 0)
                sensorsString = sensorsString.Substring(1);

            // Save to profile
            ProfileManager.Instance.ActiveProfile.sensors[DataSourceManager.Instance.dataSourceName] = sensorsString;
            ProfileManager.Instance.Save(); // Deliberately do not re-create sensors panel
        }
    }
}
