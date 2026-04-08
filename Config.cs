using System;

namespace DiscordMultiTool
{
    public class AppConfig
    {
        public string Language { get; set; } = "en";
        public bool AutoStart { get; set; } = false;
        public string OverlayPosition { get; set; } = "Top Left";
        
        // Rich Presence Data
        public string AppId { get; set; } = "";
        public string Details { get; set; } = "";
        public string State { get; set; } = "";
        public bool ShowTimestamp { get; set; } = false;
        public int ActivityType { get; set; } = 0; // 0=Playing, 1=Listening, etc.
        
        public string DiscordToken { get; set; } = "";
        public bool DiscordQuestsActive { get; set; } = false;
        
        public string LargeImageKey { get; set; } = "";
        public string LargeImageText { get; set; } = "";
        public string SmallImageKey { get; set; } = "";
        public string SmallImageText { get; set; } = "";
        
        public string Btn1Label { get; set; } = "";
        public string Btn1Url { get; set; } = "";
        public string Btn2Label { get; set; } = "";
        public string Btn2Url { get; set; } = "";
        
        public string PartySize { get; set; } = "";
        public string PartyMax { get; set; } = "";

        // Overlay Appearance
        public string OverlayBackgroundColor { get; set; } = "#BF0f172a";
        public string OverlayBorderColor { get; set; } = "#334155";
        public string OverlayTextColor { get; set; } = "White";
        public bool OverlayShowBorder { get; set; } = true;

        public bool OverlayRainbowMode { get; set; } = false;
        public double OverlayBgOpacity { get; set; } = 0.75;
        public double OverlayMasterOpacity { get; set; } = 1.0;

        // System Tray
        public bool MinimizeToTrayOnClose { get; set; } = true;
    }
}
