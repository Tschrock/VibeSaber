using System.Reflection;

namespace VibeSaber
{
    internal static class Meta
    {
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static string Company = Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";
        internal static string Product = Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Unknown";
    }
}
