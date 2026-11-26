using DiscordRPC;
using System;
using System.Collections.Generic;

namespace DiscordMultiTool.Services;

public sealed class RichPresenceService : IDisposable
{
    private DiscordRpcClient? _client;

    public bool IsActive => _client != null && _client.IsInitialized;

    public ActivityType UpdatePresence(
        string appId,
        string details,
        string state,
        int activityIndex,
        bool showTimestamp,
        string largeImageKey,
        string largeImageText,
        string smallImageKey,
        string smallImageText,
        string btn1Label,
        string btn1Url,
        string btn2Label,
        string btn2Url,
        string partySizeStr,
        string partyMaxStr)
    {
        if (string.IsNullOrWhiteSpace(appId))
            throw new ArgumentException("Application ID is required.", nameof(appId));

        appId = appId.Trim();

        if (_client == null || _client.ApplicationID != appId)
        {
            _client?.Dispose();
            _client = new DiscordRpcClient(appId);
            _client.Initialize();
        }

        ActivityType activityType = activityIndex switch
        {
            1 => ActivityType.Listening,
            2 => ActivityType.Watching,
            3 => ActivityType.Competing,
            _ => ActivityType.Playing
        };

        var presence = new RichPresence
        {
            Type = activityType,
            Details = string.IsNullOrWhiteSpace(details) ? null : details,
            State = string.IsNullOrWhiteSpace(state) ? null : state,
            Timestamps = showTimestamp ? Timestamps.Now : null
        };

        if (!string.IsNullOrWhiteSpace(largeImageKey) || !string.IsNullOrWhiteSpace(smallImageKey))
        {
            presence.Assets = new Assets
            {
                LargeImageKey = string.IsNullOrWhiteSpace(largeImageKey) ? null : largeImageKey.Trim(),
                LargeImageText = string.IsNullOrWhiteSpace(largeImageText) ? null : largeImageText,
                SmallImageKey = string.IsNullOrWhiteSpace(smallImageKey) ? null : smallImageKey.Trim(),
                SmallImageText = string.IsNullOrWhiteSpace(smallImageText) ? null : smallImageText
            };
        }

        var buttons = new List<Button>(2);
        string fixedUrl1 = NormalizeUrl(btn1Url);
        if (!string.IsNullOrWhiteSpace(btn1Label) && !string.IsNullOrEmpty(fixedUrl1))
            buttons.Add(new Button { Label = btn1Label, Url = fixedUrl1 });

        string fixedUrl2 = NormalizeUrl(btn2Url);
        if (!string.IsNullOrWhiteSpace(btn2Label) && !string.IsNullOrEmpty(fixedUrl2))
            buttons.Add(new Button { Label = btn2Label, Url = fixedUrl2 });

        if (buttons.Count > 0)
            presence.Buttons = buttons.ToArray();

        if (int.TryParse(partySizeStr, out int size) && int.TryParse(partyMaxStr, out int max) && size > 0 && max >= size)
        {
            presence.Party = new Party
            {
                ID = DiscordRPC.Secrets.CreateFriendlySecret(Random.Shared),
                Size = size,
                Max = max
            };
        }

        _client.SetPresence(presence);
        return activityType;
    }

    public bool Clear()
    {
        if (_client == null)
            return false;

        _client.ClearPresence();
        _client.Dispose();
        _client = null;
        return true;
    }

    public void Dispose()
    {
        if (_client != null)
        {
            try
            {
                _client.ClearPresence();
            }
            catch { }
            _client.Dispose();
            _client = null;
        }
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + url;
        }
        return url;
    }
}
