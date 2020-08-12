using Optispeech.Documentation;
using UnityEngine;

namespace Optispeech.Targets.Controllers {

    /// <summary>
    /// Controls a target that stays in one spot
    /// </summary>
    public class StaticTargetController : TargetController {

        /// <summary>
        /// The position of this target
        /// </summary>
        public Vector3 position;

        [HideInDocumentation]
        public override Vector3 GetTargetPosition(long currTime) {
            return position;
        }

        [HideInDocumentation]
        public override long GetCycleDuration() {
            // Static targets don't move, so just always show the current frame's accuracy
            return 0;
        }

        [HideInDocumentation]
        public override void ApplyConfigFromString(string config) {
            base.ApplyConfigFromString(config);
            string[] values = config.Split('\t');
            if (values.Length < NUM_BASE_CONFIG_VALUES + 3)
                return;
            float.TryParse(values[NUM_BASE_CONFIG_VALUES], out position.x);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 1], out position.y);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 2], out position.z);
        }

        [HideInDocumentation]
        public override string ToString() {
            return base.ToString() + "\t" + position.x + "\t" + position.y + "\t" + position.z;
        }
    }
}
