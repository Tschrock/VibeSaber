﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using IPA.Config.Stores;
using BeatSaberMarkupLanguage.Attributes;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace VibeSaber.Configuration
{
    internal struct DefaultSettings
    {
        public const bool IsEnabled = true;
        public const bool IsPreviewEnabled = false;
        public const PulseMode PulseMode = Configuration.PulseMode.NoteMiss;
        public const int PulseLength = 300;
        public const StrengthMode StrengthMode = Configuration.StrengthMode.Battery;
        public const int MaximumStrength = 100;
        public const int MinimumStrength = 0;
    }

    internal class PluginConfig
    {
        [UIValue(nameof(IsEnabled))]
        public virtual bool IsEnabled { get; set; } = DefaultSettings.IsEnabled;

        [UIValue(nameof(IsPreviewEnabled))]
        public virtual bool IsPreviewEnabled { get; set; } = DefaultSettings.IsPreviewEnabled;

        [UIValue(nameof(PulseModeOptions))]
        private List<object> PulseModeOptions = Enum.GetValues(typeof(Configuration.PulseMode)).Cast<object>().ToList();

        [UIValue(nameof(PulseMode))]
        public virtual PulseMode PulseMode { get; set; } = DefaultSettings.PulseMode;

        [UIValue(nameof(PulseLength))]
        public virtual int PulseLength { get; set; } = DefaultSettings.PulseLength;

        [UIValue(nameof(StrengthModeOptions))]
        private List<object> StrengthModeOptions = Enum.GetValues(typeof(Configuration.StrengthMode)).Cast<object>().ToList();

        [UIValue(nameof(StrengthMode))]
        public virtual StrengthMode StrengthMode { get; set; } = DefaultSettings.StrengthMode;

        [UIValue(nameof(MaximumStrength))]
        public virtual int MaximumStrength { get; set; } = DefaultSettings.MaximumStrength;

        [UIValue(nameof(MinimumStrength))]
        public virtual int MinimumStrength { get; set; } = DefaultSettings.MinimumStrength;
    }
}
