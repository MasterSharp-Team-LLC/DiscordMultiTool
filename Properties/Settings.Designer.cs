// Auto-generated minimal replacement for missing Settings.Designer.cs
// Provides a simple in-memory Settings.Default singleton used by the app.
namespace DiscordMultiTool.Properties
{
    [System.CodeDom.Compiler.GeneratedCode("Custom", "1.0")]
    internal sealed partial class Settings
    {
        private static readonly Settings defaultInstance = new Settings();

        public static Settings Default => defaultInstance;

        // These mirror the fields used in Form1.cs
        public string checkbox { get; set; } = "False";
        public string textbox1 { get; set; } = string.Empty;
        public string textbox2 { get; set; } = string.Empty;
        public string tema { get; set; } = "Classic";

        // Minimal Save implementation (no-op). The original generated class persists
        // settings; for build-time purposes this is sufficient.
        public void Save()
        {
            // Intentionally left blank for this minimal implementation.
        }
    }
}
