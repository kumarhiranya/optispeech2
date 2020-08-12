using UnityEngine;
using System.Linq;
using Optispeech.Data;
using Optispeech.Documentation;

namespace Optispeech.Targets.Controllers {

    /// <summary>
    /// Controls a landmark target, which follows a given sensor
    /// </summary>
    public class LandmarkTargetController : TargetController {

        /// <summary>
        /// The ID of the sensor to use as the landmark
        /// </summary>
        public int id;

        [HideInDocumentation]
        public override Vector3 GetTargetPosition(long currTime) {
            // Default to zeroed out sensor data
            SensorData sensorData = default;

            // Search through last data frame for our sensor
            if (DataSourceManager.Instance.dataSourceReader != null) {
                TransformedData data = DataSourceManager.Instance.dataSourceReader.lastFrame.transformedData;
                if (data.forehead.HasValue && data.forehead.Value.id == id) sensorData = data.forehead.Value;
                else if (data.jaw.HasValue && data.jaw.Value.id == id) sensorData = data.jaw.Value;
                else if (data.leftEar.HasValue && data.leftEar.Value.id == id) sensorData = data.leftEar.Value;
                else if (data.rightEar.HasValue && data.rightEar.Value.id == id) sensorData = data.rightEar.Value;
                else if (data.tongueBack.HasValue && data.tongueBack.Value.id == id) sensorData = data.tongueBack.Value;
                else if (data.tongueDorsum.HasValue && data.tongueDorsum.Value.id == id) sensorData = data.tongueDorsum.Value;
                else if (data.tongueLeft.HasValue && data.tongueLeft.Value.id == id) sensorData = data.tongueLeft.Value;
                else if (data.tongueRight.HasValue && data.tongueRight.Value.id == id) sensorData = data.tongueRight.Value;
                else if (data.tongueTip.HasValue && data.tongueTip.Value.id == id) sensorData = data.tongueTip.Value;
                else if (data.otherSensors != null) sensorData = data.otherSensors.Where(s => s.id == id).FirstOrDefault();
            }

            // Return sensor data's position
            return sensorData.position + sensorData.postOffset;
        }

        [HideInDocumentation]
        public override long GetCycleDuration() {
            // There's no good value for this, so we'll just always show the current frame's accuracy
            return 0;
        }

        [HideInDocumentation]
        public override void ApplyConfigFromString(string config) {
            base.ApplyConfigFromString(config);
            string[] values = config.Split('\t');
            if (values.Length < NUM_BASE_CONFIG_VALUES + 1)
                return;
            int.TryParse(values[NUM_BASE_CONFIG_VALUES], out id);
        }

        [HideInDocumentation]
        public override string ToString() {
            return base.ToString() + "\t" + id;
        }
    }
}
