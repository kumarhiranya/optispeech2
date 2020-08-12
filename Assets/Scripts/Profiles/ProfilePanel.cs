using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Optispeech.UI;
using Optispeech.Documentation;

namespace Optispeech.Profiles {

    /// <summary>
    /// Panel for creating, deleting, renaming, and switching between the various registered profiles
    /// </summary>
    public class ProfilePanel : MonoBehaviour {

        /// <summary>
        /// Input field for putting in the name of the new profile to create
        /// </summary>
        [SerializeField]
        private TMP_InputField profileNameInput = default;
        /// <summary>
        /// Button for creating a new profile
        /// </summary>
        [SerializeField]
        private Button newProfileButton = default;
        /// <summary>
        /// Group that all profile buttons will be added to so only one can be active at a time
        /// </summary>
        [SerializeField]
        private ToggleGroup profileToggleContainer = default;
        /// <summary>
        /// Prefab to instantiate for each profile selector
        /// </summary>
        [SerializeField]
        private ProfileSelector profileTogglePrefab = default;

        /// <summary>
        /// List of instantiated profile selectors
        /// </summary>
        private List<ProfileSelector> selectors = new List<ProfileSelector>();
        /// <summary>
        /// This panel
        /// </summary>
        private TogglePanel togglePanel;
        /// <summary>
        /// Active couroutine, if any, that is refreshing this panel
        /// </summary>
        private IEnumerator refreshCoroutine;

        [HideInDocumentation]
        private void Start() {
            ProfileManager.Instance.onProfileChange.AddListener(UpdatePanelTitle);
            ProfileManager.Instance.onProfileAdded.AddListener(AddProfileSelector);
            ProfileManager.Instance.onProfileDeleted.AddListener(RemoveProfileSelector);

            // Setup new profile inputs
            newProfileButton.interactable = false;
            newProfileButton.onClick.AddListener(() => {
                ProfileManager.Profile profile = ProfileManager.CreateProfile();
                profile.profileName = profileNameInput.text;
                ProfileManager.Instance.AddProfile(profile);
                selectors[ProfileManager.Instance.activeProfileIndex].toggle.SetIsOnWithoutNotify(true);
                profileNameInput.text = "";
            });
            profileNameInput.onValueChanged.AddListener((s) => newProfileButton.interactable = s != "");

            // Create profile selectors for each profile
            foreach (ProfileManager.Profile profile in ProfileManager.Instance.profiles) {
                AddProfileSelector(profile);
            }

            // Select active profile, defaulting to the first one
            selectors[ProfileManager.Instance.activeProfileIndex].toggle.SetIsOnWithoutNotify(true);
            UpdatePanelTitle(ProfileManager.Instance.ActiveProfile);
        }

        [HideInDocumentation]
        private void OnEnable() {
            togglePanel = GetComponentInChildren<TogglePanel>();
        }

        /// <summary>
        /// Adds a selector for the given profile
        /// </summary>
        /// <param name="profile">The profile to add a selector for</param>
        private void AddProfileSelector(ProfileManager.Profile profile) {
            GameObject profileToggle = Instantiate(profileTogglePrefab.gameObject, profileToggleContainer.transform);
            ProfileSelector selector = profileToggle.GetComponentInChildren<ProfileSelector>();
            selector.toggle.group = profileToggleContainer;
            selector.Init(this, profile);
            selectors.Add(selector);
            if (refreshCoroutine != null)
                StopCoroutine(refreshCoroutine);
            StartCoroutine(refreshCoroutine = DelayedRefresh());
        }

        /// <summary>
        /// Changes the name of the panel to reflect the name of the active profile
        /// </summary>
        /// <param name="profile"></param>
        private void UpdatePanelTitle(ProfileManager.Profile profile) {
            togglePanel.titleBar.GetComponentInChildren<TextMeshProUGUI>().text = $"Profiles - {profile.profileName}";
        }

        /// <summary>
        /// Removes the selector for the profile at the given index
        /// </summary>
        /// <param name="index">The index in <see cref="selectors"/> of the profile selector to remove</param>
        public void RemoveProfileSelector(int index) {
            Destroy(selectors[index].gameObject);
            selectors.RemoveAt(index);
            if (refreshCoroutine != null)
                StopCoroutine(refreshCoroutine);
            StartCoroutine(refreshCoroutine = DelayedRefresh());
        }

        /// <summary>
        /// Returns the current index of the given profile selector in <see cref="selectors"/>
        /// </summary>
        /// <param name="selector">The profile selector to find the index of</param>
        /// <returns>The index of <paramref name="selector"/> in <see cref="selectors"/></returns>
        public int GetProfileIndex(ProfileSelector selector) {
            return selectors.IndexOf(selector);
        }

        /// <summary>
        /// Used to wait a frame then refresh this panel a second time, so that anything we effected has time to update
        /// </summary>
        /// <returns>Coroutine</returns>
        private IEnumerator DelayedRefresh() {
            togglePanel.Refresh();
            yield return null;
            togglePanel.Refresh();
        }
    }
}
