using Optispeech.Documentation;
using Optispeech.Advanced;
using Optispeech.Sensors;
using Optispeech.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Optispeech.Data {

    /// <summary>
    /// A abstract source for data frames
    /// </summary>
    public abstract class DataSourceReader : MonoBehaviour {

        /// <summary>
        /// The different statuses a data source reader may be in
        /// </summary>
        public enum DataSourceReaderStatus {
            AVAILABLE,
            UNAVAILABLE,
            UNKNOWN
        };

        /// <summary>
        /// Setup a stopwatch to take very precise timestamp measurements, based on
        /// <see href="https://www.codeproject.com/Articles/61964/Performance-Tests-Precise-Run-Time-Measurements-wi">these recommendations</see>
        /// </summary>
        /// <returns>A setup stopwatch</returns>
        public static Stopwatch SetupStopwatch() {
            Stopwatch stopwatch = new Stopwatch();
            // Uses the second Core or Processor for the Test
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
            // Prevents "Normal" processes from interrupting Threads
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            // Prevents "Normal" Threads from interrupting this thread
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            return stopwatch;
        }

        /// <summary>
        /// This event can be used to subscribe to status changes.
        /// If determining the status can take awhile, its recommended to
        /// send <see cref="DataSourceReaderStatus.UNKNOWN"/> in GetCurrentStatus, use another thread to determine
        /// the actual status, and call this delegate once the actual status is known
        /// </summary>
        public UnityEvent<DataSourceReaderStatus> statusChangeEvent = new UnityEvent<DataSourceReaderStatus>();

        /// <summary>
        /// This FIFO queue stores data frames in a thread-safe
        /// way so that this class can add new frames, and
        /// the other parts of the program can read/consume them
        /// </summary>
        public ConcurrentQueue<DataFrame> dataQueue = new ConcurrentQueue<DataFrame>();

        /// <summary>
        /// This variable tracks the last frame added to the queue
        /// It will be used by the avatar, which only ever cares
        /// about the most recent sensor states
        /// </summary>
        public DataFrame lastFrame;

        /// <summary>
        /// This flag gets read by the new thread to tell it when to stop
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// This stopwatch is used to generate precise timestamps if they
        /// aren't provided by the data source itself
        /// </summary>
        private Stopwatch stopwatch;

        /// <summary>
        /// This function gets called automatically whenever a gameobject
        /// with this monobehaviour is added to the scene (as well as whenever
        /// the code gets re-compiled automatically while in play mode).
        /// </summary>
        protected void OnEnable() {
            if (!IsTimestampProvided()) stopwatch = SetupStopwatch();
            StartThread();
        }

        /// <summary>
        /// Implementations should return AVAILABLE if this source can currently be used
        /// Can be determined by e.g. searching if a type of process is currently
        /// running or detect the hardware directly. In that case this function should
        /// return UNKOWN and use statusChangeEvent later
        /// </summary>
        /// <returns>The current availability status of the data source</returns>
        public abstract DataSourceReaderStatus GetCurrentStatus();

        /// <summary>
        /// Implementations should probably just return a constant without
        /// needing to perform any calculations. The source should have
        /// an inherent value for this - either the data it reads comes
        /// with a timestamp or it doesn't. This function returning different
        /// values at different times may lead to undefined behavior.
        /// If this returns false, a timestamp will be calculated
        /// immediately following a data frame being received
        /// </summary>
        /// <returns>Whether or not the data frames produced by this data source have a valid timestamp value</returns>
        protected virtual bool IsTimestampProvided() { return true; }

        /// <summary>
        /// Implementations should return true unless they want to handle
        /// targets themselves, e.g. FileDataSource. This'll prevent
        /// us from saving/loading targets from our profile and will
        /// make all fields readonly in the Targets panel
        /// </summary>
        /// <returns>Whether or not targets can be changed by the user</returns>
        public virtual bool AreTargetsConfigurable() { return true; }

        /// <summary>
        /// Implementations should return true unless they want to handle
        /// sensors themselves, e.g. FileDataSource. This'll prevent
        /// us from saving/loading sensors from our profile and will
        /// make all fields readonly in the Sensors panel
        /// </summary>
        /// <returns>Whether or not sensors can be changed by the user</returns>
        public virtual bool AreSensorsConfigurable() { return true; }

        /// <summary>
        /// If the sensors are configurable, then this method should be
        /// overidden to provide a sensible default list of sensor configs
        /// </summary>
        /// <returns>A set of sensible default sensor configurations for this data source</returns>
        public virtual SensorConfiguration[] GetDefaultSensorConfigurations() {
            return new SensorConfiguration[0];
        }

        /// <summary>
        /// Implementations should have most of their logic in this function
        /// Since this will be run in a separate thread from the one Unity
        /// is using, implementations can freely wait until the next
        /// data frame is available
        /// </summary>
        /// <remarks>
        /// Implementations do NOT need to fill in any data for
        /// <see cref="DataFrame.transformedData"/> or <see cref="DataFrame.targetPositions"/>;
        /// that will be calculated automatically. <see cref="DataFrame.timestamp"/> is only
        /// required if <see cref="IsTimestampProvided"/> returns `false`.
        /// </remarks>
        /// <returns>The next data frame</returns>
        protected abstract DataFrame ReadFrame();

        /// <summary>
        /// Starts the thread that will read data frames in the background.
        /// This method is virtual so implementations can add their own
        /// code that needs to run when the thread is started,
        /// such as initializing a connection to the data source. If that is done,
        /// the implementation should still call <code>base.StartThread()</code>
        /// </summary>
        protected virtual void StartThread() {
            isActive = true;
            Thread readingThread = new Thread(new ThreadStart(Run));
            readingThread.IsBackground = true;
            readingThread.Start();
        }

        /// <summary>
        /// Optional method that implementations can override to cleanup
        /// anything whenever a data source is un-selected
        /// </summary>
        protected virtual void Cleanup() { }

        /// <summary>
        /// If supported, send message to data source reader to start a sweep
        /// </summary>
        /// <param name="folderPath">The folder path to store any sweep data in</param>
        /// <param name="sweepName">The name of this sweep</param>
        public virtual void StartSweep(string folderPath, string sweepName) { }

        /// <summary>
        /// If supported, send message to data source reader to stop a sweep
        /// </summary>
        public virtual void StopSweep() { }

        /// <summary>
        /// This is the function that gets run in the new thread
        /// It detects if data should still be read, processes the next
        /// data frame, and repeats
        /// </summary>
        private void Run() {
            if (!IsTimestampProvided()) {
                stopwatch.Reset();
                stopwatch.Start();
            }
            while (isActive) {
                DataFrame frame = ReadFrame();
                // Set timestamp
                if (!IsTimestampProvided()) {
                    frame.timestamp = stopwatch.ElapsedMilliseconds;
                }
                // Transform data so it can map onto the avatar
                frame.transformedData = DataFrame.GetTransformedData(frame.sensorData);
                // Add our target positions for this frame
                frame.targetPositions = new Dictionary<string, Vector3>();
                foreach (KeyValuePair<string, TargetController> kvp in TargetsManager.Instance.targets) {
                    frame.targetPositions.Add(kvp.Key, kvp.Value.GetTargetPosition(frame.timestamp));
                }
                // Notify subscribers about our new frame
                DataSourceManager.Instance.onFrameRead.Invoke(frame);
                // Add data frame to our queue,
                //  and mark it as our most up to date frame
                dataQueue.Enqueue(lastFrame = frame);
            }
            Cleanup();
        }

        // This gets called when the object is destroyed as well,
        // and will tell the background thread to stop reading frames
        // and cleanup
        [HideInDocumentation]
        private void OnDisable() {
            isActive = false;
        }

        [HideInDocumentation]
        private void OnApplicationQuit() {
            isActive = false;
        }
    }
}
