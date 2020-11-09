namespace VibeSaber.Configuration
{
    [System.AttributeUsage(System.AttributeTargets.All)]
    public class NameAttribute : System.ComponentModel.DisplayNameAttribute {
        public NameAttribute() : base() { }
        public NameAttribute(string displayName) : base(displayName) { }
    }
}
