using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using Optispeech.Documentation;

namespace Optispeech.Data {

    /// <summary>
    /// Multiple data source readers may need to setup a TCP connection with very similar settings, so this class
    /// can be used as a utility to set them all up appropriately
    /// </summary>
    public class TcpClientController {

        /// <summary>
        /// The internal TCP client from .NET this utility class is a wrapper for
        /// </summary>
        public TcpClient client = new TcpClient();
        /// <summary>
        /// The network stream created by the TCP client through which this program can communicate with the data source
        /// </summary>
        public NetworkStream stream = null;
        /// <summary>
        /// A callback event that fires whenever the connection succeeds
        /// </summary>
        public UnityEvent onSuccess = new UnityEvent();
        /// <summary>
        /// A callback event that fires whenever the connection fails
        /// </summary>
        public UnityEvent onFail = new UnityEvent();
        /// <summary>
        /// Some data sources may use the opposite endianness as this program expects, so this flag will determine
        /// whether or not all incoming numbers should be flipped before being parsed
        /// </summary>
        public bool flipEndian = false;

        /// <summary>
        /// An asynchronous task to connect to the TCP host
        /// </summary>
        private Task connectTask = null;
        /// <summary>
        /// A timeout token used to cancel <see cref="connectTask"/> if the connection takes too long.
        /// (This is most likely to occur if the host the connection is trying to reach is not available)
        /// </summary>
        private CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Attempts to asynchronously connect to the TCP host at the specified location,
        /// and will call either <see cref="onSuccess"/> or <see cref="onFail"/> as appropriate
        /// </summary>
        /// <param name="port">The port to connect to</param>
        /// <param name="connectTimeout">How long, in ms, to wait before assuming the host is not available</param>
        /// <param name="host">The host to connect to, defaulting to the localhost</param>
        // Calling an async function from Unity's main thread will return after
        // hitting the first "await" call, but keep running in the background
        public async void Connect(int port=30303, int connectTimeout = 1000, string host = "10.127.40.116") {
            // Yield once before starting to ensure whatever started our task can finish its thing without us calling
            // the callbacks first
            await Task.Yield();
            // Very important! TCP will by default use Nagle's algorithm to delay packets until the previous one
            // has been acknowledged, or the current packet becomes "full". We want to disable that and send
            // the packets constantly
            client.NoDelay = true;
            // Start a task attempting to create a TCP connection
            connectTask = client.ConnectAsync(host, port);
            // The try-catch statement will handle our task being cancelled
            // (like it would if our cancellation token source gets cancelled by a source list refresh)
            try {
                // Check if it completes successfully within our timeout time
                if (await Task.WhenAny(connectTask, Task.Delay(connectTimeout, timeoutCancellationTokenSource.Token)) == connectTask) {
                    if (connectTask.IsFaulted) {
                        Debug.Log($"TCP connection faulted on {host}:{port}: {connectTask.Exception}");
                        onFail.Invoke();
                    } else {
                        stream = client.GetStream();
                        onSuccess.Invoke();
                    }
                } else {
                    // Took too long to connect
                    client.Close();
                    Debug.Log($"TCP connection timed out after {connectTimeout}ms on {host}:{port}");
                    onFail.Invoke();
                }
            } catch (OperationCanceledException e) {
                Debug.LogError($"Error occured during TCP connection on {host}:{port}: {e}");
                // Something used our token to cancel our task prematurely
                client.Close();
                onFail.Invoke();
            } finally {
                timeoutCancellationTokenSource.Dispose();
                timeoutCancellationTokenSource = null;
            }
        }

        [HideInDocumentation]
        public void Dispose() {
            if (timeoutCancellationTokenSource != null) {
                // Cancel our token so that, if we were still connecting, it will immediately stop
                timeoutCancellationTokenSource.Cancel();
            } else {
                client.Close();
            }
        }

        /// <summary>
        /// Reads the next 32-bit int from the network stream
        /// </summary>
        /// <returns>The parsed value</returns>
        public Int32 ReadInt32() {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            if (flipEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Reads the next 32-bit unsigned int from the network stream
        /// </summary>
        /// <returns>The parsed value</returns>
        public uint ReadUInt32() {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Reads the next 64-bit int from the network stream
        /// </summary>
        /// <returns>The parsed value</returns>
        public long ReadInt64() {
            byte[] bytes = new byte[8];
            stream.Read(bytes, 0, 8);
            if (flipEndian) Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        /// Reads the next 32-bit float ("single") from the network stream
        /// </summary>
        /// <returns>The parsed value</returns>
        // Single is another term for a 32-bit float ("Single precision")
        public float ReadSingle() {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            if (flipEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
