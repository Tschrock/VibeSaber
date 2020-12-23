using System;
using System.Threading.Tasks;

using UnityEngine;

using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using System.Collections.Generic;
using System.Linq;

namespace VibeSaber
{
    public class ButtplugCoordinator : MonoBehaviour
    {

        public enum State
        {
            DISCONNECTED,
            CONNECTING,
            CONNECTED,
            DISCONNECTING,
        }

        private ButtplugWebsocketConnector? connection;

        private ButtplugClient? client;

        public ButtplugClientDevice[] Devices => client?.Devices ?? Array.Empty<ButtplugClientDevice>();

        public void Connect(string server)
        {
            // If we're already connected, disconnect
            if (client != null) Disconnect();

            // Make a URI
            Uri serverUri;
            try
            {
                serverUri = new Uri(server);
            }
            catch
            {
                Plugin.Instance?.Log.Warn($"Can't connect to Intiface Server. Invalid server URI '{server}'.");
                return;
            }

            // Make a new connection
            connection = new ButtplugWebsocketConnector(serverUri);

            // Make a new client
            client = new ButtplugClient(Meta.Product, connection);
            try
            {
                client.ConnectAsync().Wait();
                Plugin.Instance?.Log.Info($"Connected to Intiface Server '{server}'.");
            }
            catch (ButtplugClientConnectorException ex)
            {
                client = null;
                connection = null;
                Plugin.Instance?.Log.Error($"Can't connect to Intiface Server. {ex.InnerException.Message}");
            }
        }

        public void Disconnect()
        {
            Plugin.Instance?.Log.Info($"Try Disconnect.");
            if (this.client != null)
            {
                var client = this.client;
                this.client = null;
                foreach (var device in client.Devices)
                {
                    Plugin.Instance?.Log.Info($"Stop Device '{device.Name}'.");
                    device.StopDeviceCmd().Wait();
                }
                Plugin.Instance?.Log.Info($"Disconnect.");
                client.DisconnectAsync().Wait();
                client = null;
                connection = null;
            }
        }

        private List<double[]> activePulses = new List<double[]>();
        private double lastIntensity = 0;

        private void Awake()
        {
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
        }

        private void Update()
        {
            Update(Time.deltaTime * 1000);
        }

        private void Update(double deltaMs) {
            activePulses.ForEach(p => p[0] -= deltaMs);
            activePulses = activePulses.Where(p => p[0] > 0).ToList();
            var max = activePulses.Select(p => p[1]).DefaultIfEmpty(0).Max();
            if(max != lastIntensity) {
                lastIntensity = max;
                SetIntensity(max);
            }
        }

        public void Pulse(double intensity, double duration)
        {
            if (client != null)
            {
                activePulses.Add(new[] { duration, intensity });
                Update(0);
            }
        }

        public void SetIntensity(double intensity)
        {
            if (client != null)
            {
                foreach (var device in client.Devices)
                {
                    Plugin.Instance?.Log.Info($"SendVibrateCmd({intensity}) to '{device.Name}'.");
                    device.SendVibrateCmd(intensity).Wait();
                }
            }
        }

        public void StopAll()
        {
            if (client != null)
            {
                foreach (var device in client.Devices)
                {
                    Plugin.Instance?.Log.Info($"Stop Device '{device.Name}'.");
                    device.StopDeviceCmd().Wait();
                }
            }
        }

    }
}
