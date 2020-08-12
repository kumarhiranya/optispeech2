using Optispeech.Data.Sources;
using Optispeech.Documentation;
using Optispeech.Profiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Optispeech.Advanced {

    /// <summary>
    /// Panel to update the filter strength on the low pass filter and advanced data source settings
    /// </summary>
    public class AdvancedPanel : MonoBehaviour {

        /// <summary>
        /// Slider for the user to change the strength of the low pass filter
        /// </summary>
        [SerializeField]
        private Slider strengthField = default;
        /// <summary>
        /// Number field for the WaveFront data source host
        /// </summary>
        [SerializeField]
        private TMP_InputField hostField = default;
        /// <summary>
        /// Number field for the WaveFront data source port
        /// </summary>
        [SerializeField]
        private TMP_InputField portField = default;

        [HideInDocumentation]
        private void Start() {
            strengthField.value = ProfileManager.Instance.ActiveProfile.lpfStrength;
            strengthField.onValueChanged.AddListener(value => {
                ProfileManager.Profile profile = ProfileManager.Instance.ActiveProfile;
                profile.lpfStrength = Mathf.RoundToInt(value);
                ProfileManager.Instance.UpdateProfile(profile);
            });
            hostField.text = WaveFrontDataSource.Host;
            hostField.onValueChanged.AddListener(value => { WaveFrontDataSource.Host = value; });
            portField.text = WaveFrontDataSource.Port.ToString();
            portField.onValueChanged.AddListener(value => { if (int.TryParse(value, out int intValue)) WaveFrontDataSource.Port = intValue; });

            ProfileManager.Instance.onProfileChange.AddListener((profile) => {
                strengthField.value = profile.lpfStrength;
            });
        }
    }
}
