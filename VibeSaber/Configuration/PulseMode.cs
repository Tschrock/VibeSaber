using System.ComponentModel;

namespace VibeSaber.Configuration
{
    internal enum PulseMode
    {
        [Name("Disabled")]
        [Description("Disable Pulses.")]
        Disabled,

        [Name("Note Missed")]
        [Description("Pulse on each missed note.")]
        NoteMiss,

        [Name("Note Hit")]
        [Description("Pulse on each hit note.")]
        NoteHit,

        [Name("Every Note")]
        [Description("Pulse on every note.")]
        EveryNote
    }
}
