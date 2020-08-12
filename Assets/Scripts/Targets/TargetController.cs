using UnityEngine;
using Optispeech.Data;
using Optispeech.Documentation;
using UnityEngine.UI;
using Optispeech.UI;

namespace Optispeech.Targets {

    /// <summary>
    /// Controls a target
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Tooltip))]
    [RequireComponent(typeof(TrailRenderer))]
    public abstract class TargetController : MonoBehaviour {

        /// <summary>
        /// This is the number of config values written to a config string
        /// This is stored in a constant so implementations can know to offset
        /// their own values by this amount
        /// </summary>
        protected readonly static int NUM_BASE_CONFIG_VALUES = 4;

        /// <summary>
        /// The description of this target, which includes a type name and prefab with this controller on it,
        /// and the prefab for the config to add to the targets panel
        /// </summary>
        [HideInInspector]
        public TargetDescription description;
        /// <summary>
        /// The name of this target
        /// </summary>
        [HideInInspector]
        public string targetId;
        /// <summary>
        /// The renderer for the target marker, for setting radius and color
        /// </summary>
        [HideInInspector]
        public MeshRenderer mesh;
        /// <summary>
        /// The toggle for opening/closing this target's config
        /// </summary>
        [HideInInspector]
        public GameObject configToggle;

        /// <summary>
        /// How visible this target appears
        /// </summary>
        public float visibility = 1;
        /// <summary>
        /// The radius of the target marker as well as how close the user needs to be to be considered "in" the target
        /// </summary>
        public float radius;

        /// <summary>
        /// The trail renderer used to show this target's history, when the config is open
        /// </summary>
        [HideInInspector]
        private TrailRenderer trail;
        /// <summary>
        /// Tooltip used to display this target's name on hover
        /// </summary>
        [HideInInspector]
        private Tooltip tooltip;

        /// <summary>
        /// Returns the current position of this target based on timestamp
        /// </summary>
        /// <param name="currTime">The current time of this data frame</param>
        /// <returns>The position of this target at the given timestamp</returns>
        public abstract Vector3 GetTargetPosition(long currTime);

        /// <summary>
        /// When displaying how accurate the tongue was at matching the targets,
        /// the accuracy is shown in terms of "cycles". This describes how long a cycle is for this target, in ms
        /// </summary>
        /// <returns>The duration of one cycle for this target</returns>
        public abstract long GetCycleDuration();

        /// <summary>
        /// Sets up this target based on a target configuration string from a raw export
        /// </summary>
        /// <param name="config">The configuration of this target as a tab separated values string</param>
        public virtual void ApplyConfigFromString(string config) {
            string[] values = config.Split('\t');
            TargetsManager.Instance.targets.Remove(targetId);
            if (values.Length > 0)
                targetId = values[0];
            TargetsManager.Instance.targets[targetId] = this;
            tooltip.SetText(targetId);
            if (values.Length > 2)
                float.TryParse(values[2], out visibility);
            if (values.Length > 3)
                float.TryParse(values[3], out radius);
        }

        [HideInDocumentation]
        private void OnEnable() {
            mesh = GetComponent<MeshRenderer>();
            trail = GetComponent<TrailRenderer>();
            tooltip = GetComponent<Tooltip>();
        }

        /// <summary>
        /// Updates the target marker to appear in the specified position, with a color determined by the distance from the given sensor (if provided)
        /// </summary>
        /// <param name="targetPosition">The position the marker should appear at</param>
        /// <param name="tongueTipSensor">The sensor, if it exists, this target is tracking</param>
        public virtual void UpdateTarget(Vector3 targetPosition, SensorData? tongueTipSensor) {
            transform.position = targetPosition;
            // Update our material based on distance to the marker
            if (tongueTipSensor.HasValue && mesh != null) {
                // We subtract our local scale (all 3 scale dimensions being equal) so distance is from the tongue tip to the outer edge of the target sphere
                float distance = Vector3.Distance(targetPosition, tongueTipSensor.Value.position + tongueTipSensor.Value.postOffset) - transform.localScale.x;
                mesh.material.color = Color.Lerp(TargetsManager.Instance.closeColor, TargetsManager.Instance.farColor, distance / transform.localScale.x);
                // Apply visibility
                mesh.material.color = new Color(mesh.material.color.r, mesh.material.color.g, mesh.material.color.b, visibility);
            }
        }

        /// <summary>
        /// Creates a tab separated values string of all the configs for this target
        /// </summary>
        /// <returns>Tab separated values string</returns>
        public override string ToString() {
            // ID + type + visibility + radius + any implementation-specific values
            return targetId + "\t" + description.typeName + "\t" + visibility + "\t" + radius;
        }

        /// <summary>
        /// Function that's called whenever this target's config is opened
        /// </summary>
        public virtual void OnOpen() {
            trail.enabled = true;
        }

        /// <summary>
        /// Function that's called whenever this target's config is closed
        /// </summary>
        public virtual void OnClose() {
            trail.enabled = false;
        }
    }
}
