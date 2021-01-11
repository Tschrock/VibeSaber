using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace VibeSaber
{
    public class ButtplugCoordinator : MonoBehaviour
    {

        /// <summary>
        /// A vibration pulse.
        /// </summary>
        private class VibrationPulse
        {
            public double Intensity { get; set; } = 0;
            public double TimeRemaining { get; set; } = 0;
        }

        private ButtplugClientManager client = new ButtplugClientManager();

        /// <summary>
        /// A list of the active pulses.
        /// </summary>
        /// <returns>A list of VibrationPulses.</returns>
        private List<VibrationPulse> activePulses = new List<VibrationPulse>();

        /// <summary>
        /// The current intensity.
        /// </summary>
        private double intensity = 0;

        /// <summary>
        /// Connects to a buttplug server.
        /// </summary>
        /// <param name="server">The server to connect to.</param>
        /// <returns></returns>
        public void Connect(string server)
        {
            if (Uri.TryCreate(server, UriKind.Absolute, out var serverUri))
            {
                this.client.Connect(serverUri);
            }
            else
            {
                Plugin.Instance?.Log.Info($"Invalid Buttplug server '{server}'.");
            }
        }

        public void Disconnect()
        {
            this.client.Disconnect();
        }

        private void Awake()
        {
            // Don't destroy this object on scene changes
            GameObject.DontDestroyOnLoad(this);
        }

        private void Update()
        {
            Update(Time.deltaTime * 1000);
        }

        private void Update(double deltaMs)
        {
            activePulses.ForEach(p => p.TimeRemaining -= deltaMs);
            activePulses = activePulses.Where(p => p.TimeRemaining > 0).ToList();
            var max = activePulses.Select(p => p.Intensity).DefaultIfEmpty(0).Max();
            if (max != intensity)
            {
                intensity = max;
                this.client.SetIntensity(max);
            }
        }

        public void Pulse(double intensity, double duration)
        {
            if (client != null)
            {
                activePulses.Add(new VibrationPulse { Intensity = intensity, TimeRemaining = duration });
                this.Update(0);
            }
        }

        public void SetIntensity(double intensity)
        {
            this.client.SetIntensity(intensity);
        }

        public void StopAll()
        {
            this.client.StopAll();
        }

        public Task Shutdown() {
            return this.client.Shutdown();
        }
    }
}
