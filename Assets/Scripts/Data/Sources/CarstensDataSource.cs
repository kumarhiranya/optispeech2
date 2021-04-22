using UnityEngine;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using Optispeech.Sensors;
using Optispeech.Documentation;
using System.Linq;

namespace Optispeech.Data.Sources {

    /// <summary>
    /// A data source reader that reads from the WaveFront real-time API.
    /// Written to specification from the Real-Time API section of the
    /// <see href="https://support.ndigital.com/downloads/documents/guides/IL-1070187_rev_007.pdf">
    /// WaveFront manual revision 7.
    /// </see>
    /// </summary>
    public class CarstensDataSource : DataSourceReader {

        /// <summary>
        /// The host to try to connect to
        /// </summary>
        private static string host = "127.0.0.1";
        /// <summary>
        /// The port to try to connect to. The spec says this should always be 3030, but is configurable just
        /// in case of things like proxies or anything else that could cause a problem
        /// </summary>
        private static int port = 3030;
        /// <summary>
        /// The <see cref="CarstensDataSource"/> instance on the first <see cref="DataSourceDescription"/> with a <see cref="CarstensDataSource"/>
        /// on it's <see cref="DataSourceDescription.readerPrefab"/>
        /// </summary>
        private static CarstensDataSource mainDataSource;

        /// <summary>
        /// How long to wait to connect to WaveFront before giving up and assuming WaveFront just isn't available
        /// </summary>
        [SerializeField]
        private int connectTimeout = 1000;

        /// <summary>
        /// A client controller used to communicate with the WaveFront realtime API.
        /// This is static so that when this data source is instantiated, it shares the
        /// same controller that originally made the connection from the prefab
        /// </summary>
        private static TcpClientController clientController = null;

        /// <summary>
        /// This data source gets frames from WaveFront in realtime, which may not be 1:1 with
        /// rendered frames in OptiSpeech. To handle this, any read frames are put into this queue
        /// and ReadFrame will pass the next one, or wait if the queue is empty.
        /// </summary>
        private Queue<DataFrame> dataFrameQueue = new Queue<DataFrame>();

        /// <summary>
        /// Property that provides public access to <see cref="host"/> that saves to PlayerPrefs on write
        /// </summary>
        public static string Host {
            get => host;
            set {
                host = value;
                PlayerPrefs.SetString("wavefront-host", host);
                mainDataSource.statusChangeEvent.Invoke(mainDataSource.GetCurrentStatus());
            }
        }

        /// <summary>
        /// Property that provides public access to <see cref="port"/> that saves to PlayerPrefs on write
        /// </summary>
        public static int Port {
            get => port;
            set {
                port = value;
                PlayerPrefs.SetInt("wavefront-port", port);
                mainDataSource.statusChangeEvent.Invoke(mainDataSource.GetCurrentStatus());
            }
        }

        /// <summary>
        /// Loads host and port values from PlayerPrefs when the scene is loaded
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadAdvancedSettings() {
            // When changing advanced settings this data source will need to be refreshed, so
            // find the prefab for this data source
            mainDataSource = Resources.LoadAll("Data Source Descriptions", typeof(DataSourceDescription)).Cast<DataSourceDescription>()
                .Where(d => d.readerPrefab.TryGetComponent(out CarstensDataSource source))
                .FirstOrDefault()
                .readerPrefab.GetComponent<CarstensDataSource>();

            host = PlayerPrefs.GetString("wavefront-host", host);
            port = PlayerPrefs.GetInt("wavefront-port", port);
        }

        [HideInDocumentation]
        protected override void StartThread() {
            base.StartThread();
            SendCommand("StreamFrames AllFrames");
        }

        [HideInDocumentation]
        public override DataSourceReaderStatus GetCurrentStatus() {
            // If this gets called multiple times, make sure to close the previous connection
            if (clientController != null) {
                clientController.Dispose();
            }

            clientController = new TcpClientController();

            // WaveFront is big endian, so if we're big endian then we need to flip each value before converting it
            clientController.flipEndian = BitConverter.IsLittleEndian;

            // Make our client controller change our status on succeed and fail
            clientController.onFail.AddListener(() => statusChangeEvent.Invoke(DataSourceReaderStatus.UNAVAILABLE));
            clientController.onSuccess.AddListener(() => statusChangeEvent.Invoke(DataSourceReaderStatus.AVAILABLE));
            // Start our connection attempt
            clientController.Connect(port, connectTimeout, host);

            // Temporarily return unknown status while our connection attempt is processed
            return DataSourceReaderStatus.UNKNOWN;
        }

        [HideInDocumentation]
        public override SensorConfiguration[] GetDefaultSensorConfigurations() {
            return new SensorConfiguration[] {
                new SensorConfiguration { id = 0, type = SensorType.FOREHEAD, postOffset = Vector3.zero },
                new SensorConfiguration { id = 1, type = SensorType.LEFT_EAR, postOffset = Vector3.zero },
                new SensorConfiguration { id = 2, type = SensorType.RIGHT_EAR, postOffset = Vector3.zero },
                new SensorConfiguration { id = 3, type = SensorType.TONGUE_TIP, postOffset = Vector3.zero },
                new SensorConfiguration { id = 4, type = SensorType.TONGUE_DORSUM, postOffset = Vector3.zero },
                new SensorConfiguration { id = 5, type = SensorType.TONGUE_RIGHT, postOffset = Vector3.zero },
                new SensorConfiguration { id = 6, type = SensorType.TONGUE_LEFT, postOffset = Vector3.zero },
                new SensorConfiguration { id = 7, type = SensorType.TONGUE_BACK, postOffset = Vector3.zero },
                new SensorConfiguration { id = 8, type = SensorType.JAW, postOffset = Vector3.zero },
                new SensorConfiguration { id = 9, type = SensorType.OTHER, postOffset = Vector3.zero }
            };
        }

        [HideInDocumentation]
        protected override DataFrame ReadFrame() {
            // We can receive multiple frames from NDI Wave at once,
            // so we'll create our own buffer that'll get fed into
            // the "main" buffer for this data source, plus any other
            // handling our parent class performs
            if (dataFrameQueue.Count == 0)
                ReadPacket();

            return dataFrameQueue.Dequeue();
        }

        [HideInDocumentation]
        protected override void Cleanup() {
            // Close our connection to the NDI Wave
            SendCommand("Bye");

            if (clientController != null) {
                clientController.Dispose();
                clientController = null;
            }   
        }

        [HideInDocumentation]
        public override void StartSweep(string folderPath, string sweepName) {
            // Note this is based off the documentation revision 7
            // The current documentation on the WaveFront computer says revision 4,
            // and doesn't contain info on how to start or stop recording. Unfortunately,
            // the recording command is apparently unknown. Confusingly, the example realtime
            // program provided next to the rev4 documentation *does* allow you to start or stop
            // the recordings. (the "packet" parameter has no effect)
            SendCommand("Recording Start,file=\"" + Path.Combine(folderPath, sweepName + "_wavefront.csv") + "\"");
        }

        [HideInDocumentation]
        public override void StopSweep() {
            SendCommand("Recording Stop");
        }

        /// <summary>
        /// This handles each packet received from the WaveFront realtime API
        /// </summary>
        private void ReadPacket() {
            // Wait until next packet is ready
            while (!clientController.stream.DataAvailable)
                Thread.Sleep(1);

            // Order is really important here! This is all following the API described in the NDI Wave manual here

            // Read size of next packet from Wave NDI
            Int32 size = clientController.ReadInt32();
            // Read type of this packet
            Int32 type = clientController.ReadInt32();

            // If its not a data frame packet, ignore it and try the next packet
            if (type != 3) {
                // 8 because we've already read the first 8 bytes of this packet (2 32-bit ints)
                // Note we need a bytearray to store the data in, but we don't actually use it for anything
                byte[] bytes = new byte[size - 8];
                clientController.stream.Read(bytes, 0, size - 8);
                // If its a string, log it
                if (type == 0)
                    Debug.LogError(System.Text.Encoding.ASCII.GetString(bytes));
                if (type == 1)
                    Debug.Log(System.Text.Encoding.ASCII.GetString(bytes));
                ReadPacket();
                return;
            }

            // Read number of components in this data frame
            Int32 numComponents = clientController.ReadInt32();

            for (int i = 0; i < numComponents; i++) {
                // Read size of component
                Int32 componentSize = clientController.ReadInt32();
                // Read type of component
                Int32 componentType = clientController.ReadInt32();

                // It should only ever be 6D, per the docs
                // If we get something different we'll just ignore it and move to the next component
                if (componentType != 4) {
                    // 8 because we've already read the first 8 bytes of this component (2 32-bit ints)
                    // Note we need a bytearray to store the data in, but we don't actually use it for anything
                    byte[] bytes = new byte[componentSize - 8];
                    clientController.stream.Read(bytes, 0, componentSize - 8);
                    continue;
                }

                // Read frame number
                Int32 frameNumber = clientController.ReadInt32();
                // Read frame timestamp
                long timeStamp = clientController.ReadInt64() / 1000;

                // Read number of sensors ("Tools" in the docs)
                Int32 numSensors = clientController.ReadInt32();

                // Create our data frame
                DataFrame frame = new DataFrame {
                    timestamp = timeStamp,
                    sensorData = new SensorData[numSensors]
                };

                // Add each sensor's data to our data frame
                for (int j = 0; j < numSensors; j++) {
                    // Read rotation data. Note it returns Q_0 (Q_w) first
                    // This quaternion should be normalized, so |Q| == 1
                    float Qw = clientController.ReadSingle();
                    float Qx = clientController.ReadSingle();
                    float Qy = clientController.ReadSingle();
                    float Qz = clientController.ReadSingle();

                    // Read positional data
                    float x = clientController.ReadSingle();
                    float y = clientController.ReadSingle();
                    float z = clientController.ReadSingle();

                    // Read RMS marker fit to rigid body error
                    // Note we don't actually include this in our data frame (our program doesn't track RMS error)
                    float error = clientController.ReadSingle();

                    // We can't seem to get any actual status codes via the RTAPI, but if any values are NaN we can
                    // give it an error code ourselves
                    if (float.IsNaN(Qw) || float.IsNaN(Qx) || float.IsNaN(Qy) || float.IsNaN(Qz) ||
                        float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z)) {
                        frame.sensorData[j] = new SensorData {
                            id = j,
                            status = SensorStatus.PROCESSING_ERROR,
                            position = new Vector3(x, y, z),
                            rotation = new Quaternion(Qx, Qy, Qz, Qw)
                        };
                    } else {
                        frame.sensorData[j] = new SensorData {
                            id = j,
                            status = SensorStatus.OK,
                            position = new Vector3(x, y, z),
                            rotation = new Quaternion(Qx, Qy, Qz, Qw)
                        };
                    }
                }

                // Add our data frame to our queue
                dataFrameQueue.Enqueue(frame);
            }

            // This function needs to add at least one item to our queue for ReadFrame to work,
            // so if we ever receive 0 components that can be made into data frames,
            // we'll just have to call this function again to process the next packet
            if (dataFrameQueue.Count == 0)
                ReadPacket();
        }

        /// <summary>
        /// Sends a message to the WaveFront realtime API
        /// </summary>
        /// <param name="message">The message to send</param>
        private void SendCommand(string message) {
            // Convert message into bytes
            byte[] command = System.Text.Encoding.ASCII.GetBytes(message);

            // The NDI Wave expects data in the form of packets with a size and type before the actual data
            // For more information check out the manual
            // Specifically towards the end is the real-time API

            // Send size of packet
            clientController.stream.Write(new byte[] { 0, 0, 0, (byte)(8 + command.Length) }, 0, 4);

            // Send type of packet - command
            clientController.stream.Write(new byte[] { 0, 0, 0, 1 }, 0, 4);

            // Send our command
            clientController.stream.Write(command, 0, command.Length);
        }
    }
}
