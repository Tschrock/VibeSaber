using System;
using System.Threading.Tasks;

using UnityEngine;

using Buttplug;
using System.Collections.Generic;
using System.Linq;

namespace VibeSaber
{
    public class ButtplugCoordinator : MonoBehaviour
    {
        private List<double[]> activePulses = new List<double[]>();
        private double intensity = 0;

        public Uri? ServerUri { get; private set; }

        private ButtplugClient client = new ButtplugClient(Meta.Product);

        public ButtplugClientDevice[] Devices => client?.Devices ?? Array.Empty<ButtplugClientDevice>();

        public ButtplugCoordinator() {
            Plugin.Instance?.Log.Info($"ButtplugCoordinator Construct");
        }
        ~ButtplugCoordinator() {
            Plugin.Instance?.Log.Info($"ButtplugCoordinator Deconstruct");
        }

        public async Task Connect(string server)
        {
            try
            {
                ServerUri = new Uri(server);
            }
            catch
            {
                Plugin.Instance?.Log.Warn($"Can't connect to Intiface Server. Invalid server URI '{server}'.");
                return;
            }

            try
            {
                Plugin.Instance?.Log.Info($"Connecting to Intiface Server '{server}'...");
                await client.ConnectAsync(new ButtplugWebsocketConnectorOptions(ServerUri)).ConfigureAwait(false);
                Plugin.Instance?.Log.Info($"Connected, scanning.");
                await client.StartScanningAsync().ConfigureAwait(false);
                Plugin.Instance?.Log.Info($"Connected to Intiface Server '{server}'.");
            }
            catch (ButtplugException ex)
            {
                Plugin.Instance?.Log.Error($"Can't connect to Intiface Server. {ex.InnerException.Message}");
            }
        }

        public async Task Disconnect()
        {
            Plugin.Instance?.Log.Info($"Stopping scanning.");
            await client.StopScanningAsync().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Preparing to disconnect from server '{ServerUri}'.");
            await this.StopAll().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Disconnecting.");
            await client.DisconnectAsync().ConfigureAwait(false);
            Plugin.Instance?.Log.Info($"Successfully disconnected from server.");
        }

        private void Awake()
        {
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
        }

        private async Task Update()
        {
            await Update(Time.deltaTime * 1000).ConfigureAwait(false);
        }

        private async Task Update(double deltaMs)
        {
            activePulses.ForEach(p => p[0] -= deltaMs);
            activePulses = activePulses.Where(p => p[0] > 0).ToList();
            var max = activePulses.Select(p => p[1]).DefaultIfEmpty(0).Max();
            if (max != intensity)
            {
                intensity = max;
                await SetIntensity(max).ConfigureAwait(false);
            }
        }

        public async Task Pulse(double intensity, double duration)
        {
            if (client != null)
            {
                activePulses.Add(new[] { duration, intensity });
                await Update(0).ConfigureAwait(false);
            }
        }

        public async Task SetIntensity(double intensity)
        {
            if (client != null)
            {
                foreach (var device in client.Devices)
                {
                    Plugin.Instance?.Log.Info($"SendVibrateCmd({intensity}) to '{device.Name}'.");
                    await device.SendVibrateCmd(intensity).ConfigureAwait(false);
                }
            }
        }

        public async Task StopAll()
        {
            Plugin.Instance?.Log.Info($"Clearing pulses.");
            this.activePulses.Clear();
            Plugin.Instance?.Log.Info($"Telling all devices to stop.");
            await client.StopAllDevicesAsync().ConfigureAwait(false);
        }
    }
}
