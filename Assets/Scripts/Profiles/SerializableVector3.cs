using UnityEngine;

namespace Optispeech.Profiles {

    /// <summary>
    /// A serializable version of a <see cref="Vector3"/>, which will implicitly convert to/from <see cref="Vector3"/>.
    /// This is used for saving <see cref="Vector3"/> values in profiles
    /// </summary>
    public class SerializableVector3 {

        /// <summary>
        /// Constructor that takes x, y, and z components of the <see cref="Vector3"/> this represents
        /// </summary>
        /// <param name="x">The x value for this vector</param>
        /// <param name="y">The y value for this vector</param>
        /// <param name="z">The z value for this vector</param>
        public SerializableVector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// This vector's x component
        /// </summary>
        public float x;
        /// <summary>
        /// This vector's y component
        /// </summary>
        public float y;
        /// <summary>
        /// This vector's z component
        /// </summary>
        public float z;

        /// <summary>
        /// Implicitly converts <see cref="SerializableVector3"/> to <see cref="Vector3"/>
        /// </summary>
        /// <param name="v">The value to convert into a <see cref="Vector3"/></param>
        public static implicit operator Vector3(SerializableVector3 v) => new Vector3(v.x, v.y, v.z);
        /// <summary>
        /// Implicitly converts <see cref="Vector3"/> to <see cref="SerializableVector3"/>
        /// </summary>
        /// <param name="v">The value to convert into a <see cref="SerializableVector3"/></param>
        public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v.x, v.y, v.z);
    }
}
