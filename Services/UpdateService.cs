using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordMultiTool.Services;

public sealed record UpdateCheckResult(bool Success, bool UpdateAvailable, string CurrentVersion, string? LatestVersion, string? ErrorMessage = null);

public static class UpdateService
{
    public const string LatestReleaseUrl = "https://github.com/MasterSharp-Team-LLC/DiscordMultiTool/releases/latest";

    public static Version CurrentVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : new Version(3, 6, 0);
        }
    }

    public static string CurrentVersionString => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            using var response = await HttpClientProvider.Shared.GetAsync(LatestReleaseUrl);
            var finalUrl = response.RequestMessage?.RequestUri?.ToString();
            if (string.IsNullOrEmpty(finalUrl))
                return new UpdateCheckResult(false, false, CurrentVersionString, null, "No redirect URL");

            string tag = finalUrl.Split('/').Last();
            string? versionStr = null;
            if (tag.StartsWith("DMT-", StringComparison.OrdinalIgnoreCase))
            {
                versionStr = tag[4..].TrimStart('v', 'V');
            }
            else if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionStr = tag[1..];
            }

            if (versionStr != null && Version.TryParse(versionStr, out var remoteVersion))
            {
                bool hasUpdate = remoteVersion > CurrentVersion;
                return new UpdateCheckResult(true, hasUpdate, CurrentVersionString, versionStr);
            }

            return new UpdateCheckResult(true, false, CurrentVersionString, null);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, false, CurrentVersionString, null, ex.Message);
        }
    }

    public static void OpenReleasesPage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = LatestReleaseUrl,
            UseShellExecute = true
        });
    }
}
