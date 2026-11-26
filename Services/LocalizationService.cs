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
        // Sidebar
        { "Str_Dashboard", "Dashboard" },
        { "Str_RichPresence", "Rich Presence" },
        { "Str_DiscordBot", "Discord Bot" },
        { "Str_TelegramBot", "Telegram Bot" },
        { "Str_ProcessManager", "Process Manager" },
        { "Str_Settings", "Settings" },
        { "Str_AccountLogin", "Account / Login" },

        // Headers
        { "Str_Header_Dashboard", "System Status and Quick Actions" },
        { "Str_Header_RichPresence", "Configure Discord Presence" },
        { "Str_Header_DiscordBot", "Run and Monitor Discord Python Bots" },
        { "Str_Header_TelegramBot", "Run and Monitor Telegram Python Bots" },
        { "Str_Header_ProcessManager", "Monitor and Control Processes" },
        { "Str_Header_Settings", "Customize Application Behavior" },
        { "Str_Header_Account", "Sign in to securely sync your settings" },

        // Dashboard
        { "Str_Checking", "Checking..." },
        { "Str_Running", "Running" },
        { "Str_NotRunning", "Not running" },
        { "Str_RefreshStatus", "Refresh Status" },
        { "Str_System", "System" },
        { "Str_CheckUpdates", "Check Updates" },
        { "Str_QuickActions", "Quick Actions" },
        { "Str_SetRichPresence", "Set Rich Presence" },
        { "Str_LoadDiscordBot", "Load Discord Bot" },
        { "Str_LoadTelegramBot", "Load Telegram Bot" },
        { "Str_ToggleOverlay", "Toggle Overlay" },

        // Rich Presence
        { "Str_BasicInfo", "Basic Info" },
        { "Str_ClientID", "Client ID" },
        { "Str_Details", "Details" },
        { "Str_State", "State" },
        { "Str_ShowTime", "Show Elapsed Time" },
        { "Str_ActivityType", "Activity Type" },
        { "Str_Images", "Images (Assets)" },
        { "Str_LargeImgKey", "Large Image Key" },
        { "Str_LargeImgText", "Large Image Text" },
        { "Str_SmallImgKey", "Small Image Key" },
        { "Str_SmallImgText", "Small Image Text" },
        { "Str_Buttons", "Buttons" },
        { "Str_Btn1Label", "Button 1 Label" },
        { "Str_Btn1Url", "Button 1 URL" },
        { "Str_Btn2Label", "Button 2 Label" },
        { "Str_Btn2Url", "Button 2 URL" },
        { "Str_Party", "Party (Optional)" },
        { "Str_CurSize", "Current Size" },
        { "Str_MaxSize", "Max Size" },
        { "Str_UpdatePresence", "Update Presence" },
        { "Str_ClearPresence", "Clear Rich Presence" },

        // Bots
        { "Str_BotToken", "Bot Token (passed via environment variable)" },
        { "Str_ScriptPath", "Script Path (.py)" },
        { "Str_PythonPath", "Python Executable" },
        { "Str_Browse", "Browse..." },
        { "Str_LaunchBot", "Launch Bot" },
        { "Str_StopBot", "Stop Bot" },
        { "Str_ClearLogs", "Clear Logs" },
        { "Str_BotConsole", "Live Console Output" },
        { "Str_BotStopped", "Stopped" },
        { "Str_BotRunning", "Running (PID: {0})" },

        // Process Manager
        { "Str_ActiveProcesses", "Active Processes" },
        { "Str_SearchProcesses", "Filter processes by name or PID..." },
        { "Str_Refresh", "Refresh" },
        { "Str_EndProcess", "End Selected Process" },

        // Settings
        { "Str_Behavior", "Behavior" },
        { "Str_AutoStart", "Open at Windows Start" },
        { "Str_AutoStartDesc", "Launch automatically when you sign in" },
        { "Str_OverlayPos", "Overlay Position" },
        { "Str_OverlayPosDesc", "Choose where the overlay appears" },
        { "Str_Language", "Language" },
        { "Str_ColorEditor", "Color Editor" },
        { "Str_EditTarget", "Edit Target" },
        { "Str_EditTargetDesc", "Select which element to color" },
        { "Str_ColorWheel", "Color Sliders" },
        { "Str_HexColor", "Hex Color" },
        { "Str_Opacity", "Opacity" },

        // Overlay
        { "Str_RPM_Active", "Rich Presence: Active" },
        { "Str_RPM_Inactive", "Rich Presence: Inactive" },
        { "Str_Overlay_Procs", "Active Processes" },
        { "Str_LanguageDesc", "Select Application Language" },
        { "Str_SaveConfig", "Save Configurations" },

        // Status Messages & Misc
        { "Status_Ready", "Ready" },
        { "Status_Saved", "Settings saved successfully!" },
        { "Status_Viewing", "Viewing" },
        { "Status_OverlayEnabled", "Overlay enabled" },
        { "Status_OverlayDisabled", "Overlay disabled" },
        { "Status_CheckUpdate", "Checking for updates..." },
        { "Status_UpdateAvail", "Update available" },
        { "Status_UpToDate", "You are up to date!" },
        { "Status_UpdateFailed", "Update check failed" },
        { "Status_RPCUpdated", "Presence Updated" },
        { "Status_Killed", "Killed process" },
        { "Status_KillFail", "Failed to kill process" },
        { "Status_NoProc", "No process selected." },
        { "Status_LoadedProcs", "Loaded {0} processes." },
        { "Status_RPCError", "RPC Error: {0}" },
        { "Status_AppIdRequired", "Error: Application ID is required." },
        { "Status_PresenceCleared", "Rich Presence cleared" },
        { "Status_PresenceNotActive", "Rich Presence is not active" },
        { "Status_BotStarted", "{0} started (PID: {1})" },
        { "Status_BotStopped", "{0} stopped" },
        { "Status_BotError", "{0} error: {1}" },

        // Dropdowns
        { "Str_Act_Playing", "Playing" },
        { "Str_Act_Listening", "Listening" },
        { "Str_Act_Watching", "Watching" },
        { "Str_Act_Competing", "Competing" },
        { "Str_Pos_TopLeft", "Top Left" },
        { "Str_Pos_TopRight", "Top Right" },
        { "Str_Pos_BotLeft", "Bottom Left" },
        { "Str_Pos_BotRight", "Bottom Right" },

        // Watermarks
        { "WM_AppID", "Application ID" },
        { "WM_Details", "e.g. In Menu" },
        { "WM_State", "e.g. Ranked Match" },
        { "WM_KeyName", "key_name" },
        { "WM_HoverText", "Hover text" },
        { "WM_BtnLabel", "Visit Website" },
        { "WM_BtnUrl", "https://..." },
        { "WM_Token", "Paste your token..." },
        { "WM_Path", @"C:\Path\To\bot.py" },

        // Boot Animation
        { "Str_Intro_Discord", "Checking if Discord is open..." },
        { "Str_Intro_Telegram", "Checking if Telegram is open..." },
        { "Str_Found", "Found!" },
        { "Str_Stats_NotRunning", "Not Running" },

        // Settings - Close Button Behavior
        { "Str_CloseButtonBehavior", "Close Button Behavior" },
        { "Str_CloseButtonBehaviorDesc", "Choose what happens when you close the window" },
        { "Str_MinimizeToTray", "Run in Background" },
        { "Str_CloseApp", "Close the App" },

        // Settings - Overlay Appearance
        { "Str_Settings_Appearance", "Overlay Appearance" },
        { "Str_Settings_Rainbow", "Rainbow Mode" },
        { "Str_Settings_RainbowDesc", "Cycles colors automatically" },
        { "Str_Settings_Border", "Show Border" },
        { "Str_Settings_BorderDesc", "Draw outline around panel" },
        { "Str_Settings_BgOpacity", "Background Opacity" },
        { "Str_Settings_MasterOpacity", "Master Opacity" },

        // Overlay Function Names
        { "Str_Overlay_DiscordBot", "Discord Bot" },
        { "Str_Overlay_TelegramBot", "Telegram Bot" },
        { "Str_Overlay_RichPresence", "Rich Presence" },
        { "Str_Overlay_Active", "Active" },
        { "Str_Overlay_Inactive", "Inactive" }
    };

    private static readonly Dictionary<string, string> ItalianStrings = new()
    {
        // Sidebar
        { "Str_Dashboard", "Dashboard" },
        { "Str_RichPresence", "Rich Presence" },
        { "Str_DiscordBot", "Bot Discord" },
        { "Str_TelegramBot", "Bot Telegram" },
        { "Str_ProcessManager", "Gestione Processi" },
        { "Str_Settings", "Impostazioni" },
        { "Str_AccountLogin", "Account / Login" },

        // Headers
        { "Str_Header_Dashboard", "Panoramica del sistema e azioni rapide" },
        { "Str_Header_RichPresence", "Configura la Presenza su Discord" },
        { "Str_Header_DiscordBot", "Avvia e Monitora Bot Discord Python" },
        { "Str_Header_TelegramBot", "Avvia e Monitora Bot Telegram Python" },
        { "Str_Header_ProcessManager", "Monitora e Controlla Processi" },
        { "Str_Header_Settings", "Personalizza Comportamento Applicazione" },
        { "Str_Header_Account", "Accedi per sincronizzare le tue impostazioni" },

        // Dashboard
        { "Str_Checking", "Controllo in corso..." },
        { "Str_Running", "In Esecuzione" },
        { "Str_NotRunning", "Non in esecuzione" },
        { "Str_RefreshStatus", "Aggiorna Stato" },
        { "Str_System", "Sistema" },
        { "Str_CheckUpdates", "Controlla Aggiornamenti" },
        { "Str_QuickActions", "Azioni Rapide" },
        { "Str_SetRichPresence", "Imposta Rich Presence" },
        { "Str_LoadDiscordBot", "Carica Bot Discord" },
        { "Str_LoadTelegramBot", "Carica Bot Telegram" },
        { "Str_ToggleOverlay", "Attiva/Disattiva Overlay" },

        // Rich Presence
        { "Str_BasicInfo", "Informazioni Base" },
        { "Str_ClientID", "ID Applicazione (Client ID)" },
        { "Str_Details", "Dettagli" },
        { "Str_State", "Stato" },
        { "Str_ShowTime", "Mostra Tempo Trascorso" },
        { "Str_ActivityType", "Tipo di Attività" },
        { "Str_Images", "Immagini (Assets)" },
        { "Str_LargeImgKey", "Chiave Immagine Grande" },
        { "Str_LargeImgText", "Testo Immagine Grande" },
        { "Str_SmallImgKey", "Chiave Immagine Piccola" },
        { "Str_SmallImgText", "Testo Immagine Piccola" },
        { "Str_Buttons", "Pulsanti" },
        { "Str_Btn1Label", "Etichetta Pulsante 1" },
        { "Str_Btn1Url", "URL Pulsante 1" },
        { "Str_Btn2Label", "Etichetta Pulsante 2" },
        { "Str_Btn2Url", "URL Pulsante 2" },
        { "Str_Party", "Party (Opzionale)" },
        { "Str_CurSize", "Dimensione Attuale" },
        { "Str_MaxSize", "Dimensione Massima" },
        { "Str_UpdatePresence", "Aggiorna Presenza" },
        { "Str_ClearPresence", "Cancella Rich Presence" },

        // Bots
        { "Str_BotToken", "Token del Bot (passato tramite variabile d'ambiente)" },
        { "Str_ScriptPath", "Percorso Script (.py)" },
        { "Str_PythonPath", "Eseguibile Python" },
        { "Str_Browse", "Sfoglia..." },
        { "Str_LaunchBot", "Avvia Bot" },
        { "Str_StopBot", "Ferma Bot" },
        { "Str_ClearLogs", "Pulisci Log" },
        { "Str_BotConsole", "Console Live" },
        { "Str_BotStopped", "Fermo" },
        { "Str_BotRunning", "In Esecuzione (PID: {0})" },

        // Process Manager
        { "Str_ActiveProcesses", "Processi Attivi" },
        { "Str_SearchProcesses", "Filtra processi per nome o PID..." },
        { "Str_Refresh", "Aggiorna" },
        { "Str_EndProcess", "Termina Processo Selezionato" },

        // Settings
        { "Str_Behavior", "Comportamento" },
        { "Str_AutoStart", "Avvia con Windows" },
        { "Str_AutoStartDesc", "Avvia automaticamente all'accesso" },
        { "Str_OverlayPos", "Posizione Overlay" },
        { "Str_OverlayPosDesc", "Scegli dove appare l'overlay" },
        { "Str_Language", "Lingua" },
        { "Str_ColorEditor", "Editor Colori" },
        { "Str_EditTarget", "Scegli Elemento" },
        { "Str_EditTargetDesc", "Seleziona quale elemento colorare" },
        { "Str_ColorWheel", "Slider Colori" },
        { "Str_HexColor", "Colore Esadecimale" },
        { "Str_Opacity", "Opacità" },

        // Overlay
        { "Str_RPM_Active", "Rich Presence: Attivo" },
        { "Str_RPM_Inactive", "Rich Presence: Inattivo" },
        { "Str_Overlay_Procs", "Processi Attivi" },
        { "Str_LanguageDesc", "Seleziona Lingua Applicazione" },
        { "Str_SaveConfig", "Salva Configurazioni" },

        // Status Messages & Misc
        { "Status_Ready", "Pronto" },
        { "Status_Saved", "Impostazioni salvate con successo!" },
        { "Status_Viewing", "Visualizzando" },
        { "Status_OverlayEnabled", "Overlay abilitato" },
        { "Status_OverlayDisabled", "Overlay disabilitato" },
        { "Status_CheckUpdate", "Controllo aggiornamenti..." },
        { "Status_UpdateAvail", "Aggiornamento disponibile" },
        { "Status_UpToDate", "Sei aggiornato!" },
        { "Status_UpdateFailed", "Controllo aggiornamenti fallito" },
        { "Status_RPCUpdated", "Presenza Aggiornata" },
        { "Status_Killed", "Processo terminato" },
        { "Status_KillFail", "Impossibile terminare processo" },
        { "Status_NoProc", "Nessun processo selezionato." },
        { "Status_LoadedProcs", "Caricati {0} processi." },
        { "Status_RPCError", "Errore RPC: {0}" },
        { "Status_AppIdRequired", "Errore: ID Applicazione richiesto." },
        { "Status_PresenceCleared", "Rich Presence cancellato" },
        { "Status_PresenceNotActive", "Rich Presence non è attivo" },
        { "Status_BotStarted", "{0} avviato (PID: {1})" },
        { "Status_BotStopped", "{0} fermato" },
        { "Status_BotError", "Errore {0}: {1}" },

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
        { "WM_AppID", "ID Applicazione" },
        { "WM_Details", "es. Nel Menu" },
        { "WM_State", "es. Partita Classificata" },
        { "WM_KeyName", "nome_chiave" },
        { "WM_HoverText", "Testo al passaggio del mouse" },
        { "WM_BtnLabel", "Visita Sito Web" },
        { "WM_BtnUrl", "https://..." },
        { "WM_Token", "Incolla il tuo token..." },
        { "WM_Path", @"C:\Percorso\Del\bot.py" },

        // Boot Animation
        { "Str_Intro_Discord", "Controllo se Discord è aperto..." },
        { "Str_Intro_Telegram", "Controllo se Telegram è aperto..." },
        { "Str_Found", "Trovato!" },
        { "Str_Stats_NotRunning", "Non in Esecuzione" },

        // Settings - Close Button Behavior
        { "Str_CloseButtonBehavior", "Comportamento Pulsante Chiusura" },
        { "Str_CloseButtonBehaviorDesc", "Scegli cosa succede quando chiudi la finestra" },
        { "Str_MinimizeToTray", "Esegui in Background" },
        { "Str_CloseApp", "Chiudi l'Applicazione" },

        // Settings - Overlay Appearance
        { "Str_Settings_Appearance", "Aspetto Overlay" },
        { "Str_Settings_Rainbow", "Modalità Arcobaleno" },
        { "Str_Settings_RainbowDesc", "Cicla i colori automaticamente" },
        { "Str_Settings_Border", "Mostra Bordo" },
        { "Str_Settings_BorderDesc", "Disegna contorno" },
        { "Str_Settings_BgOpacity", "Opacità Sfondo" },
        { "Str_Settings_MasterOpacity", "Opacità Master" },

        // Overlay Function Names
        { "Str_Overlay_DiscordBot", "Bot Discord" },
        { "Str_Overlay_TelegramBot", "Bot Telegram" },
        { "Str_Overlay_RichPresence", "Rich Presence" },
        { "Str_Overlay_Active", "Attivo" },
        { "Str_Overlay_Inactive", "Inattivo" }
    };
}
