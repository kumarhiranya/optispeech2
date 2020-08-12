using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Optispeech.Profiles {

    /// <summary>
    /// A UI object that represents a profile and allows it to be selected, renamed, and deleted
    /// </summary>
    public class ProfileSelector : MonoBehaviour {

        /// <summary>
        /// Label for the name of the profile this selector represents
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI profileNameLabel = default;
        /// <summary>
        /// Input field used for renaming the profile this selector represents
        /// </summary>
        [SerializeField]
        private TMP_InputField profileNameInput = default;
        /// <summary>
        /// Toggle that determines if the profile this selector represents is being renamed. Saves the profile name when toggled off.
        /// </summary>
        [SerializeField]
        private Toggle renameProfileToggle = default;
        /// <summary>
        /// Image that shows the status of whether or not the profile this selector represents is being renamed
        /// </summary>
        [SerializeField]
        private Image renameProfileImage = default;
        /// <summary>
        /// The sprite to show on <see cref="renameProfileImage"/> when the profile this selector represents is not being renamed
        /// </summary>
        [SerializeField]
        private Sprite renameProfileSprite = default;
        /// <summary>
        /// The sprite to show on <see cref="renameProfileImage"/> when the profile this selector represents is being renamed
        /// </summary>
        [SerializeField]
        private Sprite saveProfileNameSprite = default;
        /// <summary>
        /// The button to duplicate the profile this selector represents
        /// </summary>
        [SerializeField]
        private Button duplicateProfileButton = default;
        /// <summary>
        /// The button to delete the profile this selector represents
        /// </summary>
        [SerializeField]
        private Button deleteProfileButton = default;
        /// <summary>
        /// The image on the toggle that changes whether or not the profile this selector represents is the active profile.
        /// This is used to change the interactivity of the toggle dependent on whether or not the profile this selector
        /// represents is being renamed
        /// </summary>
        [SerializeField]
        private Image toggleImage = default;

        /// <summary>
        /// The toggle that changes whether or not the profile this selector represents is the active profile
        /// </summary>
        public Toggle toggle = default;

        /// <summary>
        /// Sets up this selector with the given profile
        /// </summary>
        /// <param name="panel">The profile panel this selector will be added to</param>
        /// <param name="profile">The profile to represent with this selector</param>
        public void Init(ProfilePanel panel, ProfileManager.Profile profile) {
            profileNameLabel.text = profile.profileName;
            renameProfileToggle.onValueChanged.AddListener((value) => {
                if (value) {
                    // Enable editing of the profile name
                    renameProfileImage.sprite = saveProfileNameSprite;
                    profileNameInput.text = profileNameLabel.text;
                    profileNameLabel.gameObject.SetActive(false);
                    profileNameInput.gameObject.SetActive(true);
                    profileNameInput.ActivateInputField();
                    toggleImage.raycastTarget = false;
                } else if (profileNameInput.text != "") {
                    // Save profile name
                    string oldProfileName = profileNameLabel.text;
                    string newProfileName = profileNameInput.text;

                    CloseRename();

                    profileNameLabel.text = newProfileName;
                    profile.profileName = newProfileName;

                    ProfileManager.Instance.UpdateProfile(profile, panel.GetProfileIndex(this));
                }
            });
            profileNameInput.onDeselect.AddListener((s) => CloseRename());
            profileNameInput.onSubmit.AddListener((s) => {
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    CloseRename();
                    return;
                }
                renameProfileToggle.isOn = false;
            });
            duplicateProfileButton.onClick.AddListener(() => {
                ProfileManager.Profile duplicate = profile;
                duplicate.profileName += " (copy)";
                ProfileManager.Instance.AddProfile(duplicate);
            });
            deleteProfileButton.onClick.AddListener(() => ProfileManager.Instance.DeleteProfile(panel.GetProfileIndex(this)));

            toggle.onValueChanged.AddListener((value) => {
                if (value) ProfileManager.Instance.LoadProfile(panel.GetProfileIndex(this));
            });
        }

        /// <summary>
        /// Change the selector back to normal when renaming is either cancelled or saved
        /// </summary>
        private void CloseRename() {
            renameProfileImage.sprite = renameProfileSprite;
            profileNameLabel.gameObject.SetActive(true);
            profileNameInput.gameObject.SetActive(false);
            toggleImage.raycastTarget = true;
            renameProfileToggle.SetIsOnWithoutNotify(false);
        }
    }
}
