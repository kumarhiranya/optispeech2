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
        public int pauseTime = 200; // pauseTime in ms

        private float angle, angularSpeed, rawAngle, prevRawAngle = 0f; //angle and angleThresh in degrees, angular speed in radians/sec
        private Vector3 ellipseCenter, ellipseRadius, currPosition, prevPosition;
        private bool init = true;
        private long selfTime, startTime = 0;
        private int nPauses=0;
        
        public Vector3 GetEllipseRadius(float hAmp, float vAmp)
        {
            return new Vector3(0f, vAmp, hAmp);
        }

        public float GetAngularSpeed(float frequency)
        {
            // Returns angular speed in radians/sec
            return Mathf.PI * frequency;
        }

        public float GetLowAngularSpeed(float pauseTime, float angleThresh){
            //Returns angular speed in radians/sec
            return (2*angleThresh*Mathf.PI)/(180*pauseTime);
        }

        public Vector3 GetEllipseCenter(Vector3 startPos, float hAmp)
        {
            return startPos + new Vector3(0f, 0f, hAmp);
        }

        public float GetAngle(float angularSpeed, long currTime)
        {  
            //Returns angle in degrees
            float rawAngle = angularSpeed * currTime / 1000;
            float angle = rawAngle % 180;
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
            Debug.Log(string.Format("Parsed values from GetTargetPosition: Startposition:{0}, {1}, {2}, vAmp:{3}, hAmp:{4}, freq:{5}", startPosition.x, startPosition.y, startPosition.z, vAmp, hAmp, frequency));
            selfTime = currTime - nPauses*pauseTime;
            angularSpeed = GetAngularSpeed(frequency);
            rawAngle = angularSpeed * selfTime/1000;
            if (rawAngle<prevRawAngle){
                startTime = currTime;             
            }
            if(startTime>0 && currTime-startTime<pauseTime) return  prevPosition;
            else if(startTime>0 && currTime-startTime>=pauseTime){
                startTime = 0;
                nPauses++;
            }

            ellipseCenter = GetEllipseCenter(startPosition, hAmp);
            ellipseRadius = GetEllipseRadius(hAmp, vAmp);
            Debug.Log(string.Format("Calculated values: ellipseCenter:{0}, {1}, {2}, angularSpeed:{3}, ellipseRadius:{4}, {5}, {6}", ellipseCenter.x, ellipseCenter.y, ellipseCenter.z, angularSpeed,
            ellipseRadius.x, ellipseRadius.y, ellipseRadius.z));       
            angle = GetAngle(angularSpeed, selfTime);
            currPosition = PointOnEllipse(ellipseCenter, ellipseRadius, angle);
            prevRawAngle = rawAngle;
            prevPosition = currPosition;
            if(init) init = false;
            return currPosition;
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
