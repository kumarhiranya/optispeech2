using Optispeech.Documentation;
using UnityEngine;

namespace Optispeech.Avatar {

    /// <summary>
    /// Utility behaviour to match this object's rotation to another.
    /// Used on our "inner" head (which is used to help occlude body parts
    /// to help the avatar have a smooth fade out when the camera is close,
    /// as opposed to just clipping through) to make its jaw match the
    /// "outer" head's jaw
    /// </summary>
    public class RotationCopyCat : MonoBehaviour {

        /// <summary>
        /// The target transform to copy the rotation from every frame
        /// </summary>
        [SerializeField]
        private Transform rotationSource = default;

        [HideInDocumentation]
        private void LateUpdate() {
            transform.rotation = rotationSource.rotation;
        }
    }
}
