using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using BeatSaberMarkupLanguage.Attributes;

[assembly: InternalsVisibleTo( GeneratedStore.AssemblyVisibilityTarget )]
namespace VibeSaber
{
    public struct DefaultSettings
    {
        public const bool IsEnabled = true;
        public const int MaximumStrength = 100;
        public const int MinimumStrength = 0;
    }

    internal class Configuration
    {
        internal static Configuration Instance { get; set; }

        [UIValue( "IsEnabled" )]
        public virtual bool IsEnabled { get; set; } = DefaultSettings.IsEnabled;

        [UIValue( "MaximumStrength" )]
        public virtual int MaximumStrength { get; set; } = DefaultSettings.MaximumStrength;

        [UIValue( "MinimumStrength" )]
        public virtual int MinimumStrength { get; set; } = DefaultSettings.MinimumStrength;
    }
}