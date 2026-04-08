using Avalonia;
using Avalonia.Controls;
using DiscordRPC;
using Button = Avalonia.Controls.Button;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using DiscordMultiTool.Animations;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;

namespace DiscordMultiTool;

public partial class MainWindow : Window
{
    private StackPanel? _currentPage;
    private bool _isTransitioning = false;
    private DiscordRpcClient? _rpcClient;
    private OverlayWindow? _overlayWindow;
    private TrayIcon? _trayIcon;
    private bool _isClosing = false;
    
    // Track bot states for overlay
    private bool _discordBotActive = false;
    private bool _telegramBotActive = false;

    private string ConfigPath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "DiscordMultiTool");
            return Path.Combine(folder, "config.json");
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializeTheme();
        _currentPage = DashboardContent;
        InitializeTrayIcon();
        Closing += OnWindowClosing;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Topmost = true;
        Activate();
        _ = PlayIntroAnimation();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TrayIcon
        {
            Icon = Icon,
            ToolTipText = "DiscordMultiTool"
        };

        var menu = new NativeMenu();
        
        var showItem = new NativeMenuItem { Header = "Show" };
        showItem.Click += (s, e) => RestoreWindow();
        menu.Items.Add(showItem);
        
        var exitItem = new NativeMenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => ExitApplication();
        menu.Items.Add(exitItem);
        
        _trayIcon.Menu = menu;
        _trayIcon.Clicked += (s, e) => RestoreWindow();
        _trayIcon.IsVisible = true;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosing)
        {
            // Actually closing - cleanup
            _trayIcon?.Dispose();
            _rpcClient?.Dispose();
            _overlayWindow?.Close();
            return;
        }

        // Check user preference
        bool minimizeToTray = true; // Default
        try
        {
            string path = ConfigPath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                    minimizeToTray = config.MinimizeToTrayOnClose;
            }
        }
        catch { }

        if (minimizeToTray)
        {
            // Minimize to tray
            e.Cancel = true;
            Hide();
        }
        else
        {
            // Allow normal close
            _trayIcon?.Dispose();
            _rpcClient?.Dispose();
            _overlayWindow?.Close();
        }
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _isClosing = true;
        Close();
    }

    private void InitializeTheme()
    {
        ApplyDarkTheme();
    }

    private void ApplyDarkTheme()
    {
        var appResources = Application.Current?.Resources;
        if (appResources != null)
        {
            appResources["BackgroundColor"] = Color.Parse("#0F172A");
            appResources["SurfaceColor"] = Color.Parse("#1E293B");
            appResources["ForegroundColor"] = Colors.White;
            appResources["TextColor"] = Color.Parse("#A0A0A0");
            appResources["AccentColor"] = Color.Parse("#6C5CE7");
            appResources["ButtonBackground"] = new SolidColorBrush(Color.Parse("#1E293B"));
            appResources["ButtonForeground"] = new SolidColorBrush(Colors.White);
            appResources["ButtonBorder"] = new SolidColorBrush(Color.Parse("#6C5CE7"));
            appResources["ButtonHoverBackground"] = new SolidColorBrush(Color.Parse("#223244"));
            appResources["ButtonPressedBackground"] = new SolidColorBrush(Color.Parse("#182230"));
        }

        // Initialize Language (Default English)
        ApplyLanguage("en");
        _currentLang = "en";
        
        // Load Config
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            string path = ConfigPath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    ApplyConfigToUI(config);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
        }
    }

    private void ApplyConfigToUI(AppConfig config)
    {
        // Language
        if (!string.IsNullOrEmpty(config.Language))
        {
            ApplyLanguage(config.Language);
            _currentLang = config.Language;
            
            // Update Selector in Settings
            if (LanguageChoice != null)
            {
                foreach (ComboBoxItem item in LanguageChoice.Items)
                {
                    if (item.Tag is string t && t == _currentLang)
                    {
                        LanguageChoice.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // AutoStart
        if (AutoStartSwitch != null)
            AutoStartSwitch.IsChecked = config.AutoStart;
        
        // Overlay Position
        if (OverlayPosSelector != null && !string.IsNullOrEmpty(config.OverlayPosition))
        {
            foreach (ComboBoxItem item in OverlayPosSelector.Items)
            {
                if (item.Tag is string t && t == config.OverlayPosition)
                {
                    OverlayPosSelector.SelectedItem = item;
                    break;
                }
            }
        }

        // Rich Presence Fields
        if (AppIdBox != null) AppIdBox.Text = config.AppId;
        if (DetailsBox != null) DetailsBox.Text = config.Details;
        if (StateBox != null) StateBox.Text = config.State;
                if (TimestampCheck != null) TimestampCheck.IsChecked = config.ShowTimestamp;
                if (ActivityTypeSelector != null) ActivityTypeSelector.SelectedIndex = config.ActivityType;
        
                if (LargeImageKeyBox != null) LargeImageKeyBox.Text = config.LargeImageKey;        if (LargeImageTextBox != null) LargeImageTextBox.Text = config.LargeImageText;
        if (SmallImageKeyBox != null) SmallImageKeyBox.Text = config.SmallImageKey;
        if (SmallImageTextBox != null) SmallImageTextBox.Text = config.SmallImageText;
        
        if (Btn1LabelBox != null) Btn1LabelBox.Text = config.Btn1Label;
        if (Btn1UrlBox != null) Btn1UrlBox.Text = config.Btn1Url;
        if (Btn2LabelBox != null) Btn2LabelBox.Text = config.Btn2Label;
        if (Btn2UrlBox != null) Btn2UrlBox.Text = config.Btn2Url;
        
        if (PartySizeBox != null) PartySizeBox.Text = config.PartySize;
        if (PartyMaxBox != null) PartyMaxBox.Text = config.PartyMax;

        // Overlay Appearance
        if (Color.TryParse(config.OverlayBackgroundColor, out Color bgColor)) _bgColor = bgColor;
        if (Color.TryParse(config.OverlayBorderColor, out Color borderColor)) _borderColor = borderColor;
        if (Color.TryParse(config.OverlayTextColor, out Color textColor)) _textColor = textColor;

        // Initialize Color Sliders to Background
        UpdateColorSliders(_bgColor);

        if (OverlayRainbowSwitch != null) OverlayRainbowSwitch.IsChecked = config.OverlayRainbowMode;
        if (OverlayBorderSwitch != null) OverlayBorderSwitch.IsChecked = config.OverlayShowBorder;
        if (OverlayBgOpacitySlider != null) OverlayBgOpacitySlider.Value = config.OverlayBgOpacity;
        if (OverlayMasterOpacitySlider != null) OverlayMasterOpacitySlider.Value = config.OverlayMasterOpacity;

        // Close Button Behavior
        if (CloseButtonBehaviorSelector != null)
        {
            CloseButtonBehaviorSelector.SelectedIndex = config.MinimizeToTrayOnClose ? 0 : 1;
        }
    }

    private string _currentLang = "en";

    private string GetString(string key)
    {
        var dict = _currentLang == "it" ? _italianStrings : _englishStrings;
        return dict.TryGetValue(key, out var val) ? val : key;
    }

    private void LanguageChoice_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
        {
            if (_currentLang != langCode)
            {
                ApplyLanguage(langCode);
                _currentLang = langCode;
                
                // Refresh UI elements that are set via code
                RefreshStatus(null, null);
                
                // Update Status Bar
                StatusBar.Text = GetString("Status_Ready");

                // Refresh Page Header
                UpdatePageHeader();
            }
        }
    }

    private void UpdatePageHeader()
    {
        if (_currentPage == DashboardContent)
        {
            PageTitle.Text = GetString("Str_Dashboard"); 
            PageDescription.Text = GetString("Str_Header_Dashboard");
        }
        else if (_currentPage == RichPresenceContent)
        {
            PageTitle.Text = GetString("Str_RichPresence");
            PageDescription.Text = GetString("Str_Header_RichPresence");
        }
        else if (_currentPage == DiscordBotContent)
        {
             PageTitle.Text = GetString("Str_DiscordBot");
             PageDescription.Text = GetString("Str_Header_DiscordBot");
        }
                  else if (_currentPage == TelegramBotContent)
                  {
                       PageTitle.Text = GetString("Str_TelegramBot");
                       PageDescription.Text = GetString("Str_Header_TelegramBot");
                  }
                  else if (_currentPage == ProcessManagerContent)        {
             PageTitle.Text = GetString("Str_ProcessManager");
             PageDescription.Text = GetString("Str_Header_ProcessManager");
        }
        else if (_currentPage == SettingsContent)
        {
             PageTitle.Text = GetString("Str_Settings");
             PageDescription.Text = GetString("Str_Header_Settings");
        }
    }

    private void ApplyLanguage(string langCode)
    {
        var appResources = Application.Current?.Resources;
        if (appResources == null) return;

        var dict = langCode == "it" ? _italianStrings : _englishStrings;

        foreach (var kvp in dict)
        {
            appResources[kvp.Key] = kvp.Value;
        }
    }

    // ... Dictionaries ...

    // ... InitializeTheme ...

    // ... ApplyDarkTheme / ApplyLightTheme ...



    private readonly Dictionary<string, string> _englishStrings = new()
    {
        // Sidebar
        { "Str_Dashboard", "Dashboard" },
        { "Str_RichPresence", "Rich Presence" },
        { "Str_DiscordBot", "Discord Bot" },
        { "Str_TelegramBot", "Telegram Bot" },
        { "Str_ProcessManager", "Process Manager" },
        { "Str_Settings", "Settings" },

        // Headers
        { "Str_Header_Dashboard", "System Status and Quick Actions" },
        { "Str_Header_RichPresence", "Configure Discord Presence" },
        { "Str_Header_DiscordBot", "Load and Manage Bots" },
        { "Str_Header_TelegramBot", "Load and Manage Bots" },
        { "Str_Header_ProcessManager", "Monitor and Control Processes" },
        { "Str_Header_Settings", "Customize Application Behavior" },

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
        { "Str_BotToken", "Bot Token" },
        { "Str_ScriptPath", "Script Path" },
        { "Str_LaunchBot", "Launch Bot" },
        
        // Process Manager
        { "Str_ActiveProcesses", "Active Processes" },
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
        { "Str_ColorWheel", "Color Wheel" },
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
        { "Status_NoProc", "Nessun processo selezionato." },
        { "Status_LoadedProcs", "Loaded {0} processes." },
        { "Status_RPCError", "RPC Error: {0}" },
        { "Status_AppIdRequired", "Error: Application ID is required." },
        { "Status_PresenceCleared", "Rich Presence cleared" },
        { "Status_PresenceNotActive", "Rich Presence is not active" },

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
        { "WM_Path", "C:\\Path\\To\\File..." },

        // Boot Animation
        { "Str_Intro_Discord", "Checking if discord is open..." },
        { "Str_Intro_Telegram", "Checking if telegram is open..." },
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

    private readonly Dictionary<string, string> _italianStrings = new()
    {
        // Sidebar
        { "Str_Dashboard", "Dashboard" },
        { "Str_RichPresence", "Rich Presence" },
        { "Str_DiscordBot", "Bot Discord" },
        { "Str_TelegramBot", "Bot Telegram" },
        { "Str_ProcessManager", "Gestione Processi" },
        { "Str_Settings", "Impostazioni" },

        // Headers
        { "Str_Header_Dashboard", "Panoramica del sistema e azioni rapide" },
        { "Str_Header_RichPresence", "Configura la Presenza su Discord" },
        { "Str_Header_DiscordBot", "Carica e Gestisci Bot" },
        { "Str_Header_TelegramBot", "Carica e Gestisci Bot" },
        { "Str_Header_ProcessManager", "Monitora e Controlla Processi" },
        { "Str_Header_Settings", "Personalizza Comportamento Applicazione" },

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
        { "Str_BotToken", "Token del Bot" },
        { "Str_ScriptPath", "Percorso Script" },
        { "Str_LaunchBot", "Avvia Bot" },
        
        // Process Manager
        { "Str_ActiveProcesses", "Processi Attivi" },
        { "Str_Refresh", "Aggiorna" },
        { "Str_EndProcess", "Termina Processo Selezionato" },

        { "Str_AutoStart", "Avvia con Windows" },
        { "Str_AutoStartDesc", "Avvia automaticamente all'accesso" },
        { "Str_OverlayPos", "Posizione Overlay" },
        { "Str_OverlayPosDesc", "Scegli dove appare l'overlay" },
        { "Str_Language", "Lingua" },
        { "Str_ColorEditor", "Editor Colori" },
        { "Str_EditTarget", "Scegli Elemento" },
        { "Str_EditTargetDesc", "Seleziona quale elemento colorare" },
        { "Str_ColorWheel", "Ruota Colori" },
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
        { "WM_Path", "C:\\Percorso\\Del\\File..." },

        // Boot Animation
        { "Str_Intro_Discord", "Controllo se discord è aperto..." },
        { "Str_Intro_Telegram", "Controllo se telegram è aperto..." },
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

    private void ApplyLightTheme()
    {
        var appResources = Application.Current?.Resources;
        if (appResources != null)
        {
            appResources["BackgroundColor"] = Colors.White;
            appResources["SurfaceColor"] = Color.Parse("#F3F4F6");
            appResources["ForegroundColor"] = Color.Parse("#1F2937");
            appResources["TextColor"] = Color.Parse("#6B7280");
            appResources["AccentColor"] = Color.Parse("#7C3AED");
            appResources["ButtonBackground"] = new SolidColorBrush(Colors.White);
            appResources["ButtonForeground"] = new SolidColorBrush(Colors.Black);
            appResources["ButtonBorder"] = new SolidColorBrush(Colors.Black);
            appResources["ButtonHoverBackground"] = new SolidColorBrush(Color.Parse("#E5E7EB"));
            appResources["ButtonPressedBackground"] = new SolidColorBrush(Color.Parse("#E0E0E0"));
        }

        Background = new SolidColorBrush(Colors.White);
        StatusBar.Text = "Light theme applied!";
        StatusBar.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
    }



    private AppConfig GetCurrentAppConfig()
    {
        var config = new AppConfig();
        
        // Language
        if (LanguageChoice?.SelectedItem is ComboBoxItem langItem && langItem.Tag is string lang)
            config.Language = lang;
        else
            config.Language = _currentLang; // Fallback

        // Overlay Position
        if (OverlayPosSelector?.SelectedItem is ComboBoxItem posItem && posItem.Tag is string pos)
            config.OverlayPosition = pos;

        // Auto-Start
        config.AutoStart = AutoStartSwitch?.IsChecked ?? false;

        // Rich Presence Fields
        config.AppId = AppIdBox?.Text ?? "";
        config.Details = DetailsBox?.Text ?? "";
        config.State = StateBox?.Text ?? "";
                config.ShowTimestamp = TimestampCheck?.IsChecked ?? false;
                config.ActivityType = ActivityTypeSelector?.SelectedIndex ?? 0;
        
                config.LargeImageKey = LargeImageKeyBox?.Text ?? "";        config.LargeImageText = LargeImageTextBox?.Text ?? "";
        config.SmallImageKey = SmallImageKeyBox?.Text ?? "";
        config.SmallImageText = SmallImageTextBox?.Text ?? "";
        
        config.Btn1Label = Btn1LabelBox?.Text ?? "";
        config.Btn1Url = Btn1UrlBox?.Text ?? "";
        config.Btn2Label = Btn2LabelBox?.Text ?? "";
        config.Btn2Url = Btn2UrlBox?.Text ?? "";
        
        config.PartySize = PartySizeBox?.Text ?? "";
        config.PartyMax = PartyMaxBox?.Text ?? "";

        // Overlay Appearance
        config.OverlayBackgroundColor = _bgColor.ToString();
        config.OverlayBorderColor = _borderColor.ToString();
        config.OverlayTextColor = _textColor.ToString();
        config.OverlayRainbowMode = OverlayRainbowSwitch?.IsChecked ?? false;
        config.OverlayShowBorder = OverlayBorderSwitch?.IsChecked ?? true;
        config.OverlayBgOpacity = OverlayBgOpacitySlider?.Value ?? 0.75;
        config.OverlayMasterOpacity = OverlayMasterOpacitySlider?.Value ?? 1.0;

        // Close Button Behavior
        if (CloseButtonBehaviorSelector?.SelectedItem is ComboBoxItem behaviorItem && behaviorItem.Tag is string behavior)
            config.MinimizeToTrayOnClose = behavior == "minimize";
        else
            config.MinimizeToTrayOnClose = true; // Default

        return config;
    }

    private void SaveSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        
        try
        {
            var config = GetCurrentAppConfig();

            // Handle Registry AutoStart
            UpdateAutoStart(config.AutoStart);

            // Update Overlay Immediate if Open (Already handled by RealTime but safe to enforce)
            ApplyOverlayAppearance();
            
            StatusBar.Text = GetString("Status_Saved");
            
            // Save to cloud if logged in
            _ = SaveCloudConfig();
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error saving: {ex.Message}";
        }
    }
    
    // Helper to manage Registry AutoStart
    private void UpdateAutoStart(bool enable)
    {
        try
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (key == null) return;
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (exePath != null)
                        key.SetValue("DiscordMultiTool", $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue("DiscordMultiTool", false);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registry Error: {ex.Message}");
        }
    }

    private void NavigateToDashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(DashboardContent, GetString("Str_Dashboard"), GetString("Str_Header_Dashboard"));
    }

    private void NavigateToRichPresence(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(RichPresenceContent, GetString("Str_RichPresence"), GetString("Str_Header_RichPresence"));
    }

    private void NavigateToDiscordBot(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(DiscordBotContent, GetString("Str_DiscordBot"), GetString("Str_Header_DiscordBot"));
    }

    private void NavigateToTelegramBot(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(TelegramBotContent, GetString("Str_TelegramBot"), GetString("Str_Header_TelegramBot"));
    }

    private void NavigateToProcessManager(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(ProcessManagerContent, GetString("Str_ProcessManager"), GetString("Str_Header_ProcessManager"));
        LoadProcesses();
    }

    private void NavigateToSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(SettingsContent, GetString("Str_Settings"), GetString("Str_Header_Settings"));
    }

    private void NavigateToLogin(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowPage(LoginContent, "Account", "Sign in to securely sync your settings.");
    }

    private string? _currentUserToken;
    private string? _currentUserId;

    private async void Login_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        LoginStatusText.Text = "Logging in...";
        LoginStatusText.Foreground = Avalonia.Media.Brushes.LightGray;

        try
        {
            var config = new Firebase.Auth.FirebaseAuthConfig
            {
                ApiKey = Secrets.FirebaseApiKey,
                AuthDomain = "discordmultitool.firebaseapp.com",
                Providers = new Firebase.Auth.Providers.FirebaseAuthProvider[]
                {
                    new Firebase.Auth.Providers.EmailProvider()
                }
            };

            var client = new Firebase.Auth.FirebaseAuthClient(config);
            var userCredential = await client.SignInWithEmailAndPasswordAsync(LoginEmailBox.Text, LoginPasswordBox.Text);
            
            _currentUserToken = await userCredential.User.GetIdTokenAsync();
            _currentUserId = userCredential.User.Uid;

            LoginStatusText.Text = $"Success! Logged in as {userCredential.User.Info.Email}";
            LoginStatusText.Foreground = Avalonia.Media.Brushes.LightGreen;
            
            await LoadCloudConfig();
        }
        catch (Exception ex)
        {
            LoginStatusText.Text = $"Error: {ex.Message}";
            LoginStatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
        }
    }

    private async void Register_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        LoginStatusText.Text = "Registering...";
        LoginStatusText.Foreground = Avalonia.Media.Brushes.LightGray;

        try
        {
            var config = new Firebase.Auth.FirebaseAuthConfig
            {
                ApiKey = Secrets.FirebaseApiKey,
                AuthDomain = "discordmultitool.firebaseapp.com",
                Providers = new Firebase.Auth.Providers.FirebaseAuthProvider[]
                {
                    new Firebase.Auth.Providers.EmailProvider()
                }
            };

            var client = new Firebase.Auth.FirebaseAuthClient(config);
            var userCredential = await client.CreateUserWithEmailAndPasswordAsync(LoginEmailBox.Text, LoginPasswordBox.Text);
            
            _currentUserToken = await userCredential.User.GetIdTokenAsync();
            _currentUserId = userCredential.User.Uid;

            LoginStatusText.Text = $"Success! Registered as {userCredential.User.Info.Email}";
            LoginStatusText.Foreground = Avalonia.Media.Brushes.LightGreen;
            
            await SaveCloudConfig();
        }
        catch (Exception ex)
        {
            LoginStatusText.Text = $"Error: {ex.Message}";
            LoginStatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
        }
    }

    private async void LoginGoogle_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        LoginStatusText.Text = "Opening browser for Google Login...";
        LoginStatusText.Foreground = Avalonia.Media.Brushes.LightGray;

        try
        {
            string clientId = Secrets.GoogleClientId;
            string clientSecret = Secrets.GoogleClientSecret;
            
            if (clientId == "YOUR_GOOGLE_CLIENT_ID" || clientSecret == "YOUR_GOOGLE_CLIENT_SECRET")
            {
                LoginStatusText.Text = "Error: Google Client ID or Secret not configured. Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET environment variables.";
                LoginStatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
                return;
            }

            var credential = await Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                new[] { "email", "profile", "openid" },
                "user",
                System.Threading.CancellationToken.None
            );

            LoginStatusText.Text = "Google Authenticated! Finalizing login...";

            // Get the ID token from Google
            string idToken = credential.Token.IdToken;

            // Connect to Firebase using the ID token via REST to avoid library version issues
            var requestPayload = new
            {
                postBody = $"id_token={idToken}&providerId=google.com",
                requestUri = "http://localhost",
                returnIdpCredential = true,
                returnSecureToken = true
            };
            
            using var httpClient = new HttpClient();
            var jsonPayload = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={Secrets.FirebaseApiKey}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseString);
                string email = jsonDoc.RootElement.GetProperty("email").GetString();
                
                _currentUserToken = jsonDoc.RootElement.GetProperty("idToken").GetString();
                _currentUserId = jsonDoc.RootElement.GetProperty("localId").GetString();

                LoginStatusText.Text = $"Success! Logged in as {email}";
                LoginStatusText.Foreground = Avalonia.Media.Brushes.LightGreen;
                
                await LoadCloudConfig();
            }
            else
            {
                string errorString = await response.Content.ReadAsStringAsync();
                LoginStatusText.Text = $"Error authenticating.";
                LoginStatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
            }
        }
        catch (Exception ex)
        {
            LoginStatusText.Text = $"Error: {ex.Message}";
            LoginStatusText.Foreground = Avalonia.Media.Brushes.IndianRed;
        }
    }

    private async void ShowPage(StackPanel? page, string title, string description)
    {
        if (_isTransitioning || page == _currentPage)
            return;

        _isTransitioning = true;

        await AnimationHelper.FadeOut(PageTitle, 0.2);
        await AnimationHelper.FadeOut(PageDescription, 0.2);

        if (_currentPage != null)
        {
            await AnimationHelper.FadeOut(_currentPage, 0.25);
            _currentPage.IsVisible = false;
        }

        PageTitle.Text = title;
        PageDescription.Text = description;

        await AnimationHelper.FadeIn(PageTitle, 0.25);
        await AnimationHelper.FadeIn(PageDescription, 0.25);

        if (page != null)
        {
            page.IsVisible = true;
            page.Opacity = 0;
            _currentPage = page;
            ContentScroller?.ScrollToHome();

            await AnimationHelper.FadeIn(page, 0.35);

            AnimatePageChildren(page);
        }

        StatusBar.Text = $"{GetString("Status_Viewing")} {title}";

        _isTransitioning = false;
    }

    private async void AnimatePageChildren(StackPanel page)
    {
        if (page == DashboardContent)
        {
            var cards = new[] { DiscordCard, TelegramCard, AppCard };
            foreach (var card in cards)
            {
                if (card != null)
                {
                    // Use a fade-in for cards to avoid a temporary shrink/grow effect
                    card.Opacity = 0;
                    _ = AnimationHelper.FadeIn(card, 0.4);
                    await Task.Delay(80);
                }
            }
        }
    }

    private async void RefreshStatus(object? sender, Avalonia.Interactivity.RoutedEventArgs? e)
    {
        try
        {
            var discord = Process.GetProcessesByName("Discord");
            var telegram = Process.GetProcessesByName("Telegram");
            
            // User prefers to see "1" if the app is open, not the internal process count (e.g. 6)
            int discordAppCount = discord.Length > 0 ? 1 : 0;
            int telegramAppCount = telegram.Length > 0 ? 1 : 0;

            await UpdateStatusTextWithAnimation(DiscordStatusText, discordAppCount > 0 
                ? GetString("Str_Running")
                : GetString("Str_NotRunning"));

            await UpdateStatusTextWithAnimation(TelegramStatusText, telegramAppCount > 0
                ? GetString("Str_Running")
                : GetString("Str_NotRunning"));

            // Update Overlay if open
            UpdateOverlayFunctionStates();

            string run = GetString("Str_Running");
            string off = "Off"; 
            
            StatusBar.Text = $"Discord: {(discordAppCount > 0 ? run : "Off")} | Telegram: {(telegramAppCount > 0 ? run : "Off")}";
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error: {ex.Message}";
        }
    }

    private void ToggleOverlay_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        if (_overlayWindow == null)
        {
            _overlayWindow = new OverlayWindow();
            _overlayWindow.Closed += (s, args) => _overlayWindow = null; 
            
            // Set Position based on selector
            if (OverlayPosSelector?.SelectedItem is ComboBoxItem item && item.Tag is string pos)
            {
                _overlayWindow.SetPosition(pos);
            }

            // Apply Appearance and update function states
            ApplyOverlayAppearance();
            UpdateOverlayFunctionStates();
            
            _overlayWindow.Show();
            
            // Force initial update
            RefreshStatus(null, null);
            
            StatusBar.Text = GetString("Status_OverlayEnabled");
        }
        else
        {
            _overlayWindow.Close();
            _overlayWindow = null;
            StatusBar.Text = GetString("Status_OverlayDisabled");
        }
    }

    private async Task UpdateStatusTextWithAnimation(TextBlock textBlock, string newText)
    {
        if (textBlock.Text != newText)
        {
            await AnimationHelper.FadeOut(textBlock, 0.15);
            textBlock.Text = newText;
            await AnimationHelper.FadeIn(textBlock, 0.25);
        }
    }

    private async void CheckUpdates_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        
        StatusBar.Text = GetString("Status_CheckUpdate");
        
        try
        {
            using (var client = new HttpClient())
            {
                // We want to see where "latest" redirects to
                var response = await client.GetAsync("https://github.com/CodeSharp3210/DiscordMultiTool/releases/latest");
                var finalUrl = response.RequestMessage?.RequestUri?.ToString();

                if (string.IsNullOrEmpty(finalUrl))
                {
                    StatusBar.Text = GetString("Status_UpdateFailed");
                    return;
                }

                // Expected format: .../releases/tag/DMT-v2.0.1 or similar
                // User said: "last part of the address there is written: DMT-(version)"
                // Example: split by '/' get last part
                var tag = finalUrl.Split('/').Last(); // e.g., "DMT-2.0.2"
                
                // Extract version after "DMT-"
                string? versionStr = null;
                if (tag.StartsWith("DMT-"))
                {
                    versionStr = tag.Substring(4); // Remove "DMT-"
                    if (versionStr.StartsWith("v")) versionStr = versionStr.Substring(1); // Remove 'v' if present
                }

                // Current Version
                // Current Version
                var currentVersion = new Version("3.0.0");

                if (versionStr != null && Version.TryParse(versionStr, out var remoteVersion))
                {
                    if (remoteVersion > currentVersion)
                    {
                        StatusBar.Text = $"{GetString("Status_UpdateAvail")}: v{versionStr}";
                        // Open browser
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://github.com/CodeSharp3210/DiscordMultiTool/releases/latest",
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        StatusBar.Text = $"{GetString("Status_UpToDate")} (v3.0.0)";
                    }
                }
                else
                {
                    StatusBar.Text = $"{GetString("Status_UpToDate")} (v3.0.0)";
                }
            }
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"{GetString("Status_UpdateFailed")}: {ex.Message}";
        }
    }

    private void ClearPresence_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn)
            _ = AnimationHelper.PressAnimation(btn);

        try
        {
            if (_rpcClient != null)
            {
                _rpcClient.ClearPresence();
                _rpcClient.Dispose();
                _rpcClient = null;
                StatusBar.Text = GetString("Status_PresenceCleared");
                UpdateOverlayFunctionStates();
            }
            else
            {
                StatusBar.Text = GetString("Status_PresenceNotActive");
            }
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error clearing presence: {ex.Message}";
        }
    }

    private void UpdatePresence_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (AppIdBox == null) return;
        if (sender is Button btn)
            _ = AnimationHelper.PressAnimation(btn);

        // Logic to fix Button URLs
        string FixUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                return "https://" + url;
            }
            return url;
        }

        string appId = AppIdBox.Text ?? "";
        string details = DetailsBox?.Text ?? "";
        string state = StateBox?.Text ?? "";
        
        // Activity Type
        int activityIndex = ActivityTypeSelector?.SelectedIndex ?? 0;
        // Map 0->Playing, 1->Listening, 2->Watching, 3->Competing
        DiscordRPC.ActivityType activityType = DiscordRPC.ActivityType.Playing;
        switch (activityIndex)
        {
            case 1: activityType = DiscordRPC.ActivityType.Listening; break;
            case 2: activityType = DiscordRPC.ActivityType.Watching; break;
            case 3: activityType = DiscordRPC.ActivityType.Competing; break;
            default: activityType = DiscordRPC.ActivityType.Playing; break;
        }

        // Images
        string largeKey = LargeImageKeyBox?.Text ?? "";
        string largeText = LargeImageTextBox?.Text ?? "";
        string smallKey = SmallImageKeyBox?.Text ?? "";
        string smallText = SmallImageTextBox?.Text ?? "";

        // Buttons
        string btn1Label = Btn1LabelBox?.Text ?? "";
        string btn1Url = FixUrl(Btn1UrlBox?.Text ?? "");
        string btn2Label = Btn2LabelBox?.Text ?? "";
        string btn2Url = FixUrl(Btn2UrlBox?.Text ?? "");

        // Party
        string partySizeStr = PartySizeBox?.Text ?? "";
        string partyMaxStr = PartyMaxBox?.Text ?? "";

        bool showTimer = TimestampCheck?.IsChecked == true;

        if (string.IsNullOrWhiteSpace(appId))
        {
            StatusBar.Text = GetString("Status_AppIdRequired"); // Technical error
            return;
        }

        try
        {
            if (_rpcClient == null || _rpcClient.ApplicationID != appId)
            {
                _rpcClient?.Dispose();
                _rpcClient = new DiscordRpcClient(appId);
                _rpcClient.Initialize();
            }

            var rp = new RichPresence()
            {
                Type = activityType,
                Details = string.IsNullOrEmpty(details) ? null : details,
                State = string.IsNullOrEmpty(state) ? null : state,
                Timestamps = showTimer ? Timestamps.Now : null
            };


            // Assets
            if (!string.IsNullOrEmpty(largeKey) || !string.IsNullOrEmpty(smallKey))
            {
                rp.Assets = new Assets()
                {
                    LargeImageKey = string.IsNullOrEmpty(largeKey) ? null : largeKey,
                    LargeImageText = string.IsNullOrEmpty(largeText) ? null : largeText,
                    SmallImageKey = string.IsNullOrEmpty(smallKey) ? null : smallKey,
                    SmallImageText = string.IsNullOrEmpty(smallText) ? null : smallText
                };
            }

            // Buttons
            var buttonsList = new List<DiscordRPC.Button>();
            if (!string.IsNullOrEmpty(btn1Label) && !string.IsNullOrEmpty(btn1Url))
                buttonsList.Add(new DiscordRPC.Button() { Label = btn1Label, Url = btn1Url });

            
            if (!string.IsNullOrEmpty(btn2Label) && !string.IsNullOrEmpty(btn2Url))
                buttonsList.Add(new DiscordRPC.Button() { Label = btn2Label, Url = btn2Url });

            if (buttonsList.Any())
                rp.Buttons = buttonsList.ToArray();

            // Party
            if (int.TryParse(partySizeStr, out int size) && int.TryParse(partyMaxStr, out int max))
            {
                rp.Party = new Party()
                {
                    ID = DiscordRPC.Secrets.CreateFriendlySecret(new Random()),
                    Size = size,
                    Max = max
                };
            }

            _rpcClient.SetPresence(rp);
            StatusBar.Text = $"{GetString("Status_RPCUpdated")} ({activityType})!";
            UpdateOverlayFunctionStates();
        }
        catch (Exception ex)
        {
            StatusBar.Text = string.Format(GetString("Status_RPCError"), ex.Message);
        }
    }

    private void RefreshProcesses_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn)
             _ = AnimationHelper.PressAnimation(btn);
        LoadProcesses();
    }

    private void LoadProcesses()
    {
        if (ProcessListBox == null) return;

        try
        {
            var processes = Process.GetProcesses()
                                   .Where(p => p.MainWindowHandle != IntPtr.Zero)
                                   .OrderBy(p => p.ProcessName)
                                   .Select(p => $"{p.ProcessName} (ID: {p.Id})")
                                   .ToList();
            ProcessListBox.ItemsSource = processes;
            StatusBar.Text = string.Format(GetString("Status_LoadedProcs"), processes.Count);
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error listing processes: {ex.Message}";
        }
    }

    private void KillProcess_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ProcessListBox?.SelectedItem is string selected && selected.Contains("(ID: "))
        {
            if (sender is Button btn)
                _ = AnimationHelper.PressAnimation(btn);

            try
            {
                // Extract ID from format "Name (ID: 1234)"
                var idStr = selected.Split(new[] { "(ID: ", ")" }, StringSplitOptions.RemoveEmptyEntries).Last();
                if (int.TryParse(idStr, out int pid))
                {
                    var process = Process.GetProcessById(pid);
                    process.Kill();
                    StatusBar.Text = $"{GetString("Status_Killed")} {process.ProcessName} ({pid})";
                    LoadProcesses(); // Refresh list
                }
            }
            catch (Exception ex)
            {
                StatusBar.Text = $"{GetString("Status_KillFail")}: {ex.Message}";
            }
        }
        else
            StatusBar.Text = GetString("Status_NoProc");
    }

    private async Task PlayIntroAnimation()
    {
        try
        {
            // 1. Fade In Logo (Starts at X = 150)
            if (IntroLogo != null) await AnimationHelper.FadeIn(IntroLogo, 0.6);
            
            await Task.Delay(200);

            // 2. Move Logo to Left (X = 0) and Fade In Text Panel
            var moveTask = AnimationHelper.MoveHorizontal(IntroLogo, 150, 0, 0.8);
            var fadeTask = AnimationHelper.FadeIn(IntroTextPanel, 0.8);

            // Init Texts
            if (IntroDiscordText != null) IntroDiscordText.Text = GetString("Str_Intro_Discord");
            if (IntroTelegramText != null) IntroTelegramText.Text = GetString("Str_Intro_Telegram");

            await Task.WhenAll(moveTask, fadeTask);

            // 3. Checking Discord
            await Task.Delay(1000);
            
            bool discordRunning = Process.GetProcessesByName("Discord").Length > 0;
            if (IntroDiscordFound != null)
            {
                IntroDiscordFound.Text = discordRunning ? GetString("Str_Found") : GetString("Str_Stats_NotRunning");
                IntroDiscordFound.Foreground = new SolidColorBrush(discordRunning ? Color.Parse("#10B981") : Colors.Red);
                await AnimationHelper.FadeIn(IntroDiscordFound, 0.4);
            }
            
            await Task.Delay(500);

            // 4. Checking Telegram
            if (IntroTelegramText != null) await AnimationHelper.FadeIn(IntroTelegramText, 0.4);
            
            await Task.Delay(1000);

            bool telegramRunning = Process.GetProcessesByName("Telegram").Length > 0;
            if (IntroTelegramFound != null)
            {
                IntroTelegramFound.Text = telegramRunning ? GetString("Str_Found") : GetString("Str_Stats_NotRunning");
                IntroTelegramFound.Foreground = new SolidColorBrush(telegramRunning ? Color.Parse("#10B981") : Colors.Red);
                await AnimationHelper.FadeIn(IntroTelegramFound, 0.4);
            }
            
            await Task.Delay(500);

            // 5. Done
            await Task.Delay(500);
            StatusBar.Text = "Ready";
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex);
        }
        finally
        {
            // Fade out overlay
            if (IntroOverlay != null)
            {
                await AnimationHelper.FadeOut(IntroOverlay, 0.8);
                IntroOverlay.IsVisible = false;
                
                // Refresh dashboard status now that intro is done
                RefreshStatus(null, null);
            }
            Topmost = false;
        }
    }
    // Real-Time Updates & Color Sync
    private bool _isUpdating = false;
    private Color _bgColor = Color.Parse("#BF0f172a");
    private Color _borderColor = Color.Parse("#334155");
    private Color _textColor = Colors.White;

    private void UpdateColorSliders(Color color)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        
        if (RedSlider != null) RedSlider.Value = color.R;
        if (GreenSlider != null) GreenSlider.Value = color.G;
        if (BlueSlider != null) BlueSlider.Value = color.B;
        if (AlphaSlider != null) AlphaSlider.Value = color.A;
        
        UpdateValueLabels();
        _isUpdating = false;
    }

    private void UpdateValueLabels()
    {
        if (RedValue != null) RedValue.Text = ((byte)(RedSlider?.Value ?? 0)).ToString();
        if (GreenValue != null) GreenValue.Text = ((byte)(GreenSlider?.Value ?? 0)).ToString();
        if (BlueValue != null) BlueValue.Text = ((byte)(BlueSlider?.Value ?? 0)).ToString();
        if (AlphaValue != null) AlphaValue.Text = ((byte)(AlphaSlider?.Value ?? 255)).ToString();
        
        UpdateColorPreview();
        UpdateHexDisplay();
    }

    private void UpdateHexDisplay()
    {
        if (HexColorInput != null && !_isUpdating)
        {
            byte r = (byte)(RedSlider?.Value ?? 0);
            byte g = (byte)(GreenSlider?.Value ?? 0);
            byte b = (byte)(BlueSlider?.Value ?? 0);
            byte a = (byte)(AlphaSlider?.Value ?? 255);
            
            string hex = a < 255 ? $"#{a:X2}{r:X2}{g:X2}{b:X2}" : $"#{r:X2}{g:X2}{b:X2}";
            HexColorInput.Text = hex;
        }
    }

    private void UpdateColorPreview()
    {
        if (ColorPreviewBorder != null)
        {
            byte r = (byte)(RedSlider?.Value ?? 0);
            byte g = (byte)(GreenSlider?.Value ?? 0);
            byte b = (byte)(BlueSlider?.Value ?? 0);
            byte a = (byte)(AlphaSlider?.Value ?? 255);
            Color previewColor = Color.FromArgb(a, r, g, b);
            ColorPreviewBorder.Background = new SolidColorBrush(previewColor);
        }
    }

    private void OnColorTargetChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating) return;
        
        if (ColorTargetSelector?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            Color targetColor = tag switch
            {
                "bg" => _bgColor,
                "border" => _borderColor,
                "text" => _textColor,
                _ => _bgColor
            };
            UpdateColorSliders(targetColor);
        }
    }

    private void OnColorSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdating) return;
        
        UpdateValueLabels();
        
        byte r = (byte)(RedSlider?.Value ?? 0);
        byte g = (byte)(GreenSlider?.Value ?? 0);
        byte b = (byte)(BlueSlider?.Value ?? 0);
        byte a = (byte)(AlphaSlider?.Value ?? 255);
        
        Color newColor = Color.FromArgb(a, r, g, b);

        if (ColorTargetSelector?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "bg": _bgColor = newColor; break;
                case "border": _borderColor = newColor; break;
                case "text": _textColor = newColor; break;
            }
            ApplyOverlayAppearance();
        }
    }

    private void OnHexInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdating || HexColorInput?.Text == null) return;

        string hex = HexColorInput.Text.Trim();
        if (string.IsNullOrEmpty(hex)) return;

        if (Color.TryParse(hex, out Color parsedColor))
        {
            _isUpdating = true;
            
            if (RedSlider != null) RedSlider.Value = parsedColor.R;
            if (GreenSlider != null) GreenSlider.Value = parsedColor.G;
            if (BlueSlider != null) BlueSlider.Value = parsedColor.B;
            if (AlphaSlider != null) AlphaSlider.Value = parsedColor.A;
            
            UpdateValueLabels();
            
            Color newColor = parsedColor;
            if (ColorTargetSelector?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                switch (tag)
                {
                    case "bg": _bgColor = newColor; break;
                    case "border": _borderColor = newColor; break;
                    case "text": _textColor = newColor; break;
                }
                ApplyOverlayAppearance();
            }
            
            _isUpdating = false;
        }
    }

    private void OnRealTimeSettingChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isUpdating) return;
        ApplyOverlayAppearance();
    }

    private void OnSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdating) return;
        ApplyOverlayAppearance();
    }

    private void ApplyOverlayAppearance()
    {
        if (_overlayWindow == null) return;

        bool rainbow = OverlayRainbowSwitch?.IsChecked ?? false;
        bool showBorder = OverlayBorderSwitch?.IsChecked ?? true;
        double bgOp = OverlayBgOpacitySlider?.Value ?? 0.75;
        double masterOp = OverlayMasterOpacitySlider?.Value ?? 1.0;

        _overlayWindow.SetAppearance(_bgColor, _borderColor, _textColor, rainbow, showBorder, bgOp, masterOp);
        UpdateOverlayFunctionStates();
    }

    private void UpdateOverlayFunctionStates()
    {
        if (_overlayWindow == null) return;

        bool rpcActive = _rpcClient != null && _rpcClient.IsInitialized;
        _overlayWindow.UpdateFunctionStates(_discordBotActive, _telegramBotActive, rpcActive, _textColor);
    }

    private async Task LoadCloudConfig()
    {
        if (string.IsNullOrEmpty(_currentUserToken) || string.IsNullOrEmpty(_currentUserId)) return;

        try
        {
            using var httpClient = new HttpClient();
            // Try fetching from the default Realtime Database URL
            string dbUrl = $"https://discordmultitool-default-rtdb.europe-west1.firebasedatabase.app/users/{_currentUserId}/config.json?auth={_currentUserToken}";
            
            var response = await httpClient.GetAsync(dbUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                if (json != "null" && !string.IsNullOrEmpty(json))
                {
                    // It exists! Load it into the app.
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                        try {
                            var config = JsonSerializer.Deserialize<AppConfig>(json);
                            if (config != null)
                            {
                                ApplyConfigToUI(config);
                            }
                        } catch {}
                    });
                    LoginStatusText.Text += "\nSettings downloaded & synced!";
                }
                else
                {
                    // No config found, save the current local one
                    await SaveCloudConfig();
                }
            }
            else
            {
                // Fallback to the US URL if EU isn't working
                dbUrl = $"https://discordmultitool-default-rtdb.firebaseio.com/users/{_currentUserId}/config.json?auth={_currentUserToken}";
                response = await httpClient.GetAsync(dbUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (json != "null" && !string.IsNullOrEmpty(json))
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                            try {
                                var config = JsonSerializer.Deserialize<AppConfig>(json);
                                if (config != null)
                                {
                                    ApplyConfigToUI(config);
                                }
                            } catch {}
                        });
                        LoginStatusText.Text += "\nSettings downloaded & synced!";
                    }
                    else
                    {
                        await SaveCloudConfig();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cloud config: {ex.Message}");
        }
    }

    private async Task SaveCloudConfig()
    {
        if (string.IsNullOrEmpty(_currentUserToken) || string.IsNullOrEmpty(_currentUserId)) return;

        try
        {
            var config = GetCurrentAppConfig();
            string json = JsonSerializer.Serialize(config);
            using var httpClient = new HttpClient();
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            // Try EU Database first
            string dbUrl = $"https://discordmultitool-default-rtdb.europe-west1.firebasedatabase.app/users/{_currentUserId}/config.json?auth={_currentUserToken}";
            var response = await httpClient.PutAsync(dbUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                // Try US Database
                dbUrl = $"https://discordmultitool-default-rtdb.firebaseio.com/users/{_currentUserId}/config.json?auth={_currentUserToken}";
                await httpClient.PutAsync(dbUrl, content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cloud config: {ex.Message}");
        }
    }
}

// Helper extension for ListBox refresh if needed, or just reassign ItemsSource
public static class ListBoxExtensions
{
    public static void Refresh(this ListBox listBox)
    {
        var items = listBox.ItemsSource;
        listBox.ItemsSource = null;
        listBox.ItemsSource = items;
    }
}

