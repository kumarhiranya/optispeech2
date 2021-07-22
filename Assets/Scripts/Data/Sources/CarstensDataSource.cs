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

        // The documentation for the Carstens EMA is not available online, and you'll have to get a copy
        // from Dr. Katz or someone else with access to the documents. 
        // The way the Carstens works is through a TCP connection to cs5recorder where each command you can give has a response,
        // including one "200 Welcome" response provided at the beginning of the connection.
        // Each response can be a text reply or a binary reply. The latter is just binary data of a fixed size struct
        // depending on the command given, and text replies can have one or more status lines, the last one having a space
        // between the status code and message, all before it having a dash instead. The documentation does not say
        // which commands will have multi-line statuses, so we assume none do for now (obviously testing is required)
        // Certain commands, and the documentation states which commands this applies to, will then contain a text payload
        // that will end with a single line containing only a single period '.'.

        // Values taken from documentation
        static int SystemNumberOfChannels = 24;
        static int SystemNumberOfTX = 9;

        private delegate void TextReplyHandler(int status, string message);
        private delegate void TextReplyPayloadHandler(int status, string message, string payload);
        private delegate void DroppedResponseHandler();
        private static string host = "10.127.40.116";
        private static int port = 30303;
        private static CarstensDataSource mainDataSource;

        private enum ResponseType {
            TEXT,
            TEXT_WITH_PAYLOAD,
            BINARY_DATA,
            BINARY_STATS
        }

        private struct Command {
            public string command;
            public ResponseType responseType;
            public TextReplyHandler textReplyHandler;
            public TextReplyPayloadHandler textReplyPayloadHandler;
            public DroppedResponseHandler droppedResponseHandler;
        }

        private readonly Command sprepare = new Command {
            command = "sprepare",
            responseType = ResponseType.TEXT_WITH_PAYLOAD,
            // TODO no documentation on sprepare response, need to test possible responses
            textReplyPayloadHandler = (int status, string message, string payload) => { },
            droppedResponseHandler = () => { }
        };

        private readonly Command sstart = new Command {
            command = "sstart\n",
            responseType = ResponseType.TEXT_WITH_PAYLOAD,
            // TODO no documentation on sstart response, need to test possible responses
            textReplyPayloadHandler = (int status, string message, string payload) => { },
            droppedResponseHandler = () => { }
        };

        private readonly Command sstop = new Command {
            command = "sstop\n",
            responseType = ResponseType.TEXT_WITH_PAYLOAD,
            // TODO no documentation on sstop response, need to test possible responses
            textReplyPayloadHandler = (int status, string message, string payload) => { },
            droppedResponseHandler = () => { }
        };

        private readonly Command exit = new Command {
            command = "exit\n",
            responseType = ResponseType.TEXT,
            // TODO no documentation on exit response, need to test possible responses
            textReplyHandler = (int status, string message) => { },
            droppedResponseHandler = () => { }
        };

        [SerializeField]
        private int connectTimeout = 1000;

        private TcpClientController clientController = null;
        private int numFailures;

        private Queue<Command> commandsQueue = new Queue<Command>();

        // We override this function so we can tell the NDI Wave to start streaming data
        protected override void StartThread() {
            // Handle welcome response, by ignoring it
            HandleResponse((int status, string message) => { }, () => { });

            commandsQueue.Enqueue(sprepare);

            base.StartThread();
        }

        public override DataSourceReaderStatus GetCurrentStatus() {
            // If this gets called multiple times, make sure to close the previous connection
            if (clientController != null) {
                clientController.Dispose();
            }

            clientController = new TcpClientController();

            // Make our client controller change our status on succeed and fail
            clientController.onFail.AddListener(() => statusChangeEvent.Invoke(DataSourceReaderStatus.UNAVAILABLE));
            clientController.onSuccess.AddListener(() => statusChangeEvent.Invoke(DataSourceReaderStatus.AVAILABLE));

            // Start our connection attempt
            // clientController.Connect(30303, connectTimeout);
            clientController.Connect(port, connectTimeout, host);
            

            // Temporarily return unknown status while our connection attempt is processed
            return DataSourceReaderStatus.UNKNOWN;
        }

        protected override bool IsTimestampProvided() {
            return false;
        }

        protected override DataFrame ReadFrame() {
            // Reset our number of failures
            numFailures = 0;

            // We send all requests in the same thread to ensure responses don't become interlaced or associated with the wrong command
            // After each sent command we wait until it gets a response or is considered dropped, before moving on to the next command
            // Note this is the ReadFrame thread, not the main thread. This is so our sleep statements don't affect the interface
            while (commandsQueue.Count > 0) {
                Command command = commandsQueue.Dequeue();
                switch (command.responseType) {
                    case ResponseType.TEXT:
                        SendCommand(command.command);
                        HandleResponse(command.textReplyHandler, command.droppedResponseHandler);
                        break;
                    case ResponseType.TEXT_WITH_PAYLOAD:
                        SendCommand(command.command);
                        HandleResponse(command.textReplyPayloadHandler, command.droppedResponseHandler);
                        break;
                    default:
                        continue;
                }
            }

            // After the queue is empty we can send our data command and return its response
            SendCommand("data");
            return HandleDataResponse();
        }

        // Close our connection to cs5recorder
        protected override void Cleanup() {
            commandsQueue.Enqueue(exit);

            if (clientController != null)
                clientController.Dispose();
        }

        public override void StartSweep(string folderPath, string sweepName) {
            commandsQueue.Enqueue(sstart);
        }

        public override void StopSweep() {
            commandsQueue.Enqueue(sstop);
        }

        private void HandleResponse(TextReplyPayloadHandler onSuccess, DroppedResponseHandler onFail) {
            if (!Readable(100)) {
                onFail();
                return;
            }

            // Since the text responses work on a line by line basis, we'll pipe our tcp client's stream to a StreamReader
            // Not we create the reader for each response, since we'll also be handling binary responses, and text responses
            // should be infrequent (mostly just starting and stopping sweeps)
            using (StreamReader reader = new StreamReader(clientController.client.GetStream())) {
                int statusCode;
                string message;

                // Read status line(s)
                while (true) {
                    string status = reader.ReadLine();
                    if (status[4] == ' ') {
                        // This is the last status
                        statusCode = int.Parse(status.Substring(0, 3));
                        message = status.Substring(4);
                        break;
                    }
                    // TODO handle multi-line statuses
                }

                // Read text payload
                string payload = "";
                while (true) {
                    string line = reader.ReadLine();
                    if (line == ".") {
                        // End of payload
                        break;
                    }
                    if (payload != "") payload += "\n";
                    payload += line;
                }

                // Pass everything to our response handler
                onSuccess(statusCode, message, payload);
            }
        }

        private void HandleResponse(TextReplyHandler onSuccess, DroppedResponseHandler onFail) {
            if (!Readable(100)) {
                onFail();
                return;
            }

            // Since the text responses work on a line by line basis, we'll pipe our tcp client's stream to a StreamReader
            // Not we create the reader for each response, since we'll also be handling binary responses, and text responses
            // should be infrequent (mostly just starting and stopping sweeps)
            using (StreamReader reader = new StreamReader(clientController.client.GetStream())) {
                int statusCode;
                string message;

                // Read status line(s)
                while (true) {
                    string status = reader.ReadLine();
                    if (status[4] == ' ') {
                        // This is the last status
                        statusCode = int.Parse(status.Substring(0, 3));
                        message = status.Substring(4);
                        break;
                    }
                    // TODO handle multi-line statuses
                }

                // Pass everything to our response handler
                onSuccess(statusCode, message);
            }
        }

        private DataFrame HandleDataResponse() {
            if (!Readable(100)) {
                // Most commands we don't matter about failure count,
                // but we don't want to be stuck in a loop requesting data frames,
                // so after 3 consecutive failures we'll count this data source as disabled
                numFailures++;
                if (numFailures >= 3) {
                    statusChangeEvent.Invoke(DataSourceReaderStatus.UNAVAILABLE);
                    return default;
                }
                // Send another command, since we still need this data response
                SendCommand("data");
                return HandleDataResponse();
            }

            // Data structure taken from documentation
            // "Demod"
            // I believe this is number of sensors
            uint cnt = clientController.ReadUInt32();
            // I believe this is the sweep number
            uint sweepNr = clientController.ReadUInt32();
            // I don't know what these represent
            float[] dataS = new float[SystemNumberOfChannels * SystemNumberOfTX];
            float[] dataC = new float[SystemNumberOfChannels * SystemNumberOfTX];
            for (int i = 0; i < SystemNumberOfChannels * SystemNumberOfTX; i++)
                dataS[i] = clientController.ReadSingle();
            for (int i = 0; i < SystemNumberOfChannels * SystemNumberOfTX; i++)
                dataC[i] = clientController.ReadSingle();
            // Taxonomic Distance, used for head correction
            float TaxDist = clientController.ReadSingle();
            // Bit pattern where each bit represents a sensor's amp status (whether its amplitude != 0)
            uint ampOK = clientController.ReadUInt32();
            // Bit pattern where each bit represents a sensor's pos status (whether its position can be calculated)
            uint posOK = clientController.ReadUInt32();
            // Bit pattern where each bit represents whether a sensor is used as a reference for head correction
            uint SensIsRef = clientController.ReadUInt32();
            // Reserved for future use?
            uint[] other = new uint[6];
            for (int i = 0; i < 6; i++)
                other[i] = clientController.ReadUInt32();

            // "pos"
            float[] pos = new float[SystemNumberOfChannels * 7];
            for (int i = 0; i < SystemNumberOfChannels * 7; i++)
                pos[i] = clientController.ReadSingle();

            // "head"
            float[] head = new float[SystemNumberOfChannels * 7];
            for (int i = 0; i < SystemNumberOfChannels * 7; i++)
                head[i] = clientController.ReadSingle();

            // TODO figure out how to construct dataframe from read information
            return default;
        }

        private bool Readable(int timeout) {
            // Wait until response is ready, or 100 ms passes (at which point we'll consider the request dropped)
            int milliseconds = 0;
            while (!clientController.stream.DataAvailable) {
                Thread.Sleep(1);
                milliseconds++;
                if (milliseconds >= timeout) {
                    return false;
                }
            }
            return true;
        }

        private void SendCommand(string message) {
            // Convert message into bytes
            byte[] command = System.Text.Encoding.ASCII.GetBytes(message + "\n");

            // Send our command, composed of a single line of text
            clientController.stream.Write(command, 0, command.Length);
        }
    }
}
