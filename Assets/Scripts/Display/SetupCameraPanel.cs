using Optispeech.Documentation;
using Optispeech.Profiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Display {

    /// <summary>
    /// This sets up a camera panel to include buttons to move the camera to preset positions and rotations.
    /// Each child transform of this gameObject will be a different preset
    /// </summary>
    public class SetupCameraPanel : MonoBehaviour {

        /// <summary>
        /// Reference to the content container of the camera panel, to add the preset buttons to
        /// </summary>
        [SerializeField]
        private RectTransform cameraPanelContent = default;
        /// <summary>
        /// Button prefab to create each preset button.
        /// Must have a <see cref="Button"/> and <see cref="TextMeshProUGUI"/> component somewhere in the prefab (they can be in children)
        /// </summary>
        [SerializeField]
        private GameObject buttonPrefab = default;

        [HideInDocumentation]
        private void Awake() {
            for (int i = 0; i < transform.childCount; i++) {
                // Create button
                GameObject newButton = Instantiate(buttonPrefab, cameraPanelContent);

                // Set its text
                newButton.GetComponentInChildren<TextMeshProUGUI>().SetText(transform.GetChild(i).gameObject.name);

                // Set it up to change the camera view on click
                Transform targetView = transform.GetChild(i);
                newButton.GetComponent<Button>().onClick.AddListener(() => {
                    Camera.main.transform.SetPositionAndRotation(targetView.position, targetView.rotation);

                    // Save camera settings to profile
                    ProfileManager.Profile profile = ProfileManager.Instance.ActiveProfile;
                    profile.cameraPos = Camera.main.transform.position;
                    profile.cameraRot = Camera.main.transform.localEulerAngles;
                    ProfileManager.Instance.UpdateProfile(profile);
                });
            }
        }
    }
}
