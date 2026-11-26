using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace DiscordMultiTool;

public static class Secrets
{
    private static string? _googleClientId;
    private static string? _googleClientSecret;
    private static string? _firebaseApiKey;
    private static bool _initialized;

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        // 1. Embedded secrets.json (bundled in official Release builds when secrets.json exists at build time)
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream("DiscordMultiTool.secrets.json");
            if (stream != null)
            {
                LoadFromStream(stream);
            }
        }
        catch { }

        // 2. External secrets.json next to executable or working directory (overrides embedded)
        try
        {
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "secrets.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Environment.CurrentDirectory, "secrets.json");
            }

            if (File.Exists(jsonPath))
            {
                using FileStream fs = File.OpenRead(jsonPath);
                LoadFromStream(fs);
            }
        }
        catch { }

        // 3. Environment variable overrides
        _googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? _googleClientId;
        _googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? _googleClientSecret;
        _firebaseApiKey = Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ?? _firebaseApiKey;
    }

    private static void LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        if (root.TryGetProperty("GoogleClientId", out var cid) && !string.IsNullOrWhiteSpace(cid.GetString()))
            _googleClientId = cid.GetString();
        if (root.TryGetProperty("GoogleClientSecret", out var csec) && !string.IsNullOrWhiteSpace(csec.GetString()))
            _googleClientSecret = csec.GetString();
        if (root.TryGetProperty("FirebaseApiKey", out var fkey) && !string.IsNullOrWhiteSpace(fkey.GetString()))
            _firebaseApiKey = fkey.GetString();
    }

    public static string GoogleClientId
    {
        get
        {
            EnsureInitialized();
            return _googleClientId ?? "YOUR_GOOGLE_CLIENT_ID";
        }
    }

    public static string GoogleClientSecret
    {
        get
        {
            EnsureInitialized();
            return _googleClientSecret ?? "YOUR_GOOGLE_CLIENT_SECRET";
        }
    }

    public static string FirebaseApiKey
    {
        get
        {
            EnsureInitialized();
            return _firebaseApiKey ?? "YOUR_FIREBASE_API_KEY";
        }
    }
}
