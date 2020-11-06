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

    [Plugin( RuntimeOptions.SingleStartInit )]
    public class Plugin
    {
        public static string PluginName = "VibeSaber";
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init( IPALogger logger )
        {
            Instance = this;
            Log = logger;
            Log.Info( "VibeSaber initialized." );
        }

        #region BSIPA Config
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.Instance = conf.Generated<Configuration>();
            Log.Debug("Config loaded");
            BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu( PluginName, "VibeSaber.Configuration.bsml", Configuration.Instance );
        }
        #endregion

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug( "OnApplicationStart" );
            new GameObject( "VibeSaberController" ).AddComponent<VibeSaberController>();

        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug( "OnApplicationQuit" );

        }
    }
}
