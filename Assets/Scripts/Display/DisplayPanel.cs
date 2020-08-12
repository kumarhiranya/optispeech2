using Optispeech.Documentation;
using Optispeech.Profiles;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Display {

    /// <summary>
    /// A panel controller that handles various visibility settings
    /// </summary>
    public class DisplayPanel : MonoBehaviour {

        /// <summary>
        /// The material used on the head part of the avatar
        /// </summary>
        [SerializeField]
        private Material headMaterial = default;
        /// <summary>
        /// The material used on the gums part of the avatar
        /// </summary>
        [SerializeField]
        private Material gumsMaterial = default;
        /// <summary>
        /// The container all reference markers are in
        /// </summary>
        [SerializeField]
        private GameObject markersParent = default;
        /// <summary>
        /// The container all sensor markers are in
        /// </summary>
        [SerializeField]
        private GameObject sensorsParent = default;
        /// <summary>
        /// The container the tongue is in
        /// </summary>
        [SerializeField]
        private GameObject tongueParent = default;

        /// <summary>
        /// Slider for controlling the transparency of <see cref="headMaterial"/>
        /// </summary>
        [SerializeField]
        private Slider headTransparencySlider = default;
        /// <summary>
        /// Slider for controlling the transparency of <see cref="gumsMaterial"/>
        /// </summary>
        [SerializeField]
        private Slider gumsTransparencySlider = default;
        /// <summary>
        /// Toggle for controlling the visibility of <see cref="markersParent"/>
        /// </summary>
        [SerializeField]
        private Toggle markersVisibleToggle = default;
        /// <summary>
        /// Toggle for controlling the visibility of <see cref="sensorsParent"/>
        /// </summary>
        [SerializeField]
        private Toggle sensorsVisibleToggle = default;
        /// <summary>
        /// Toggle for controlling the visibility of <see cref="tongueParent"/>
        /// </summary>
        [SerializeField]
        private Toggle tongueVisibleToggle = default;

        [HideInDocumentation]
        private void Start() {
            headTransparencySlider.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((float value, ref ProfileManager.Profile profile) => profile.headTransparency = value));
            gumsTransparencySlider.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((float value, ref ProfileManager.Profile profile) => profile.gumsTransparency = value));
            markersVisibleToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.markersVisible = value));
            sensorsVisibleToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.sensorsVisible = value));
            tongueVisibleToggle.onValueChanged.AddListener(ProfileManager.UpdateProfileCB((bool value, ref ProfileManager.Profile profile) => profile.tongueVisible = value));

            LoadProfile(ProfileManager.Instance.ActiveProfile);
            ProfileManager.Instance.onProfileChange.AddListener(LoadProfile);
        }

        [HideInDocumentation]
        private void LoadProfile(ProfileManager.Profile profile) {
            headMaterial.color = new Color(headMaterial.color.r, headMaterial.color.g, headMaterial.color.b, profile.headTransparency);
            headTransparencySlider.SetValueWithoutNotify(profile.headTransparency);

            gumsMaterial.color = new Color(gumsMaterial.color.r, gumsMaterial.color.g, gumsMaterial.color.b, profile.gumsTransparency);
            gumsTransparencySlider.SetValueWithoutNotify(profile.gumsTransparency);

            markersParent.SetActive(profile.markersVisible);
            markersVisibleToggle.SetIsOnWithoutNotify(profile.markersVisible);

            sensorsParent.SetActive(profile.sensorsVisible);
            sensorsVisibleToggle.SetIsOnWithoutNotify(profile.sensorsVisible);

            tongueParent.SetActive(profile.tongueVisible);
            tongueVisibleToggle.SetIsOnWithoutNotify(profile.tongueVisible);
        }
    }
}
