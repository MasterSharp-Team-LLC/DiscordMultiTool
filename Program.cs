using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordMultiTool;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Task.Run(InitializeAsync);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRITICAL ERROR: " + ex.ToString());
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static async Task InitializeAsync()
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dmt");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string guiPath = Path.Combine(folder, "GUI.png");
            string overlayPath = Path.Combine(folder, "Overlay.py");

            string guiUrl = "https://github.com/CodeSharp3210/DiscordMultiTool/releases/download/DMT-2.5.6/GUI.png";
            string overlayUrl = "https://github.com/CodeSharp3210/DiscordMultiTool/releases/download/DMT-2.5.6/Overlay.py";

            using var client = new HttpClient();
            
            if (!File.Exists(guiPath))
            {
                try
                {
                    var data = await client.GetByteArrayAsync(guiUrl);
                    await File.WriteAllBytesAsync(guiPath, data);
                }
                catch { }
            }

            if (!File.Exists(overlayPath))
            {
                try
                {
                    var data = await client.GetByteArrayAsync(overlayUrl);
                    await File.WriteAllBytesAsync(overlayPath, data);
                }
                catch { }
            }
        }
        catch { }
    }
}
