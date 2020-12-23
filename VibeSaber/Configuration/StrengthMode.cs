using System.ComponentModel;

namespace VibeSaber.Configuration
{
    internal enum StrengthMode
    {
        [Name("Disabled (Max)")]
        [Description("Disables variable strength and always uses the max.")]
        Disabled,

        [Name("Battery")]
        [Description("Strength is proportional to battery level.")]
        Battery,

        [Name("Inverse Battery")]
        [Description("Strength is inversely proportional to battery level.")]
        InverseBattery,

        [Name("Song Time")]
        [Description("Strength increases as a song progresses.")]
        SongTime
    }
}
