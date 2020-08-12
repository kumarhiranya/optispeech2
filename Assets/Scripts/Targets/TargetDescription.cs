using UnityEngine;

namespace Optispeech.Targets {

    /// <summary>
    /// ScriptableObject that describes a single target type
    /// </summary>
    [CreateAssetMenu(menuName = "Optispeech/Target Type")]
    public class TargetDescription : ScriptableObject {

        /// <summary>
        /// The name of this target type. Shown in the add target dropdown, and
        /// is used to identify the types of targets in their config strings
        /// </summary>
        public string typeName;
        /// <summary>
        /// A prefab with the target controller to use for these types of targets
        /// </summary>
        public TargetController targetPrefab;
        /// <summary>
        /// A prefab that shows the configuration UI elements for targets of this type
        /// </summary>
        public TargetConfig targetConfigPrefab;
    }
}
