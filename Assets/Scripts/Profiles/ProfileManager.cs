using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using Optispeech.Advanced;
using Optispeech.Documentation;

namespace Optispeech.Profiles {

    /// <summary>
    /// Manages the active profile, which includes all the settings in the entire program, and handles updating
    /// settings and switching profiles. Profiles are saved to PlayerPrefs.
    /// </summary>
    public class ProfileManager : MonoBehaviour {

        /// <summary>
        /// A collection of settings
        /// </summary>
        /// <remarks>
        /// structs don't have default values so we add tons of attributes to set the default for each parameter
        /// as well as tell our json library to handle default values by "IgnoreAndPopulate", which means
        /// it'll use the default value when that property is missing from the string,
        /// which means we can use an empty object as the default profile when reading from PlayerPrefs.
        /// Additionally it'll ignore any values to serialize that are already the default, which will
        /// lead to shorter strings saved to PlayerPrefs.
        /// Some default values aren't known at compile time and are set manually
        /// </remarks>
        public struct Profile {
            /// <summary>
            /// The name of this profile, defaulting to "Default"
            /// </summary>
            [DefaultValue("Default")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string profileName;
            /// <summary>
            /// The strength of the low pass filter, defaulting to the default value in <see cref="FilterManager.defaultStrength"/>
            /// </summary>
            public int lpfStrength;
            /// <summary>
            /// A dictionary of target IDs to config settings, defaulting to an empty dictionary
            /// </summary>
            public Dictionary<string, string> targets;
            /// <summary>
            /// A dictionary of sensor IDs to config settings, defaulting to an empty dictionary
            /// </summary>
            public Dictionary<string, string> sensors;
            /// <summary>
            /// The folder to save sweep data in, defaulting to the "My Documents" folder
            /// </summary>
            public string sweepFolder;
            /// <summary>
            /// The name of the current sweep, defaulting to "sweep"
            /// </summary>
            [DefaultValue("sweep")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string sweepName;
            /// <summary>
            /// When to stop a sweep automatically (or -1 to never stop automatically), defaulting to -1
            /// </summary>
            [DefaultValue(-1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public int autoStopDuration;
            /// <summary>
            /// Whether or not to save raw data from sweeps, defaulting to true
            /// </summary>
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool saveRaw;
            /// <summary>
            /// Whether or not to save transformed data from sweeps, defaulting to true
            /// </summary>
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool saveTransformed;
            /// <summary>
            /// Whether or not to save transformed data but ignoring offsets from sweeps, defaulting to false
            /// </summary>
            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool saveTransformedWithoutOffsets;
            /// <summary>
            /// Whether or not to save audio data from sweeps, defaulting to true
            /// </summary>
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool saveAudio;
            /// <summary>
            /// The current position of the camera, defaulting to <see cref="initialCameraPos"/>
            /// </summary>
            public SerializableVector3 cameraPos;
            /// <summary>
            /// The current rotation of the camera, defaulting to <see cref="initialCameraRot"/>
            /// </summary>
            public SerializableVector3 cameraRot;
            /// <summary>
            /// The transparency of the head material, defaulting to 0
            /// </summary>
            [DefaultValue(0)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public float headTransparency;
            /// <summary>
            /// The transparency of the gums material, defaulting to 0
            /// </summary>
            [DefaultValue(0)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public float gumsTransparency;
            /// <summary>
            /// The visibility of the reference markers, defaulting to invisible
            /// </summary>
            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool markersVisible;
            /// <summary>
            /// The visibility of the sensor markers, defaulting to visible
            /// </summary>
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool sensorsVisible;
            /// <summary>
            /// The visibility of the tongue, defaulting to visible
            /// </summary>
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool tongueVisible;
        }

        /// <summary>
        /// Delegate to make updating profiles based on input string callbacks easier
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="profile">The profile to apply the value to</param>
        public delegate void ProfileStringUpdater(string value, ref Profile profile);
        /// <summary>
        /// Delegate to make updating profiles based on input int callbacks easier
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="profile">The profile to apply the value to</param>
        public delegate void ProfileIntUpdater(int value, ref Profile profile);
        /// <summary>
        /// Delegate to make updating profiles based on input float callbacks easier
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="profile">The profile to apply the value to</param>
        public delegate void ProfileFloatUpdater(float value, ref Profile profile);
        /// <summary>
        /// Delegate to make updating profiles based on input bool callbacks easier
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="profile">The profile to apply the value to</param>
        public delegate void ProfileBoolUpdater(bool value, ref Profile profile);

        /// <summary>
        /// Static member to access the singleton instance of this class
        /// </summary>
        public static ProfileManager Instance = default;

        /// <summary>
        /// Create a UnityEvent that passes along a Profile to each listener
        /// </summary>
        [Serializable]
        public class ProfileEvent : UnityEvent<Profile> { }
        /// <summary>
        /// Create a UnityEvent that passes along an int to each listener
        /// </summary>
        [Serializable]
        public class IntEvent : UnityEvent<int> { }

        /// <summary>
        /// Event that fires whenever the active profile changes
        /// </summary>
        [HideInInspector]
        public ProfileEvent onProfileChange = new ProfileEvent();
        /// <summary>
        /// Event that fires whenever a new profile is added to the list
        /// </summary>
        [HideInInspector]
        public ProfileEvent onProfileAdded = new ProfileEvent();
        /// <summary>
        /// Event that fires whenever a profile is removed from the list
        /// </summary>
        [HideInInspector]
        public IntEvent onProfileDeleted = new IntEvent();
        /// <summary>
        /// List of all registered profiles
        /// </summary>
        [HideInInspector]
        public List<Profile> profiles;
        /// <summary>
        /// The index in <see cref="profiles"/> of the active profile
        /// </summary>
        [HideInInspector]
        public int activeProfileIndex;

        /// <summary>
        /// The default camera position for new profiles
        /// </summary>
        private Vector3 initialCameraPos;
        /// <summary>
        /// The default camera rotation for new profiles
        /// </summary>
        private Vector3 initialCameraRot;

        /// <summary>
        /// Property that provides public read-only access to the active profile
        /// </summary>
        public Profile ActiveProfile {
            get => activeProfile;
            private set {
                activeProfile = value;
                onProfileChange.Invoke(value);
            }
        }

        /// <summary>
        /// The currently active profile
        /// </summary>
        private Profile activeProfile;

        [HideInDocumentation]
        private void Awake() {
            if (Instance == null || Instance == this) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }

            initialCameraPos = Camera.main.transform.position;
            initialCameraRot = Camera.main.transform.localEulerAngles;
        }

        [HideInDocumentation]
        private void Start() {
            // Load our list of profiles
            profiles = JsonConvert.DeserializeObject<List<Profile>>(PlayerPrefs.GetString("profilesList", "[]"));
            // Make sure runtime defaults have some value
            for (int i = 0; i < profiles.Count; i++) {
                Profile profile = profiles[i];
                // Ensure dictionary exists (we want an empty dictionary not a null reference)
                if (profile.targets == null) profile.targets = new Dictionary<string, string>();
                // Ensure if a SerializedVector doesn't exist, it becomes the default
                if (profile.cameraPos == null) profile.cameraPos = initialCameraPos;
                if (profile.cameraRot == null) profile.cameraRot = initialCameraRot;
                // Save profile back to array (since we copied the profile when making it local)
                profiles[i] = profile;
            }
            // If the list is empty, add a default one
            if (profiles.Count == 0)
                profiles.Add(CreateProfile());

            // Load initial profile
            activeProfileIndex = PlayerPrefs.GetInt("activeProfile", 0);
            // Don't use property when loading the initial profile
            // because we don't want to save and we don't need to tell other scrips the profile has changed
            // (they'll just use ProfileManager.Instance.ActiveProfile when loading)
            activeProfile = profiles[activeProfileIndex];
        }

        /// <summary>
        /// Loads a profile from the list at the specified index
        /// </summary>
        /// <param name="index">The index in <see cref="profiles"/> of the profile to load</param>
        public void LoadProfile(int index) {
            activeProfileIndex = index;
            PlayerPrefs.SetInt("activeProfile", index);
            PlayerPrefs.Save();
            ActiveProfile = profiles[index];
        }

        /// <summary>
        /// Creates a new profile
        /// </summary>
        /// <remarks>
        /// Some default values aren't known at compile time, so here we create a new profile
        /// and manually set the default values appropriately
        /// </remarks>
        /// <returns>A default profile</returns>
        public static Profile CreateProfile() {
            // Start by deserializing an empty object to get all the compile-time defaults
            Profile profile = JsonConvert.DeserializeObject<Profile>("{}");

            // Set each of the runtime defaults
            profile.lpfStrength = FilterManager.Instance.defaultStrength;
            profile.sweepFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            profile.targets = new Dictionary<string, string>();
            profile.sensors = new Dictionary<string, string>();
            profile.cameraPos = Instance.initialCameraPos;
            profile.cameraRot = Instance.initialCameraRot;

            return profile;
        }

        /// <summary>
        /// Deletes the profile at the specified index
        /// </summary>
        /// <param name="index">The index in <see cref="profiles"/> of the profile to delete</param>
        public void DeleteProfile(int index) {
            profiles.RemoveAt(index);
            PlayerPrefs.SetString("profilesList", JsonConvert.SerializeObject(profiles));
            PlayerPrefs.Save();
            onProfileDeleted.Invoke(index);

            // If we deleted the active profile, go to the first profile
            if (activeProfileIndex == index) {
                // If we deleted the last profile, add a new default one
                if (profiles.Count == 0)
                    AddProfile(CreateProfile());
                else
                    LoadProfile(0);
            }
        }

        /// <summary>
        /// Adds the given profile to the profiles list and loads it
        /// </summary>
        /// <param name="profile">The profile to add to <see cref="profiles"/></param>
        public void AddProfile(Profile profile) {
            profiles.Add(profile);
            Save();
            onProfileAdded.Invoke(profile);
            LoadProfile(profiles.Count - 1);
        }

        /// <summary>
        /// Updates a profile at the given index
        /// </summary>
        /// <param name="profile">The new value of the profile to save</param>
        /// <param name="index">The index in <see cref="profiles"/> to save the profile to</param>
        public void UpdateProfile(Profile profile, int index = -1) {
            if (index == -1) index = activeProfileIndex;

            profiles[index] = profile;
            Save();
            if (index == activeProfileIndex)
                ActiveProfile = profiles[index];
        }

        /// <summary>
        /// Saves <see cref="profiles"/> without invoking any listeners
        /// </summary>
        /// <remarks>
        /// Rarely to be used outside this class.
        /// </remarks>
        public void Save() {
            PlayerPrefs.SetString("profilesList", JsonConvert.SerializeObject(profiles));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Function intended to be used as a callback for input elements for conveniently updating a profile
        /// </summary>
        /// <param name="updater">Delegate to update the given profile with the given value</param>
        /// <returns>Callback to give to an input element's onValueChanged event</returns>
        public static UnityAction<string> UpdateProfileCB(ProfileStringUpdater updater) {
            return (string value) => {
                Profile profile = Instance.ActiveProfile;
                updater(value, ref profile);
                Instance.UpdateProfile(profile);
            };
        }

        /// <summary>
        /// Function intended to be used as a callback for input elements for conveniently updating a profile
        /// </summary>
        /// <param name="updater">Delegate to update the given profile with the given value</param>
        /// <returns>Callback to give to an input element's onValueChanged event</returns>
        public static UnityAction<string> UpdateProfileCB(ProfileIntUpdater updater) {
            return (string value) => {
                if (int.TryParse(value, out int stringValue)) {
                    Profile profile = Instance.ActiveProfile;
                    updater(stringValue, ref profile);
                    Instance.UpdateProfile(profile);
                }
            };
        }

        /// <summary>
        /// Function intended to be used as a callback for input elements for conveniently updating a profile
        /// </summary>
        /// <param name="updater">Delegate to update the given profile with the given value</param>
        /// <returns>Callback to give to an input element's onValueChanged event</returns>
        public static UnityAction<float> UpdateProfileCB(ProfileFloatUpdater updater) {
            return (float value) => {
                Profile profile = Instance.ActiveProfile;
                updater(value, ref profile);
                Instance.UpdateProfile(profile);
            };
        }

        /// <summary>
        /// Function intended to be used as a callback for input elements for conveniently updating a profile
        /// </summary>
        /// <param name="updater">Delegate to update the given profile with the given value</param>
        /// <returns>Callback to give to an input element's onValueChanged event</returns>
        public static UnityAction<bool> UpdateProfileCB(ProfileBoolUpdater updater) {
            return (bool value) => {
                Profile profile = Instance.ActiveProfile;
                updater(value, ref profile);
                Instance.UpdateProfile(profile);
            };
        }
    }
}
