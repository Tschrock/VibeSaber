using System.Diagnostics;
using System.Threading.Tasks;

using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPALogger = IPA.Logging.Logger;

using VibeSaber.Configuration;

using UnityEngine;

namespace VibeSaber
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin? Instance { get; private set; }

        internal IPALogger Log { get; private set; }

        internal PluginConfig Config { get; private set; }

        internal ButtplugCoordinator? ButtplugCoordinator { get; private set; }

        [Init]
        public Plugin(IPALogger logger, Config conf)
        {
            Plugin.Instance = this;
            this.Log = logger;
            this.Config = conf.Generated<PluginConfig>();
            Log.Info("VibeSaber loaded");
        }

        [OnEnable]
        public void OnPluginEnabled()
        {
            Log.Info("OnPluginEnabled");
            Log.Info("Adding Settings Menu");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu(Meta.Product, "VibeSaber.Configuration.PluginConfig.bsml", this.Config);
            Log.Info("Hooking Game Events");
            BS_Utils.Utilities.BSEvents.menuSceneActive += OnMenuSceneActive;
            BS_Utils.Utilities.BSEvents.gameSceneActive += OnGameSceneActive;
            BS_Utils.Utilities.BSEvents.songPaused += OnSongPaused;
            BS_Utils.Utilities.BSEvents.songUnpaused += OnSongUnpaused;
            BS_Utils.Utilities.BSEvents.levelCleared += OnLevelCleared;
            BS_Utils.Utilities.BSEvents.levelFailed += OnLevelFailed;
            BS_Utils.Utilities.BSEvents.levelQuit += OnLevelQuit;
            BS_Utils.Utilities.BSEvents.levelRestarted += OnLevelRestarted;
            BS_Utils.Utilities.BSEvents.noteWasCut += OnNoteCut;
            BS_Utils.Utilities.BSEvents.noteWasMissed += OnNoteMissed;
            BS_Utils.Utilities.BSEvents.energyDidChange += OnEnergyChanged;
            if (ButtplugCoordinator == null)
            {
                Log.Info("Creating Buttplug Coordinator");
                ButtplugCoordinator = new GameObject("ButtplugCoordinator").AddComponent<ButtplugCoordinator>();
                ButtplugCoordinator.Connect(Config.ServerUrl);
            }
        }

        [OnDisable]
        public async Task OnPluginDisabled()
        {
            Log.Info("OnPluginDisabled Start");
            Log.Info("Removing Settings Menu");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.RemoveSettingsMenu(this.Config);
            Log.Info("Unhooking Game Events");
            BS_Utils.Utilities.BSEvents.menuSceneActive -= OnMenuSceneActive;
            BS_Utils.Utilities.BSEvents.gameSceneActive -= OnGameSceneActive;
            BS_Utils.Utilities.BSEvents.songPaused -= OnSongPaused;
            BS_Utils.Utilities.BSEvents.songUnpaused -= OnSongUnpaused;
            BS_Utils.Utilities.BSEvents.levelCleared -= OnLevelCleared;
            BS_Utils.Utilities.BSEvents.levelFailed -= OnLevelFailed;
            BS_Utils.Utilities.BSEvents.levelQuit -= OnLevelQuit;
            BS_Utils.Utilities.BSEvents.levelRestarted -= OnLevelRestarted;
            BS_Utils.Utilities.BSEvents.noteWasCut -= OnNoteCut;
            BS_Utils.Utilities.BSEvents.noteWasMissed -= OnNoteMissed;
            BS_Utils.Utilities.BSEvents.energyDidChange -= OnEnergyChanged;
            if (ButtplugCoordinator != null)
            {

                // System.Diagnostics.Process.GetCurrentProcess().Kill();
                Log.Info("Disconnecting from Buttplug server");
                await ButtplugCoordinator.Shutdown().ConfigureAwait(false);
                Log.Info("Destroying Buttplug Coordinator");
                GameObject.Destroy(ButtplugCoordinator);
                ButtplugCoordinator = null;
            }
            Log.Info("OnPluginDisabled End");
        }

        private float energyLevel;
        private float songProgress = 0.5F;
        private float CalculateIntensity()
        {
            var min = System.Math.Min(Config.MinimumStrength, Config.MaximumStrength);
            var max = System.Math.Max(Config.MinimumStrength, Config.MaximumStrength);
            var range = max - min;
            switch (Config.StrengthMode)
            {
                case StrengthMode.Battery: return min + (energyLevel * range);
                case StrengthMode.InverseBattery: return min + ((1 - energyLevel) * range);
                case StrengthMode.SongTime: return min + (songProgress * range);
                case StrengthMode.Disabled: return max;
                default: return min;
            }
        }

        private void UpdateIntensity()
        {
            if (Config.PulseMode == PulseMode.Disabled)
            {
                ButtplugCoordinator?.SetIntensity(CalculateIntensity() / 100);
            }
        }

        private void SendPulse()
        {
            if (Config.PulseMode != PulseMode.Disabled)
            {
                ButtplugCoordinator?.Pulse(CalculateIntensity() / 100, Config.PulseLength);
            }
        }

        public void OnMenuSceneActive()
        {
            ButtplugCoordinator?.StopAll();
            Log.Info("OnMenuSceneActive");
        }

        public void OnGameSceneActive()
        {
            Log.Info("OnGameSceneActive");
            energyLevel = 0.5F;
            UpdateIntensity();
        }

        public void OnSongPaused()
        {
            Log.Info("OnSongPaused");
            ButtplugCoordinator?.StopAll();
        }

        public void OnSongUnpaused()
        {
            Log.Info("OnSongUnpaused");
            UpdateIntensity();
        }

        public void OnLevelCleared(StandardLevelScenesTransitionSetupDataSO setupData, LevelCompletionResults results)
        {
            ButtplugCoordinator?.StopAll();
            Log.Info("OnLevelCleared");
        }

        public void OnLevelFailed(StandardLevelScenesTransitionSetupDataSO setupData, LevelCompletionResults results)
        {
            ButtplugCoordinator?.StopAll();
            Log.Info("OnLevelFailed");
        }

        public void OnLevelQuit(StandardLevelScenesTransitionSetupDataSO setupData, LevelCompletionResults results)
        {
            ButtplugCoordinator?.StopAll();
            Log.Info("OnLevelQuit");
        }

        public void OnLevelRestarted(StandardLevelScenesTransitionSetupDataSO setupData, LevelCompletionResults results)
        {
            Log.Info("OnLevelRestarted");
        }

        public void OnNoteCut(NoteData noteData1, NoteCutInfo noteCutInfo, int multiplier)
        {
            Log.Info("OnNoteHit");
            if (Config.PulseMode == PulseMode.NoteHit || Config.PulseMode == PulseMode.EveryNote)
            {
                SendPulse();
            }
        }

        public void OnNoteMissed(NoteData noteDate, int multiplier)
        {
            Log.Info("OnNoteMissed");
            if (Config.PulseMode == PulseMode.NoteMiss || Config.PulseMode == PulseMode.EveryNote)
            {
                SendPulse();
            }
        }

        public void OnEnergyChanged(float energy)
        {
            Log.Info("OnEnergyChanged");
            energyLevel = energy;
            UpdateIntensity();
        }
    }
}
