using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Buttplug;

namespace VibeSaber
{
    /// <summary>
    /// Manages the connection to the Buttplug server.
    /// </summary>
    public class ButtplugClientManager
    {
        private enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED,
            DISCONNECTING
        }

        private enum TaskType
        {
            CONNECT,
            DISCONNECT,
            SEND_COMMAND,
            CLEAR_ALL
        }

        private ButtplugClient? client = null;

        private State state = State.DISCONNECTED;

        private readonly object buttlock = new object();

        private Queue<KeyValuePair<TaskType, Func<Task>>> taskQueue = new Queue<KeyValuePair<TaskType, Func<Task>>>();

        private Task? currentTask;

        private Func<Task> BuildConnectTask(Uri uri)
        {
            return async () =>
            {
                ButtplugClient newClient;
                lock (this.buttlock)
                {
                    if (this.client != null) throw new TaskCanceledException("An existing client already exists.");
                    if (this.state != State.DISCONNECTED) throw new TaskCanceledException("An existing client is already connected.");
                    this.client = newClient = new ButtplugClient(Meta.Product);
                    this.state = State.CONNECTING;
                }
                try
                {
                    Plugin.Instance?.Log.Info($"Connecting to Buttplug server '{uri}'.");
                    await newClient.ConnectAsync(new ButtplugWebsocketConnectorOptions(uri)).ConfigureAwait(false);
                    Plugin.Instance?.Log.Info($"Connected to Buttplug server.");
                    Plugin.Instance?.Log.Info($"Starting scan for devices.");
                    await newClient.StartScanningAsync().ConfigureAwait(false);
                    Plugin.Instance?.Log.Info($"Scan started.");
                }
                catch (Exception e)
                {
                    try
                    {
                        Plugin.Instance?.Log.Info($"Error connecting to Buttplug server '{uri}'.");
                        Plugin.Instance?.Log.Info($"newClient: '{newClient}'.");
                        Plugin.Instance?.Log.Info($"newClient.Connected: '{newClient.Connected}'.");
                        Plugin.Instance?.Log.Info($"newClient.Name: '{newClient.Name}'.");
                        var field = typeof(ButtplugClient).GetField("_messageSorter", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field == null)
                        {
                            Plugin.Instance?.Log.Info($"field: '{null}'.");
                            foreach (var feild in typeof(ButtplugClient).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                Plugin.Instance?.Log.Info(feild.Name);
                            }
                        }
                        else
                        {
                            var fieldValue = field.GetValue(newClient);
                            Plugin.Instance?.Log.Info($"fieldValue: '{fieldValue}'.");
                            Plugin.Instance?.Log.Info($"fieldValue not null: '{fieldValue != null}'.");
                        }
                        Plugin.Instance?.Log.Error(e);
                    }
                    catch (Exception e2)
                    {
                        Plugin.Instance?.Log.Error(e2);
                    }
                    lock (this.buttlock)
                    {
                        taskQueue.Clear();
                        this.client = null;
                        this.state = State.DISCONNECTED;
                    }
                    throw;
                }
                lock (this.buttlock)
                {
                    if (this.state != State.CONNECTING) throw new TaskCanceledException("Invalid connection state.");
                    this.state = State.CONNECTED;
                }
            };
        }

        private async Task DisconnectTask()
        {
            ButtplugClient oldClient;
            lock (this.buttlock)
            {
                if (this.client == null) throw new TaskCanceledException("No client to disconnect.");
                if (this.state != State.CONNECTED) throw new TaskCanceledException("No client connected.");
                oldClient = this.client;
                this.state = State.DISCONNECTING;
            }
            Plugin.Instance?.Log.Info($"Stopping devices.");
            await oldClient.StopAllDevicesAsync().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Stopping scans.");
            await oldClient.StopScanningAsync().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Disconnecting from server.");
            await oldClient.DisconnectAsync().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Disconnected.");
            lock (this.buttlock)
            {
                if (this.state != State.DISCONNECTING) throw new TaskCanceledException("Invalid connection state.");
                this.state = State.DISCONNECTED;
                this.client = null;
            }
        }

        public void Connect(Uri uri)
        {
            lock (this.buttlock)
            {
                taskQueue.Clear();
                if (this.state == State.CONNECTED || this.state == State.CONNECTING)
                {
                    taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.DISCONNECT, this.DisconnectTask));
                }
                taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.CONNECT, this.BuildConnectTask(uri)));
            }
            this.UpdateTaskQueue();
        }

        public void Disconnect()
        {
            lock (this.buttlock)
            {
                taskQueue.Clear();
                if (this.state == State.CONNECTED || this.state == State.CONNECTING)
                {
                    taskQueue.Enqueue(new KeyValuePair<TaskType, Func<Task>>(TaskType.DISCONNECT, this.DisconnectTask));
                }
            }
            this.UpdateTaskQueue();
        }

        private void UpdateTaskQueue()
        {
            lock (this.buttlock)
            {
                if (this.currentTask == null && this.taskQueue.Count > 0)
                {
                    var nextTask = this.taskQueue.Dequeue();
                    this.currentTask = nextTask.Value();
                    this.currentTask.ContinueWith((Task x) =>
                    {
                        this.currentTask = null;
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
                            if (this.client == null) throw new TaskCanceledException("No client to send command to.");
                            if (this.state != State.CONNECTED) throw new TaskCanceledException("No client connected.");
                            currentClient = this.client;
                        }

                        Plugin.Instance?.Log.Info($"Sending intensity {value}.");
                        foreach (var device in currentClient.Devices)
                        {
                            await device.SendVibrateCmd(value).ConfigureAwait(false);
                        }
                        Plugin.Instance?.Log.Info($"Sent intensity.");
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
                            if (this.client == null) throw new TaskCanceledException("No client to send command to.");
                            if (this.state != State.CONNECTED) throw new TaskCanceledException("No client connected.");
                            currentClient = this.client;
                        }
                        Plugin.Instance?.Log.Info($"Sending StopAllDevices.");
                        await currentClient.StopAllDevicesAsync().ConfigureAwait(false);
                        Plugin.Instance?.Log.Info($"Sent StopAllDevices.");
                    }));
                }
                this.UpdateTaskQueue();
            }
        }
    }
}
