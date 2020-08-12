using Optispeech.Data;
using Optispeech.Documentation;
using UnityEngine;

namespace Optispeech.Avatar {

    /// <summary>
    /// Controls the avatar's tongue, based on the rigged joints in the tongue model.
    /// Since these are the rigged joints, we can't have more of less of them without updating the model/rig.
    /// </summary>
    public class TongueController : MonoBehaviour {

        /// <summary>
        /// Utility struct for saving and loading position and rotation from a transform
        /// </summary>
        private struct PositionAndRotation {
            /// <summary> Position to save </summary>
            public Vector3 position;
            /// <summary> Rotation to save </summary>
            public Quaternion rotation;

            /// <summary>
            /// Constructor that will take the position and rotation from the provided transform
            /// </summary>
            /// <param name="transform">The transform to copy the position and rotation from</param>
            public PositionAndRotation(Transform transform) {
                position = transform.position;
                rotation = transform.rotation;
            }

            /// <summary>
            /// Applies the saved position and rotation to the target transform
            /// </summary>
            /// <param name="transform">The transform to copy the position and rotation to</param>
            public void Apply(Transform transform) {
                transform.SetPositionAndRotation(position, rotation);
            }
        }

        // Not using an array for the joints because the points have a specific meaning, and are explicitly not arbitrary
        /// <summary>
        /// The rigged joint at the base of the tongue model
        /// </summary>
        [Header("Joints")]
        [SerializeField]
        private Transform rootJoint = default;
        /// <summary>
        /// The rigged joint slightly above the base of the tongue model
        /// </summary>
        [SerializeField]
        private Transform farLowerJoint = default;
        /// <summary>
        /// The rigged joint slightly below the center of the tongue model
        /// </summary>
        [SerializeField]
        private Transform lowerJoint = default;
        /// <summary>
        /// The rigged joint at the center of the tongue model
        /// </summary>
        [SerializeField]
        private Transform centerJoint = default;
        /// <summary>
        /// The rigged joint left of the center of the tongue model
        /// </summary>
        [SerializeField]
        private Transform leftJoint = default;
        /// <summary>
        /// The rigged joint right of the center of the tongue model
        /// </summary>
        [SerializeField]
        private Transform rightJoint = default;
        /// <summary>
        /// The rigged joint slightly above the center of the tongue model
        /// </summary>
        [SerializeField]
        private Transform upperJoint = default;
        /// <summary>
        /// The rigged joint slightly below the tip of the tongue model
        /// </summary>
        [SerializeField]
        private Transform farUpperJoint = default;
        /// <summary>
        /// The rigged joint at the tip of the tongue model
        /// </summary>
        [SerializeField]
        private Transform tipJoint = default;
        /// <summary>
        /// The rigged joint that, when rotated, controls the angle the jaw makes
        /// </summary>
        [SerializeField]
        private Transform jawRot = default;

        // Each possible sensor marker has some number of "weights" as children, that influence the tongue rig
        /// <summary>
        /// The sensor marker on the tip of the tongue. Also has three children the influence the rig
        /// </summary>
        [Header("Sensor Markers")]
        [SerializeField]
        private Transform tipSensor = default;
        /// <summary>
        /// The sensor marker on the tongue's dorsum. Also has two children that influence the rig
        /// </summary>
        [SerializeField]
        private Transform dorsumSensor = default;
        /// <summary>
        /// The sensor marker on the right of the tongue. Also has two children that influence the rig
        /// </summary>
        [SerializeField]
        private Transform rightSensor = default;
        /// <summary>
        /// The sensor marker on the left of the tongue. Also has two children that influence the rig
        /// </summary>
        [SerializeField]
        private Transform leftSensor = default;
        /// <summary>
        /// The sensor marker on the back of the tongue. Also has three children that influence the rig
        /// </summary>
        [SerializeField]
        private Transform backSensor = default;
        /// <summary>
        /// The sensor marker at the jaw's rotation pivot
        /// </summary>
        [SerializeField]
        private Transform jawSensor = default;

        /// <summary>
        /// Tracks whether or not we've been moved from our default state. If told to reset and we haven't moved,
        /// we'll just do nothing
        /// </summary>
        private bool hasMoved = false;

        /// <summary> Saved position and rotation of the root joint in our default state </summary>
        /// <see cref="rootJoint"/>
        private PositionAndRotation initialRootJoint;
        /// <summary> Saved position and rotation of the far lower joint in our default state </summary>
        /// <see cref="farLowerJoint"/>
        private PositionAndRotation initialFarLowerJoint;
        /// <summary> Saved position and rotation of the lower joint in our default state </summary>
        /// <see cref="lowerJoint"/>
        private PositionAndRotation initialLowerJoint;
        /// <summary> Saved position and rotation of the center joint in our default state </summary>
        /// <see cref="centerJoint"/>
        private PositionAndRotation initialCenterJoint;
        /// <summary> Saved position and rotation of the left joint in our default state </summary>
        /// <see cref="leftJoint"/>
        private PositionAndRotation initialLeftJoint;
        /// <summary> Saved position and rotation of the right joint in our default state </summary>
        /// <see cref="rightJoint"/>
        private PositionAndRotation initialRightJoint;
        /// <summary> Saved position and rotation of the upper joint in our default state </summary>
        /// <see cref="upperJoint"/>
        private PositionAndRotation initialUpperJoint;
        /// <summary> Saved position and rotation of the far upper joint in our default state </summary>
        /// <see cref="farUpperJoint"/>
        private PositionAndRotation initialFarUpperJoint;
        /// <summary> Saved position and rotation of the tip joint in our default state </summary>
        /// <see cref="tipJoint"/>
        private PositionAndRotation initialTipJoint;
        /// <summary> Saved position and rotation of the jaw rotation joint in our default state </summary>
        /// <see cref="jawRot"/>
        private PositionAndRotation initialJawRot;

        /// <summary> Saved position and rotation of the tongue tip sensor marker in our default state </summary>
        /// <see cref="tipSensor"/>
        private PositionAndRotation initialTipSensor;
        /// <summary> Saved position and rotation of the tongue dorsum sensor marker in our default state </summary>
        /// <see cref="dorsumSensor"/>
        private PositionAndRotation initialDorsumSensor;
        /// <summary> Saved position and rotation of the tongue right sensor marker in our default state </summary>
        /// <see cref="rightSensor"/>
        private PositionAndRotation initialRightSensor;
        /// <summary> Saved position and rotation of the tongue left sensor marker in our default state </summary>
        /// <see cref="leftSensor"/>
        private PositionAndRotation initialLeftSensor;
        /// <summary> Saved position and rotation of the tongue back sensor marker in our default state </summary>
        /// <see cref="backSensor"/>
        private PositionAndRotation initialBackSensor;

        [HideInDocumentation]
        private void Awake() {
            // Store the initial positions and rotations of each joint and sensor marker
            initialRootJoint = new PositionAndRotation(rootJoint);
            initialFarLowerJoint = new PositionAndRotation(farLowerJoint);
            initialLowerJoint = new PositionAndRotation(lowerJoint);
            initialCenterJoint = new PositionAndRotation(centerJoint);
            initialLeftJoint = new PositionAndRotation(leftJoint);
            initialRightJoint = new PositionAndRotation(rightJoint);
            initialUpperJoint = new PositionAndRotation(upperJoint);
            initialFarUpperJoint = new PositionAndRotation(farUpperJoint);
            initialTipJoint = new PositionAndRotation(tipJoint);
            initialJawRot = new PositionAndRotation(jawRot);

            initialTipSensor = new PositionAndRotation(tipSensor);
            initialDorsumSensor = new PositionAndRotation(dorsumSensor);
            initialRightSensor = new PositionAndRotation(rightSensor);
            initialLeftSensor = new PositionAndRotation(leftSensor);
            initialBackSensor = new PositionAndRotation(backSensor);
        }

        [HideInDocumentation]
        private void OnEnable() {
            Reset();
        }

        /// <summary>
        /// If the rig has moved out of its default state,
        /// apply all the initial positions and rotations to revert back
        /// </summary>
        public void Reset() {
            if (hasMoved) {
                hasMoved = false;

                // Reset each joint and sensor marker to its initial position and rotation
                initialRootJoint.Apply(rootJoint);
                initialFarLowerJoint.Apply(farLowerJoint);
                initialLowerJoint.Apply(lowerJoint);
                initialCenterJoint.Apply(centerJoint);
                initialLeftJoint.Apply(leftJoint);
                initialRightJoint.Apply(rightJoint);
                initialUpperJoint.Apply(upperJoint);
                initialFarUpperJoint.Apply(farUpperJoint);
                initialTipJoint.Apply(tipJoint);

                initialTipSensor.Apply(tipSensor);
                initialDorsumSensor.Apply(dorsumSensor);
                initialRightSensor.Apply(rightSensor);
                initialLeftSensor.Apply(leftSensor);
                initialBackSensor.Apply(backSensor);
                jawRot.rotation = initialJawRot.rotation;
            }
        }

        // TODO how to handle sensors that don't have a value
        /// <summary>
        /// Updates the model so the tongue rig matches the data given
        /// </summary>
        /// <param name="data">The transformed data to apply to the tongue rig</param>
        public void UpdateRig(TransformedData data) {
            hasMoved = true;

            // Calculate jaw rotation based on the jaw sensor relative position to the center of the head
            if (data.jaw.HasValue) jawRot.rotation = Quaternion.LookRotation(data.jaw.Value.position - jawRot.transform.position);

            // Position each tongue sensor marker
            // Note each sensor has some number of children who will also be moving
            // so as to maintain the same relative position to their parent
            if (data.tongueTip.HasValue) tipSensor.position = data.tongueTip.Value.position + data.tongueTip.Value.postOffset;
            if (data.tongueDorsum.HasValue) dorsumSensor.position = data.tongueDorsum.Value.position + data.tongueDorsum.Value.postOffset;
            if (data.tongueLeft.HasValue) leftSensor.position = data.tongueLeft.Value.position + data.tongueLeft.Value.postOffset;
            if (data.tongueRight.HasValue) rightSensor.position = data.tongueRight.Value.position + data.tongueRight.Value.postOffset;
            if (data.tongueBack.HasValue) backSensor.position = data.tongueBack.Value.position + data.tongueBack.Value.postOffset;

            // Update the joints on our rig
            // The weights are specific to the tongue model/rig we're using
            if (data.tongueTip.HasValue) tipJoint.rotation = data.tongueTip.Value.rotation;
            tipJoint.position = tipSensor.GetChild(2).position;

            farUpperJoint.rotation = tipJoint.rotation;
            farUpperJoint.position = tipSensor.GetChild(1).position;

            upperJoint.rotation = tipJoint.rotation;
            upperJoint.position = (tipSensor.GetChild(0).position + dorsumSensor.GetChild(1).position + rightSensor.GetChild(1).position + leftSensor.GetChild(1).position) / 4;

            if (data.tongueRight.HasValue) rightJoint.rotation = data.tongueRight.Value.rotation;
            rightJoint.position = rightSensor.GetChild(0).position;

            if (data.tongueLeft.HasValue) leftJoint.rotation = data.tongueLeft.Value.rotation;
            leftJoint.position = leftSensor.GetChild(0).position;

            if (data.tongueDorsum.HasValue) centerJoint.rotation = data.tongueDorsum.Value.rotation;
            centerJoint.position = dorsumSensor.GetChild(0).position;

            if (data.tongueBack.HasValue) lowerJoint.rotation = data.tongueBack.Value.rotation;
            lowerJoint.position = backSensor.GetChild(2).position;

            if (data.jaw.HasValue) {
                Quaternion a = lowerJoint.rotation;
                Quaternion b = data.jaw.Value.rotation;

                farLowerJoint.rotation = Quaternion.Lerp(a, b, .5f);
                farLowerJoint.position = (backSensor.GetChild(1).position + this.jawSensor.GetChild(2).position) / 2;

                rootJoint.rotation = Quaternion.Lerp(a, b, .75f);
                rootJoint.position = (backSensor.GetChild(0).position * 3 + this.jawSensor.GetChild(1).position) / 4;
            }
        }
    }
}
