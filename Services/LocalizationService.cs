using Avalonia;
using System.Collections.Generic;

namespace DiscordMultiTool.Services;

public sealed class LocalizationService
{
    public string CurrentLanguage { get; private set; } = "en";

    public string GetString(string key)
    {
        var dict = CurrentLanguage == "it" ? ItalianStrings : EnglishStrings;
        return dict.TryGetValue(key, out var val) ? val : key;
    }

    public void ApplyLanguage(string langCode)
    {
        CurrentLanguage = langCode == "it" ? "it" : "en";

        var appResources = Application.Current?.Resources;
        if (appResources == null) return;

        var dict = CurrentLanguage == "it" ? ItalianStrings : EnglishStrings;
        foreach (var kvp in dict)
        {
            appResources[kvp.Key] = kvp.Value;
        }
    }

    private static readonly Dictionary<string, string> EnglishStrings = new()
    {
        // Sidebar & Categories
        { "Str_Nav_Overview", "OVERVIEW" },
        { "Str_Nav_Modules", "MODULES" },
        { "Str_Nav_System", "WORKSPACE" },
        { "Str_Dashboard", "Dashboard" },
        { "Str_RichPresence", "Rich Presence" },
        { "Str_DiscordBot", "Discord Bot" },
        { "Str_TelegramBot", "Telegram Bot" },
        { "Str_ProcessManager", "Process Monitor" },
        { "Str_Settings", "Settings & HUD" },
        { "Str_AccountLogin", "Cloud Account" },
        { "Str_NotSignedIn", "Not signed in" },

        // Headers
        { "Str_Header_Dashboard", "Live client telemetry, active modules, and quick launchers" },
        { "Str_Header_RichPresence", "Design and broadcast custom Discord activity with real-time preview" },
        { "Str_Header_DiscordBot", "Execute and monitor Python Discord bots in an isolated runner" },
        { "Str_Header_TelegramBot", "Execute and monitor Python Telegram bots in an isolated runner" },
        { "Str_Header_ProcessManager", "Inspect windowed desktop processes, memory usage, and PIDs" },
        { "Str_Header_Settings", "Configure startup behavior, language, and floating HUD aesthetics" },
        { "Str_Header_Account", "Authenticate with Firebase to sync your configuration across devices" },

        // Dashboard
        { "Str_Checking", "Checking..." },
        { "Str_Running", "Running" },
        { "Str_NotRunning", "Offline" },
        { "Str_RefreshStatus", "Refresh" },
        { "Str_System", "App Runtime" },
        { "Str_CheckUpdates", "Check Updates" },
        { "Str_QuickActions", "Modules & Workspaces" },
        { "Str_SetRichPresence", "Rich Presence Studio" },
        { "Str_Desc_RichPresence", "Custom status, artwork keys, buttons & party info" },
        { "Str_LoadDiscordBot", "Discord Bot Runner" },
        { "Str_Desc_DiscordBot", "Launch .py bots with live stdout/stderr streaming" },
        { "Str_LoadTelegramBot", "Telegram Bot Runner" },
        { "Str_Desc_TelegramBot", "Manage Telegram scripts with secure token injection" },
        { "Str_Desc_ProcessManager", "Filter active desktop apps, inspect RAM & terminate" },
        { "Str_ToggleOverlay", "Toggle HUD Overlay" },
        { "Str_Desc_Overlay", "Pin a translucent, customizable status HUD over games & apps" },

        // Rich Presence
        { "Str_BasicInfo", "Activity & Identity" },
        { "Str_ClientID", "Application ID (Client ID)" },
        { "Str_Details", "Details (Top Line)" },
        { "Str_State", "State (Second Line)" },
        { "Str_ShowTime", "Show Elapsed Timer" },
        { "Str_ActivityType", "Activity Type" },
        { "Str_Images", "Art Assets" },
        { "Str_LargeImgKey", "Large Image Key" },
        { "Str_LargeImgText", "Large Hover Text" },
        { "Str_SmallImgKey", "Small Image Key" },
        { "Str_SmallImgText", "Small Hover Text" },
        { "Str_Buttons", "Interactive Buttons" },
        { "Str_Btn1Label", "Button 1 Label" },
        { "Str_Btn1Url", "Button 1 URL" },
        { "Str_Btn2Label", "Button 2 Label" },
        { "Str_Btn2Url", "Button 2 URL" },
        { "Str_Party", "Party Size" },
        { "Str_CurSize", "Current Party" },
        { "Str_MaxSize", "Max Party" },
        { "Str_UpdatePresence", "Broadcast Presence" },
        { "Str_ClearPresence", "Clear" },
        { "Str_LivePreview", "LIVE DISCORD PREVIEW" },

        // Bots
        { "Str_BotToken", "Bot Token (injected via ENV)" },
        { "Str_ScriptPath", "Python Script (.py)" },
        { "Str_PythonPath", "Python Interpreter" },
        { "Str_Browse", "Browse" },
        { "Str_LaunchBot", "Start Runner" },
        { "Str_StopBot", "Stop Runner" },
        { "Str_ClearLogs", "Clear" },
        { "Str_CopyLogs", "Copy" },
        { "Str_BotConsole", "TERMINAL OUTPUT" },
        { "Str_BotStopped", "STOPPED" },
        { "Str_BotRunning", "RUNNING • PID {0}" },

        // Process Manager
        { "Str_ActiveProcesses", "Windowed Processes" },
        { "Str_SearchProcesses", "Search by process name, window title, or PID..." },
        { "Str_Refresh", "Refresh" },
        { "Str_EndProcess", "Terminate Process" },

        // Settings
        { "Str_Behavior", "General & Startup" },
        { "Str_AutoStart", "Launch at Windows Sign-In" },
        { "Str_AutoStartDesc", "Start DiscordMultiTool automatically with Windows" },
        { "Str_OverlayPos", "HUD Anchor Corner" },
        { "Str_OverlayPosDesc", "Default screen corner when opening the overlay" },
        { "Str_Language", "Interface Language" },
        { "Str_LanguageDesc", "Switch UI text immediately" },
        { "Str_ColorEditor", "HUD Color Studio" },
        { "Str_EditTarget", "Color Channel" },
        { "Str_EditTargetDesc", "Select which HUD layer to customize" },
        { "Str_ColorWheel", "RGBA Channels" },
        { "Str_HexColor", "HEX" },
        { "Str_Opacity", "Alpha:" },
        { "Str_SaveConfig", "Save All Settings" },

        // Overlay
        { "Str_RPM_Active", "Rich Presence: Active" },
        { "Str_RPM_Inactive", "Rich Presence: Inactive" },
        { "Str_Overlay_Procs", "Active Processes" },

        // Status Messages & Misc
        { "Status_Ready", "Ready" },
        { "Status_Saved", "Configuration saved locally and synced!" },
        { "Status_Viewing", "Viewing" },
        { "Status_OverlayEnabled", "HUD Overlay enabled" },
        { "Status_OverlayDisabled", "HUD Overlay closed" },
        { "Status_CheckUpdate", "Checking GitHub releases..." },
        { "Status_UpdateAvail", "Update available" },
        { "Status_UpToDate", "Up to date" },
        { "Status_UpdateFailed", "Update check failed" },
        { "Status_RPCUpdated", "Rich Presence active" },
        { "Status_Killed", "Terminated" },
        { "Status_KillFail", "Failed to terminate" },
        { "Status_NoProc", "Select a process from the list first." },
        { "Status_LoadedProcs", "{0} windowed processes active" },
        { "Status_RPCError", "RPC Error: {0}" },
        { "Status_AppIdRequired", "Application ID (Client ID) is required." },
        { "Status_PresenceCleared", "Rich Presence cleared" },
        { "Status_PresenceNotActive", "Rich Presence is not currently active" },
        { "Status_BotStarted", "{0} started (PID {1})" },
        { "Status_BotStopped", "{0} stopped" },
        { "Status_BotError", "{0} error: {1}" },
        { "Status_CopiedLogs", "Console logs copied to clipboard" },

        // Dropdowns
        { "Str_Act_Playing", "Playing" },
        { "Str_Act_Listening", "Listening to" },
        { "Str_Act_Watching", "Watching" },
        { "Str_Act_Competing", "Competing in" },
        { "Str_Pos_TopLeft", "Top Left" },
        { "Str_Pos_TopRight", "Top Right" },
        { "Str_Pos_BotLeft", "Bottom Left" },
        { "Str_Pos_BotRight", "Bottom Right" },

        // Watermarks
        { "WM_AppID", "e.g. 112233445566778899" },
        { "WM_Details", "e.g. Editing MainWindow.axaml" },
        { "WM_State", "e.g. Workspace: DiscordMultiTool" },
        { "WM_KeyName", "asset_key" },
        { "WM_HoverText", "Tooltip text" },
        { "WM_BtnLabel", "GitHub Repo" },
        { "WM_BtnUrl", "https://..." },
        { "WM_Token", "Paste bot token..." },
        { "WM_Path", @"C:\Bots\main.py" },

        // Boot Animation
        { "Str_Intro_Discord", "Detecting Discord client" },
        { "Str_Intro_Telegram", "Detecting Telegram client" },
        { "Str_Found", "Active" },
        { "Str_Stats_NotRunning", "Offline" },

        // Settings - Close Button Behavior
        { "Str_CloseButtonBehavior", "Window Close Action" },
        { "Str_CloseButtonBehaviorDesc", "Minimize to system tray or quit completely" },
        { "Str_MinimizeToTray", "Keep in Tray" },
        { "Str_CloseApp", "Quit App" },

        // Settings - Overlay Appearance
        { "Str_Settings_Appearance", "HUD Overlay Appearance" },
        { "Str_Settings_Rainbow", "Chroma Rainbow Cycle" },
        { "Str_Settings_RainbowDesc", "Smooth 60 FPS HSL spectrum animation" },
        { "Str_Settings_Border", "Crisp Outline Border" },
        { "Str_Settings_BorderDesc", "Render 1px frame around the HUD" },
        { "Str_Settings_BgOpacity", "Surface Opacity" },
        { "Str_Settings_MasterOpacity", "Master Window Opacity" },

        // Overlay Function Names
        { "Str_Overlay_DiscordBot", "Discord Bot" },
        { "Str_Overlay_TelegramBot", "Telegram Bot" },
        { "Str_Overlay_RichPresence", "Rich Presence" },
        { "Str_Overlay_Active", "ACTIVE" },
        { "Str_Overlay_Inactive", "IDLE" }
    };

    private static readonly Dictionary<string, string> ItalianStrings = new()
    {
        // Sidebar & Categories
        { "Str_Nav_Overview", "PANORAMICA" },
        { "Str_Nav_Modules", "MODULI" },
        { "Str_Nav_System", "SISTEMA" },
        { "Str_Dashboard", "Dashboard" },
        { "Str_RichPresence", "Rich Presence" },
        { "Str_DiscordBot", "Bot Discord" },
        { "Str_TelegramBot", "Bot Telegram" },
        { "Str_ProcessManager", "Monitor Processi" },
        { "Str_Settings", "Impostazioni & HUD" },
        { "Str_AccountLogin", "Account Cloud" },
        { "Str_NotSignedIn", "Non connesso" },

        // Headers
        { "Str_Header_Dashboard", "Telemetria client in tempo reale, moduli attivi e avvio rapido" },
        { "Str_Header_RichPresence", "Configura la presenza Discord con anteprima profilo in tempo reale" },
        { "Str_Header_DiscordBot", "Esegui e monitora bot Discord in Python con console integrata" },
        { "Str_Header_TelegramBot", "Esegui e monitora bot Telegram in Python con console integrata" },
        { "Str_Header_ProcessManager", " Controlla processi attivi, memoria RAM utilizzata e PID" },
        { "Str_Header_Settings", "Personalizza avvio, lingua e stile dell'HUD flottante" },
        { "Str_Header_Account", "Accedi con Firebase per sincronizzare le impostazioni nel cloud" },

        // Dashboard
        { "Str_Checking", "Verifica..." },
        { "Str_Running", "Attivo" },
        { "Str_NotRunning", "Offline" },
        { "Str_RefreshStatus", "Aggiorna" },
        { "Str_System", "Runtime App" },
        { "Str_CheckUpdates", "Aggiornamenti" },
        { "Str_QuickActions", "Moduli & Strumenti" },
        { "Str_SetRichPresence", "Studio Rich Presence" },
        { "Str_Desc_RichPresence", "Stato custom, immagini, pulsanti e party su Discord" },
        { "Str_LoadDiscordBot", "Runner Bot Discord" },
        { "Str_Desc_DiscordBot", "Avvia script .py con log stdout/stderr in tempo reale" },
        { "Str_LoadTelegramBot", "Runner Bot Telegram" },
        { "Str_Desc_TelegramBot", "Gestisci bot Telegram con token via variabile d'ambiente" },
        { "Str_Desc_ProcessManager", "Filtra applicazioni aperte, controlla RAM e termina" },
        { "Str_ToggleOverlay", "Attiva / Chiudi HUD Overlay" },
        { "Str_Desc_Overlay", "Mostra un pannello HUD compatto e personalizzabile sopra giochi e app" },

        // Rich Presence
        { "Str_BasicInfo", "Attività & Identità" },
        { "Str_ClientID", "ID Applicazione (Client ID)" },
        { "Str_Details", "Dettagli (Prima Riga)" },
        { "Str_State", "Stato (Seconda Riga)" },
        { "Str_ShowTime", "Mostra Timer Trascorso" },
        { "Str_ActivityType", "Tipo Attività" },
        { "Str_Images", "Risorse Immagini (Assets)" },
        { "Str_LargeImgKey", "Chiave Immagine Grande" },
        { "Str_LargeImgText", "Testo Immagine Grande" },
        { "Str_SmallImgKey", "Chiave Immagine Piccola" },
        { "Str_SmallImgText", "Testo Immagine Piccola" },
        { "Str_Buttons", "Pulsanti Interattivi" },
        { "Str_Btn1Label", "Etichetta Pulsante 1" },
        { "Str_Btn1Url", "URL Pulsante 1" },
        { "Str_Btn2Label", "Etichetta Pulsante 2" },
        { "Str_Btn2Url", "URL Pulsante 2" },
        { "Str_Party", "Dimensione Party" },
        { "Str_CurSize", "Membri Attuali" },
        { "Str_MaxSize", "Membri Massimi" },
        { "Str_UpdatePresence", "Aggiorna Presenza" },
        { "Str_ClearPresence", "Rimuovi" },
        { "Str_LivePreview", "ANTEPRIMA LIVE DISCORD" },

        // Bots
        { "Str_BotToken", "Token del Bot (passato via ENV)" },
        { "Str_ScriptPath", "Script Python (.py)" },
        { "Str_PythonPath", "Interprete Python" },
        { "Str_Browse", "Sfoglia" },
        { "Str_LaunchBot", "Avvia Bot" },
        { "Str_StopBot", "Ferma Bot" },
        { "Str_ClearLogs", "Pulisci" },
        { "Str_CopyLogs", "Copia" },
        { "Str_BotConsole", "OUTPUT CONSOLE" },
        { "Str_BotStopped", "FERMO" },
        { "Str_BotRunning", "ATTIVO • PID {0}" },

        // Process Manager
        { "Str_ActiveProcesses", "Processi con Finestra" },
        { "Str_SearchProcesses", "Cerca per nome processo, titolo finestra o PID..." },
        { "Str_Refresh", "Aggiorna" },
        { "Str_EndProcess", "Termina Processo" },

        // Settings
        { "Str_Behavior", "Generale & Avvio" },
        { "Str_AutoStart", "Avvia con Windows" },
        { "Str_AutoStartDesc", "Apri automaticamente DiscordMultiTool all'accesso" },
        { "Str_OverlayPos", "Angolo HUD Overlay" },
        { "Str_OverlayPosDesc", "Posizione predefinita sullo schermo" },
        { "Str_Language", "Lingua Interfaccia" },
        { "Str_LanguageDesc", "Cambia lingua immediatamente" },
        { "Str_ColorEditor", "Studio Colori HUD" },
        { "Str_EditTarget", "Livello Colore" },
        { "Str_EditTargetDesc", "Scegli quale elemento dell'HUD colorare" },
        { "Str_ColorWheel", "Canali RGBA" },
        { "Str_HexColor", "HEX" },
        { "Str_Opacity", "Alpha:" },
        { "Str_SaveConfig", "Salva Impostazioni" },

        // Overlay
        { "Str_RPM_Active", "Rich Presence: Attivo" },
        { "Str_RPM_Inactive", "Rich Presence: Inattivo" },
        { "Str_Overlay_Procs", "Processi Attivi" },

        // Status Messages & Misc
        { "Status_Ready", "Pronto" },
        { "Status_Saved", "Impostazioni salvate e sincronizzate!" },
        { "Status_Viewing", "Sezione:" },
        { "Status_OverlayEnabled", "HUD Overlay attivato" },
        { "Status_OverlayDisabled", "HUD Overlay chiuso" },
        { "Status_CheckUpdate", "Controllo rilasci GitHub..." },
        { "Status_UpdateAvail", "Aggiornamento disponibile" },
        { "Status_UpToDate", "Versione aggiornata" },
        { "Status_UpdateFailed", "Controllo fallito" },
        { "Status_RPCUpdated", "Rich Presence attivo" },
        { "Status_Killed", "Terminato" },
        { "Status_KillFail", "Impossibile terminare" },
        { "Status_NoProc", "Seleziona prima un processo dalla lista." },
        { "Status_LoadedProcs", "{0} processi attivi rilevati" },
        { "Status_RPCError", "Errore RPC: {0}" },
        { "Status_AppIdRequired", "L'ID Applicazione (Client ID) è obbligatorio." },
        { "Status_PresenceCleared", "Rich Presence rimosso" },
        { "Status_PresenceNotActive", "Rich Presence non è attivo" },
        { "Status_BotStarted", "{0} avviato (PID {1})" },
        { "Status_BotStopped", "{0} fermato" },
        { "Status_BotError", "Errore {0}: {1}" },
        { "Status_CopiedLogs", "Log copiati negli appunti" },

        // Dropdowns
        { "Str_Act_Playing", "Sta giocando a" },
        { "Str_Act_Listening", "Sta ascoltando" },
        { "Str_Act_Watching", "Sta guardando" },
        { "Str_Act_Competing", "Sta competendo in" },
        { "Str_Pos_TopLeft", "In alto a sinistra" },
        { "Str_Pos_TopRight", "In alto a destra" },
        { "Str_Pos_BotLeft", "In basso a sinistra" },
        { "Str_Pos_BotRight", "In basso a destra" },

        // Watermarks
        { "WM_AppID", "es. 112233445566778899" },
        { "WM_Details", "es. Modificando MainWindow.axaml" },
        { "WM_State", "es. Progetto: DiscordMultiTool" },
        { "WM_KeyName", "chiave_asset" },
        { "WM_HoverText", "Testo al passaggio" },
        { "WM_BtnLabel", "Repository GitHub" },
        { "WM_BtnUrl", "https://..." },
        { "WM_Token", "Incolla token del bot..." },
        { "WM_Path", @"C:\Bot\main.py" },

        // Boot Animation
        { "Str_Intro_Discord", "Ricerca client Discord" },
        { "Str_Intro_Telegram", "Ricerca client Telegram" },
        { "Str_Found", "Attivo" },
        { "Str_Stats_NotRunning", "Offline" },

        // Settings - Close Button Behavior
        { "Str_CloseButtonBehavior", "Azione Chiusura Finestra" },
        { "Str_CloseButtonBehaviorDesc", "Riduci nell'area di notifica o chiudi del tutto" },
        { "Str_MinimizeToTray", "Riduci nel Tray" },
        { "Str_CloseApp", "Chiudi App" },

        // Settings - Overlay Appearance
        { "Str_Settings_Appearance", "Stile HUD Overlay" },
        { "Str_Settings_Rainbow", "Ciclo Chroma Arcobaleno" },
        { "Str_Settings_RainbowDesc", "Animazione spettro HSL fluida a 60 FPS" },
        { "Str_Settings_Border", "Bordo Sottile" },
        { "Str_Settings_BorderDesc", "Mostra contorno 1px attorno al pannello" },
        { "Str_Settings_BgOpacity", "Opacità Sfondo" },
        { "Str_Settings_MasterOpacity", "Opacità Globale" },

        // Overlay Function Names
        { "Str_Overlay_DiscordBot", "Bot Discord" },
        { "Str_Overlay_TelegramBot", "Bot Telegram" },
        { "Str_Overlay_RichPresence", "Rich Presence" },
        { "Str_Overlay_Active", "ATTIVO" },
        { "Str_Overlay_Inactive", "INATTIVO" }
    };
}
