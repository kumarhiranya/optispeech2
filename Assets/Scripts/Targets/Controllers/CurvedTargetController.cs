using Optispeech.Documentation;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;

namespace Optispeech.Targets.Controllers {

    /// <summary>
    /// Controls an oscillating target, which takes two points and oscillates between them with a given frequency
    /// </summary>
    public class CurvedTargetController : TargetController {

        /// <summary>
        /// Basic elliptical trajectory parameters:
        ///     startPosition: start point for the trajectory
        ///     semiMajor, semiMinor: Semi-major (horizontal) and semi-minor (vertical) axis for the ellipse
        ///     frequency: no. of cycles per second for the target
        /// </summary>
        public Vector3 startPosition;
        public float hAmp, vAmp, frequency;

        private float angle, angularSpeed;
        private Vector3 ellipseCenter, ellipseRadius;

        public Vector3 GetEllipseRadius(float hAmp, float vAmp)
        {
            return new Vector3(0f, vAmp, hAmp);
        }

        public float GetAngularSpeed(float frequency)
        {
            return 180 * frequency;
        }

        public Vector3 GetEllipseCenter(Vector3 startPos, float hAmp)
        {
            return startPos + new Vector3(0f, 0f, hAmp);
        }

        public float GetAngle(float angularSpeed, long currTime)
        {
            // input angular speed in degrees/sec, currTime in ms
            // outputs angle in degrees
            float angle = (angularSpeed * currTime / 1000) % 180;
            if (angle < 90) { angle = 180 - angle; }
            return angle;
        }

        public Vector3 PointOnEllipse(Vector3 center, Vector3 axes, float angle)
        {
            //Rotation on z-y plane, angle being the arctan(y/z), i.e. angle between +z and +y in counter-clockwise direction, angle in degrees
            float tempAngle = angle*Mathf.PI/180;
            return new Vector3(center.x, center.y + axes.y * Mathf.Sin(tempAngle), center.z + axes.z * Mathf.Cos(tempAngle));
        }

        [HideInDocumentation]
        public override long GetCycleDuration() {
            return Mathf.RoundToInt(1000 / frequency);
        }

        [HideInDocumentation]
        public override Vector3 GetTargetPosition(long currTime)
        {
            // Debug.Log(string.Format("Parsed values from GetTargetPosition: Startposition:{0}, {1}, {2}, vAmp:{3}, hAmp:{4}, freq:{5}", startPosition.x, startPosition.y, startPosition.z, vAmp, hAmp, frequency));
            angularSpeed = GetAngularSpeed(frequency);
            ellipseCenter = GetEllipseCenter(startPosition, hAmp);
            ellipseRadius = GetEllipseRadius(hAmp, vAmp);
            // Debug.Log(string.Format("Calculated values: ellipseCenter:{0}, {1}, {2}, angularSpeed:{3}, ellipseRadius:{4}, {5}, {6}", ellipseCenter.x, ellipseCenter.y, ellipseCenter.z, angularSpeed,
            // ellipseRadius.x, ellipseRadius.y, ellipseRadius.z));       
            angle = GetAngle(angularSpeed, currTime);
            Debug.Log(string.Format("Calculated values: angle: {0}, angularSpeed:{1}", angle, angularSpeed));
            return PointOnEllipse(ellipseCenter, ellipseRadius, angle);
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
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 3], out vAmp);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 4], out hAmp);
            float.TryParse(values[NUM_BASE_CONFIG_VALUES + 5], out frequency);
            // Debug.Log(string.Format("Parsed values from ApplyConfigFromString: Startposition:{0}, {1}, {2}, vAmp:{3}, hAmp:{4}, freq:{5}", startPosition.x, startPosition.y, startPosition.z, vAmp, hAmp, frequency));
            
        }

        [HideInDocumentation]
        public override string ToString() {
            return base.ToString() + "\t" + startPosition.x + "\t" + startPosition.y + "\t" + startPosition.z + "\t" + vAmp + "\t" + hAmp + "\t" + frequency;
        }
    }
}
