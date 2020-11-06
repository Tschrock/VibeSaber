using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine.SceneManagement;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace VibeSaber
{

    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal IPALogger Log { get; private set; }
        internal PluginConfig Config { get; private set; }

        [Init]
        public Plugin(IPALogger logger, Config conf)
        {
            Plugin.Instance = this;
            this.Log = logger;
            this.Config = conf.Generated<PluginConfig>();

            Log.Info("VibeSaber initialized.");
            Log.Debug("Config loaded");
        }

        [OnEnable]
        public void OnPluginEnabled()
        {
            Log.Debug("OnPluginEnabled");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu(Meta.Product, "VibeSaber.Configuration.bsml", this.Config);
            // new GameObject( "VibeSaberController" ).AddComponent<VibeSaberController>();
        }

        [OnDisable]
        public void OnPluginDisabled()
        {
            Log.Debug("OnPluginDisabled");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.RemoveSettingsMenu(this.Config);
            // Remove game object
        }
    }
}
