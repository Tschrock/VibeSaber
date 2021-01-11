using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Buttplug;

namespace VibeSaber
{
    /// <summary>
    /// Manages the connection to the Buttplug server and ensures that all async requests are executed sequentially and in the correct order.
    /// </summary>
    public class ButtplugClientManager
    {
        /// <summary>
        /// Represents the connection state of a client.
        /// </summary>
        private enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED,
            DISCONNECTING
        }

        /// <summary>
        /// Represents the type of client task.
        /// </summary>
        private enum TaskType
        {
            CONNECT,
            DISCONNECT,
            SEND_COMMAND,
            CLEAR_ALL
        }

        /// <summary>
        /// The currently active client.
        /// </summary>
        private ButtplugClient? activeClient = null;

        private State state = State.DISCONNECTED;

        private bool shutdown = false;

        private readonly object buttlock = new object();

        private Queue<KeyValuePair<TaskType, Func<Task>>> taskQueue = new Queue<KeyValuePair<TaskType, Func<Task>>>();

        private Task? currentTask;

        /// <summary>
        /// Creates a new task that connects to the given Uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private Func<Task> BuildConnectTask(Uri uri)
        {
            return async () =>
            {
                ButtplugClient newClient;
                lock (this.buttlock)
                {
                    // Make sure we don't already have a client
                    if (this.activeClient != null) throw new TaskCanceledException("An existing client already exists.");
                    // Make sure we're disconnected
                    if (this.state != State.DISCONNECTED) throw new TaskCanceledException("An existing client is already connected.");
                    // Create a new client
                    newClient = new ButtplugClient(Meta.Product);
                    // Store a reference to the new client. It's important to do this *before* any async calls so it doesn't get garbage collected
                    // https://stackoverflow.com/questions/26957311/why-does-gc-collects-my-object-when-i-have-a-reference-to-it
                    this.activeClient = newClient;
                    // Update the state to Connecting
                    this.state = State.CONNECTING;
                }
                try
                {
                    // Connect to the server
                    Plugin.Instance?.Log.Info($"Connecting to Buttplug server '{uri}'.");
                    await this.activeClient.ConnectAsync(new ButtplugWebsocketConnectorOptions(uri)).ConfigureAwait(false);
                    Plugin.Instance?.Log.Info($"Connected to Buttplug server.");

                    // Start scanning for connected devices
                    Plugin.Instance?.Log.Info($"Starting scan for devices.");
                    await this.activeClient.StartScanningAsync().ConfigureAwait(false);
                    Plugin.Instance?.Log.Info($"Scan started.");

                    // Update the state
                    lock (this.buttlock)
                    {
                        // Make sure we were connecting (just in case)
                        if (this.state != State.CONNECTING) throw new TaskCanceledException("Invalid connection state.");
                        // Update the state to Connected
                        this.state = State.CONNECTED;
                    }
                }
                catch (Exception e)
                {
                    // Log the error
                    Plugin.Instance?.Log.Error(e);

                    // Clean up
                    lock (this.buttlock)
                    {
                        // Clear the task queue because they're probably invalid now.
                        taskQueue.Clear();
                        // Clear the current client
                        this.activeClient = null;
                        // Update the state
                        this.state = State.DISCONNECTED;
                    }
                    throw;
                }
            };
        }

        /// <summary>
        /// A task that disconnects the currently connected client.
        /// </summary>
        /// <returns></returns>
        private async Task DisconnectTask()
        {
            ButtplugClient oldClient;
            lock (this.buttlock)
            {
                // Make sure we have an active client
                if (this.activeClient == null) throw new TaskCanceledException("No client to disconnect.");
                // Make sure we're connected
                if (this.state != State.CONNECTED) throw new TaskCanceledException("No client connected.");
                // Get the old client
                oldClient = this.activeClient;
                // Update the state
                this.state = State.DISCONNECTING;
            }
            // Stop all connected devices
            Plugin.Instance?.Log.Info($"Stopping devices.");
            await oldClient.StopAllDevicesAsync().ConfigureAwait(false);
            // Stop device scanning
            Plugin.Instance?.Log.Info($"Stopping scans.");
            await oldClient.StopScanningAsync().ConfigureAwait(false);
            // Disconnect from the server
            Plugin.Instance?.Log.Info($"Disconnecting from server.");
            await oldClient.DisconnectAsync().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Disconnected.");
            lock (this.buttlock)
            {
                // Make sure we're disconnecting
                if (this.state != State.DISCONNECTING) throw new TaskCanceledException("Invalid connection state.");
                // Update the state
                this.state = State.DISCONNECTED;
                // Clear the old client - It's important to do this *after* everything is done so it doesn't get prematurely garbage collected
                // https://stackoverflow.com/questions/26957311/why-does-gc-collects-my-object-when-i-have-a-reference-to-it
                this.activeClient = null;
            }
        }

        /// <summary>
        /// Starts a connection to the given Uri. Any existing connections will be disconnected.
        /// </summary>
        /// <param name="uri"></param>
        public void Connect(Uri uri)
        {
            lock (this.buttlock)
            {
                if (this.shutdown) return;
                taskQueue.Clear();
                if (this.state == State.CONNECTED || this.state == State.CONNECTING)
                {
                    taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.DISCONNECT, this.DisconnectTask));
                }
                taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.CONNECT, this.BuildConnectTask(uri)));
            }
            this.UpdateTaskQueue();
        }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        public void Disconnect()
        {
            lock (this.buttlock)
            {
                if (this.shutdown) return;
                taskQueue.Clear();
                if (this.state == State.CONNECTED || this.state == State.CONNECTING)
                {
                    taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.DISCONNECT, this.DisconnectTask));
                }
            }
            this.UpdateTaskQueue();
        }

        /// <summary>
        /// Updates the task queue.
        /// </summary>
        private void UpdateTaskQueue()
        {
            lock (this.buttlock)
            {
                // If there's no current task and there's tasks in the queue
                if (!this.shutdown && this.currentTask == null && this.taskQueue.Count > 0)
                {
                    // Get the next task
                    var nextTask = this.taskQueue.Dequeue();
                    // Run the task and save it
                    this.currentTask = nextTask.Value();
                    // Set up a continuation to re-check the task queue
                    this.currentTask.ContinueWith((Task x) =>
                    {
                        // Remove the old task
                        this.currentTask = null;
                        // Re-check the task queue
                        UpdateTaskQueue();
                    });
                }
            }
        }

        public void SetIntensity(double value)
        {
            if (this.state == State.CONNECTING || this.state == State.CONNECTED)
            {
                lock (this.buttlock)
                {
                    this.taskQueue = new Queue<KeyValuePair<TaskType, Func<Task>>>(this.taskQueue.Where(t => t.Key != TaskType.SEND_COMMAND));
                    taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.SEND_COMMAND, async () =>
                    {
                        ButtplugClient currentClient;
                        lock (this.buttlock)
                        {
                            if (this.activeClient == null) throw new TaskCanceledException("No client to send command to.");
                            if (this.state != State.CONNECTED) throw new TaskCanceledException("Client is not connected.");
                            currentClient = this.activeClient;
                        }

                        Plugin.Instance?.Log.Trace($"Sending intensity {value}.");
                        foreach (var device in currentClient.Devices)
                        {
                            await device.SendVibrateCmd(value).ConfigureAwait(false);
                        }
                        Plugin.Instance?.Log.Trace($"Sent intensity.");
                    }));
                }
                this.UpdateTaskQueue();
            }
        }

        public void StopAll()
        {
            if (this.state == State.CONNECTING || this.state == State.CONNECTED)
            {
                lock (this.buttlock)
                {
                    this.taskQueue = new Queue<KeyValuePair<TaskType, Func<Task>>>(this.taskQueue.Where(t => t.Key != TaskType.SEND_COMMAND && t.Key != TaskType.CLEAR_ALL));
                    taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.CLEAR_ALL, async () =>
                    {
                        ButtplugClient currentClient;
                        lock (this.buttlock)
                        {
                            if (this.activeClient == null) throw new TaskCanceledException("No client to send command to.");
                            if (this.state != State.CONNECTED) throw new TaskCanceledException("Client is not connected.");
                            currentClient = this.activeClient;
                        }
                        Plugin.Instance?.Log.Info($"Sending StopAllDevices.");
                        await currentClient.StopAllDevicesAsync().ConfigureAwait(false);
                        Plugin.Instance?.Log.Info($"Sent StopAllDevices.");
                    }));
                }
                this.UpdateTaskQueue();
            }
        }

        public async Task Shutdown()
        {
            lock (this.buttlock)
            {
                shutdown = true;
                taskQueue.Clear();
            }
            var task = this.currentTask;
            if (task != null)
            {
                await task.ConfigureAwait(false);
            }
            if (this.state == State.CONNECTED || this.state == State.CONNECTING)
            {
                await this.DisconnectTask().ConfigureAwait(false);
            }

        }
    }
}
