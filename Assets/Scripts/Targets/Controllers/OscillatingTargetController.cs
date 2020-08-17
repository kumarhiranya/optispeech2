using Optispeech.Documentation;
using UnityEngine;

namespace Optispeech.Targets.Controllers {

    /// <summary>
    /// Controls an oscillating target, which takes two points and oscillates between them with a given frequency
    /// </summary>
    public class OscillatingTargetController : TargetController {

        /// <summary>
        /// The first point to oscillate between
        /// </summary>
        public Vector3 startPosition;
        /// <summary>
        /// The second point to oscillate between
        /// </summary>
        public Vector3 endPosition;
        /// <summary>
        /// How many oscillations per second
        /// </summary>
        public float frequency;

        [HideInDocumentation]
        public override Vector3 GetTargetPosition(long currTime) {
            // We multiply frequency by 4 so that frequency is one full cycle not just a quarter
            // Debug.Log(string.Format("Calculating targetposition"));

            return Vector3.Lerp(startPosition, endPosition, (Mathf.Cos(frequency * 2 * Mathf.PI * currTime / 1000f) + 1) / 2f);
        }

        [HideInDocumentation]
        public override long GetCycleDuration() {
            return Mathf.RoundToInt(1000 / frequency);
        }

        [HideInDocumentation]
        public override void ApplyConfigFromString(string config) {
            base.ApplyConfigFromString(config);
            string[] values = config.Split('\t');
            if (values.Length < NUM_BASE_CONFIG_VALUES + 7)
                return;
            float.TryParse(values[NUM_BASE_CONFIG_VALUES], out startPosition.x);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 1], out startPosition.y);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 2], out startPosition.z);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 3], out endPosition.x);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 4], out endPosition.y);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 5], out endPosition.z);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 6], out frequency);
            // Debug.Log(string.Format("Parsed values: Startposition:{0}, {1}, {2}, Endposition:{3}, {4}, {5}", startPosition.x, startPosition.y, startPosition.z, endPosition.x, endPosition.y, endPosition.z));
        }

        [HideInDocumentation]
        public override string ToString() {
            return base.ToString() + "\t" + startPosition.x + "\t" + startPosition.y + "\t" + startPosition.z + "\t" + endPosition.x + "\t" + endPosition.y + "\t" + endPosition.z + "\t" + frequency;
        }
    }
}
