using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DiscordMultiTool.Animations;
using DiscordMultiTool.Models;
using DiscordMultiTool.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMultiTool;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly LocalizationService _localization = new();
    private readonly RichPresenceService _rpcService = new();
    private readonly FirebaseAuthService _authService = new();
    private readonly BotRunnerService _discordBotRunner = new("Discord Bot", "DISCORD_TOKEN");
    private readonly BotRunnerService _telegramBotRunner = new("Telegram Bot", "TELEGRAM_TOKEN");

    private readonly StringBuilder _discordLogBuffer = new();
    private readonly StringBuilder _telegramLogBuffer = new();
    private const int MaxLogLength = 24000;

    private List<ProcessInfoItem> _allProcesses = new();
    private Control? _currentPage;
    private Button[] _navButtons = Array.Empty<Button>();
    private OverlayWindow? _overlayWindow;
    private TrayIcon? _trayIcon;
    private bool _isTransitioning;
    private bool _isClosing;
    private bool _isUpdating;

    private Color _bgColor = Color.Parse("#D90B0D13");
    private Color _borderColor = Color.Parse("#323B54");
    private Color _textColor = Colors.White;
    private readonly SolidColorBrush _colorPreviewBrush = new(Color.Parse("#D90B0D13"));

    private static readonly SolidColorBrush OnlineBrush = new(Color.Parse("#23A55A"));
    private static readonly SolidColorBrush OnlineSoftBrush = new(Color.Parse("#2223A55A"));
    private static readonly SolidColorBrush AccentBrushStatic = new(Color.Parse("#5865F2"));
    private static readonly SolidColorBrush DangerBrushStatic = new(Color.Parse("#F23F43"));
    private static readonly SolidColorBrush MutedBrushStatic = new(Color.Parse("#5E6778"));
    private static readonly SolidColorBrush MutedTextBrushStatic = new(Color.Parse("#949BA4"));
    private static readonly SolidColorBrush NeutralPillBrush = new(Color.Parse("#1B2030"));

    public MainWindow()
    {
        _isUpdating = true;
        InitializeComponent();
        _isUpdating = false;

        _navButtons = new[]
        {
            NavDashboardBtn,
            NavRpcBtn,
            NavDiscordBotBtn,
            NavTelegramBotBtn,
            NavProcessBtn,
            NavSettingsBtn,
            NavAccountBtn
        };

        string ver = UpdateService.CurrentVersionString;
        Title = $"DiscordMultiTool v{ver}";
        SidebarVersionText.Text = $"v{ver} • Studio";
        AppVersionText.Text = $"v{ver}";
        StatusRightSummary.Text = $"v{ver} • Ready";
        ColorPreviewBorder.Background = _colorPreviewBrush;

        _currentPage = DashboardContent;
        _localization.ApplyLanguage("en");
        ApplyConfigToUI(_configService.Load());
        RefreshRpcPreview();
        UpdateModuleTelemetryBadges();

        WireBotRunnerEvents();
        InitializeTrayIcon();
        Closing += OnWindowClosing;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Activate();
        _ = PlayIntroAnimation();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    #region Tray & Lifecycle

    private void InitializeTrayIcon()
    {
        _trayIcon = new TrayIcon
        {
            Icon = Icon,
            ToolTipText = "DiscordMultiTool"
        };

        var menu = new NativeMenu();

        var showItem = new NativeMenuItem { Header = "Show Studio" };
        showItem.Click += (_, _) => RestoreWindow();
        menu.Items.Add(showItem);

        var exitItem = new NativeMenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += (_, _) => RestoreWindow();
        _trayIcon.IsVisible = true;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosing || !_configService.Current.MinimizeToTrayOnClose)
        {
            CleanupResources();
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void CleanupResources()
    {
        _discordBotRunner.Dispose();
        _telegramBotRunner.Dispose();
        _rpcService.Dispose();
        _overlayWindow?.Close();
        _overlayWindow = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
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

    #endregion

    #region Config & Localization

    private void ApplyConfigToUI(AppConfig config)
    {
        _isUpdating = true;
        try
        {
            if (!string.IsNullOrEmpty(config.Language))
            {
                _localization.ApplyLanguage(config.Language);
                SelectComboBoxItemByTag(LanguageChoice, _localization.CurrentLanguage);
            }

            AutoStartSwitch.IsChecked = config.AutoStart;
            SelectComboBoxItemByTag(OverlayPosSelector, config.OverlayPosition);

            AppIdBox.Text = config.AppId;
            DetailsBox.Text = config.Details;
            StateBox.Text = config.State;
            TimestampCheck.IsChecked = config.ShowTimestamp;
            ActivityTypeSelector.SelectedIndex = Math.Clamp(config.ActivityType, 0, 3);

            LargeImageKeyBox.Text = config.LargeImageKey;
            LargeImageTextBox.Text = config.LargeImageText;
            SmallImageKeyBox.Text = config.SmallImageKey;
            SmallImageTextBox.Text = config.SmallImageText;

            Btn1LabelBox.Text = config.Btn1Label;
            Btn1UrlBox.Text = config.Btn1Url;
            Btn2LabelBox.Text = config.Btn2Label;
            Btn2UrlBox.Text = config.Btn2Url;

            PartySizeBox.Text = config.PartySize;
            PartyMaxBox.Text = config.PartyMax;

            DiscordBotTokenBox.Text = config.DiscordBotToken;
            DiscordBotScriptBox.Text = config.DiscordBotScriptPath;
            DiscordBotPythonBox.Text = string.IsNullOrWhiteSpace(config.DiscordBotPythonPath) ? "python" : config.DiscordBotPythonPath;

            TelegramBotTokenBox.Text = config.TelegramBotToken;
            TelegramBotScriptBox.Text = config.TelegramBotScriptPath;
            TelegramBotPythonBox.Text = string.IsNullOrWhiteSpace(config.TelegramBotPythonPath) ? "python" : config.TelegramBotPythonPath;

            if (Color.TryParse(config.OverlayBackgroundColor, out Color bgColor)) _bgColor = bgColor;
            if (Color.TryParse(config.OverlayBorderColor, out Color borderColor)) _borderColor = borderColor;
            if (Color.TryParse(config.OverlayTextColor, out Color textColor)) _textColor = textColor;

            SetColorSlidersInternal(_bgColor);
            OverlayRainbowSwitch.IsChecked = config.OverlayRainbowMode;
            OverlayBorderSwitch.IsChecked = config.OverlayShowBorder;
            OverlayBgOpacitySlider.Value = config.OverlayBgOpacity;
            OverlayMasterOpacitySlider.Value = config.OverlayMasterOpacity;

            CloseButtonBehaviorSelector.SelectedIndex = config.MinimizeToTrayOnClose ? 0 : 1;
        }
        finally
        {
            _isUpdating = false;
        }

        UpdateValueLabels(updateHex: true);
        RefreshRpcPreview();
        UpdatePageHeader();
    }

    private AppConfig ReadConfigFromUI()
    {
        return new AppConfig
        {
            Language = GetSelectedTag(LanguageChoice) ?? _localization.CurrentLanguage,
            OverlayPosition = GetSelectedTag(OverlayPosSelector) ?? "Top Left",
            AutoStart = AutoStartSwitch.IsChecked ?? false,

            AppId = AppIdBox.Text ?? "",
            Details = DetailsBox.Text ?? "",
            State = StateBox.Text ?? "",
            ShowTimestamp = TimestampCheck.IsChecked ?? false,
            ActivityType = Math.Max(0, ActivityTypeSelector.SelectedIndex),

            LargeImageKey = LargeImageKeyBox.Text ?? "",
            LargeImageText = LargeImageTextBox.Text ?? "",
            SmallImageKey = SmallImageKeyBox.Text ?? "",
            SmallImageText = SmallImageTextBox.Text ?? "",

            Btn1Label = Btn1LabelBox.Text ?? "",
            Btn1Url = Btn1UrlBox.Text ?? "",
            Btn2Label = Btn2LabelBox.Text ?? "",
            Btn2Url = Btn2UrlBox.Text ?? "",

            PartySize = PartySizeBox.Text ?? "",
            PartyMax = PartyMaxBox.Text ?? "",

            DiscordBotToken = DiscordBotTokenBox.Text ?? "",
            DiscordBotScriptPath = DiscordBotScriptBox.Text ?? "",
            DiscordBotPythonPath = string.IsNullOrWhiteSpace(DiscordBotPythonBox.Text) ? "python" : DiscordBotPythonBox.Text.Trim(),

            TelegramBotToken = TelegramBotTokenBox.Text ?? "",
            TelegramBotScriptPath = TelegramBotScriptBox.Text ?? "",
            TelegramBotPythonPath = string.IsNullOrWhiteSpace(TelegramBotPythonBox.Text) ? "python" : TelegramBotPythonBox.Text.Trim(),

            OverlayBackgroundColor = _bgColor.ToString(),
            OverlayBorderColor = _borderColor.ToString(),
            OverlayTextColor = _textColor.ToString(),
            OverlayRainbowMode = OverlayRainbowSwitch.IsChecked ?? false,
            OverlayShowBorder = OverlayBorderSwitch.IsChecked ?? true,
            OverlayBgOpacity = OverlayBgOpacitySlider.Value,
            OverlayMasterOpacity = OverlayMasterOpacitySlider.Value,

            MinimizeToTrayOnClose = (GetSelectedTag(CloseButtonBehaviorSelector) ?? "minimize") == "minimize"
        };
    }

    private void SaveSettings(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        try
        {
            var config = ReadConfigFromUI();
            _configService.Save(config);
            AutoStartService.SetEnabled(config.AutoStart);
            ApplyOverlayAppearance();

            StatusBar.Text = _localization.GetString("Status_Saved");

            if (_authService.IsAuthenticated)
            {
                _ = _authService.SaveCloudConfigAsync(config);
            }
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error saving: {ex.Message}";
        }
    }

    private void LanguageChoice_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating) return;
        string? langCode = GetSelectedTag(LanguageChoice);
        if (string.IsNullOrEmpty(langCode) || langCode == _localization.CurrentLanguage) return;

        _localization.ApplyLanguage(langCode);
        _ = RefreshStatusAsync();
        StatusBar.Text = _localization.GetString("Status_Ready");
        UpdatePageHeader();
        UpdateBotStatusLabels();
        RefreshRpcPreview();
        if (!_authService.IsAuthenticated)
        {
            SidebarAccountEmailText.Text = _localization.GetString("Str_NotSignedIn");
        }
    }

    private static void SelectComboBoxItemByTag(ComboBox comboBox, string targetTag)
    {
        foreach (var obj in comboBox.Items)
        {
            if (obj is ComboBoxItem item && item.Tag is string tag && tag == targetTag)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static string? GetSelectedTag(ComboBox? comboBox)
    {
        return comboBox?.SelectedItem is ComboBoxItem item ? item.Tag as string : null;
    }

    #endregion

    #region Navigation & Active State

    private void SetActiveNavButton(Button active)
    {
        foreach (var btn in _navButtons)
        {
            btn.Classes.Set("Active", ReferenceEquals(btn, active));
        }
    }

    private void UpdatePageHeader()
    {
        (string titleKey, string descKey) = _currentPage switch
        {
            var p when p == RichPresenceContent => ("Str_RichPresence", "Str_Header_RichPresence"),
            var p when p == DiscordBotContent => ("Str_DiscordBot", "Str_Header_DiscordBot"),
            var p when p == TelegramBotContent => ("Str_TelegramBot", "Str_Header_TelegramBot"),
            var p when p == ProcessManagerContent => ("Str_ProcessManager", "Str_Header_ProcessManager"),
            var p when p == SettingsContent => ("Str_Settings", "Str_Header_Settings"),
            var p when p == LoginContent => ("Str_AccountLogin", "Str_Header_Account"),
            _ => ("Str_Dashboard", "Str_Header_Dashboard")
        };

        string title = _localization.GetString(titleKey);
        PageTitle.Text = title;
        BreadcrumbText.Text = title;
        PageDescription.Text = _localization.GetString(descKey);
    }

    private void NavigateToDashboard(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavDashboardBtn);
        _ = ShowPageAsync(DashboardContent, _localization.GetString("Str_Dashboard"), _localization.GetString("Str_Header_Dashboard"));
    }

    private void NavigateToRichPresence(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavRpcBtn);
        _ = ShowPageAsync(RichPresenceContent, _localization.GetString("Str_RichPresence"), _localization.GetString("Str_Header_RichPresence"));
    }

    private void NavigateToDiscordBot(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavDiscordBotBtn);
        _ = ShowPageAsync(DiscordBotContent, _localization.GetString("Str_DiscordBot"), _localization.GetString("Str_Header_DiscordBot"));
    }

    private void NavigateToTelegramBot(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavTelegramBotBtn);
        _ = ShowPageAsync(TelegramBotContent, _localization.GetString("Str_TelegramBot"), _localization.GetString("Str_Header_TelegramBot"));
    }

    private void NavigateToProcessManager(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavProcessBtn);
        _ = ShowPageAsync(ProcessManagerContent, _localization.GetString("Str_ProcessManager"), _localization.GetString("Str_Header_ProcessManager"));
        _ = LoadProcessesAsync();
    }

    private void NavigateToSettings(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavSettingsBtn);
        _ = ShowPageAsync(SettingsContent, _localization.GetString("Str_Settings"), _localization.GetString("Str_Header_Settings"));
    }

    private void NavigateToLogin(object? sender, RoutedEventArgs e)
    {
        SetActiveNavButton(NavAccountBtn);
        _ = ShowPageAsync(LoginContent, _localization.GetString("Str_AccountLogin"), _localization.GetString("Str_Header_Account"));
    }

    private async Task ShowPageAsync(Control page, string title, string description)
    {
        if (_isTransitioning || page == _currentPage)
            return;

        _isTransitioning = true;
        try
        {
            if (_currentPage != null)
            {
                await AnimationHelper.FadeOut(_currentPage, 0.12);
                _currentPage.IsVisible = false;
            }

            PageTitle.Text = title;
            BreadcrumbText.Text = title;
            PageDescription.Text = description;

            page.IsVisible = true;
            page.Opacity = 0;
            _currentPage = page;
            ContentScroller.ScrollToHome();

            await AnimationHelper.FadeIn(page, 0.18);
            StatusBar.Text = $"{_localization.GetString("Status_Viewing")} {title}";
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    #endregion

    #region Dashboard, Telemetry & Intro

    private void UpdateModuleTelemetryBadges()
    {
        bool rpcActive = _rpcService.IsActive;
        bool hudActive = _overlayWindow != null;
        bool discordBot = _discordBotRunner.IsRunning;
        bool telegramBot = _telegramBotRunner.IsRunning;

        NavRpcDot.IsVisible = rpcActive;
        NavDiscordBotDot.IsVisible = discordBot;
        NavTelegramBotDot.IsVisible = telegramBot;
        NavOverlayDot.IsVisible = hudActive;

        HeaderRpcDot.Fill = rpcActive ? OnlineBrush : MutedBrushStatic;
        HeaderRpcText.Text = rpcActive ? "RPC Active" : "RPC Idle";
        HeaderRpcText.Foreground = rpcActive ? Brushes.White : MutedTextBrushStatic;

        HeaderHudDot.Fill = hudActive ? AccentBrushStatic : MutedBrushStatic;
        HeaderHudText.Text = hudActive ? "HUD Live" : "HUD Off";
        HeaderHudText.Foreground = hudActive ? Brushes.White : MutedTextBrushStatic;

        int activeBots = (discordBot ? 1 : 0) + (telegramBot ? 1 : 0);
        StatusRightSummary.Text = $"v{UpdateService.CurrentVersionString} • RPC {(rpcActive ? "ON" : "OFF")} • Bots {activeBots}";
    }

    private void RefreshStatus(object? sender, RoutedEventArgs? e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        _ = RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            var discordTask = ProcessService.IsProcessRunningAsync("Discord");
            var telegramTask = ProcessService.IsProcessRunningAsync("Telegram");
            await Task.WhenAll(discordTask, telegramTask);

            bool discordRunning = discordTask.Result;
            bool telegramRunning = telegramTask.Result;

            string running = _localization.GetString("Str_Running");
            string offline = _localization.GetString("Str_NotRunning");

            DiscordStatusText.Text = discordRunning ? running : offline;
            DiscordStatusText.Foreground = discordRunning ? OnlineBrush : MutedTextBrushStatic;
            DiscordStatusPill.Background = discordRunning ? OnlineSoftBrush : NeutralPillBrush;

            TelegramStatusText.Text = telegramRunning ? running : offline;
            TelegramStatusText.Foreground = telegramRunning ? OnlineBrush : MutedTextBrushStatic;
            TelegramStatusPill.Background = telegramRunning ? OnlineSoftBrush : NeutralPillBrush;

            UpdateOverlayFunctionStates();
            UpdateModuleTelemetryBadges();
            StatusBar.Text = $"Discord: {(discordRunning ? running : offline)}  •  Telegram: {(telegramRunning ? running : offline)}";
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error: {ex.Message}";
        }
    }

    private async void CheckUpdates_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        StatusBar.Text = _localization.GetString("Status_CheckUpdate");

        var result = await UpdateService.CheckForUpdatesAsync();
        if (!result.Success)
        {
            StatusBar.Text = $"{_localization.GetString("Status_UpdateFailed")}: {result.ErrorMessage}";
            return;
        }

        if (result.UpdateAvailable && !string.IsNullOrEmpty(result.LatestVersion))
        {
            StatusBar.Text = $"{_localization.GetString("Status_UpdateAvail")}: v{result.LatestVersion}";
            UpdateService.OpenReleasesPage();
        }
        else
        {
            StatusBar.Text = $"{_localization.GetString("Status_UpToDate")} (v{result.CurrentVersion})";
        }
    }

    private async Task PlayIntroAnimation()
    {
        try
        {
            var discordCheckTask = ProcessService.IsProcessRunningAsync("Discord");
            var telegramCheckTask = ProcessService.IsProcessRunningAsync("Telegram");

            IntroDiscordText.Text = _localization.GetString("Str_Intro_Discord");
            IntroTelegramText.Text = _localization.GetString("Str_Intro_Telegram");

            await AnimationHelper.FadeIn(IntroLogo, 0.22);

            await Task.WhenAll(
                AnimationHelper.MoveHorizontal(IntroLogo, 80, 0, 0.3),
                AnimationHelper.FadeIn(IntroTextPanel, 0.3));

            bool discordRunning = await discordCheckTask;
            IntroDiscordFound.Text = discordRunning ? _localization.GetString("Str_Found") : _localization.GetString("Str_Stats_NotRunning");
            IntroDiscordFound.Foreground = discordRunning ? OnlineBrush : MutedTextBrushStatic;

            bool telegramRunning = await telegramCheckTask;
            IntroTelegramFound.Text = telegramRunning ? _localization.GetString("Str_Found") : _localization.GetString("Str_Stats_NotRunning");
            IntroTelegramFound.Foreground = telegramRunning ? OnlineBrush : MutedTextBrushStatic;

            await Task.WhenAll(
                AnimationHelper.FadeIn(IntroDiscordFound, 0.18),
                AnimationHelper.FadeIn(IntroTelegramText, 0.18),
                AnimationHelper.FadeIn(IntroTelegramFound, 0.2));

            await Task.Delay(200);
            StatusBar.Text = _localization.GetString("Status_Ready");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            await AnimationHelper.FadeOut(IntroOverlay, 0.25);
            IntroOverlay.IsVisible = false;
            _ = RefreshStatusAsync();
        }
    }

    #endregion

    #region Rich Presence & Live Preview

    private void OnRpcPreviewChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        RefreshRpcPreview();
    }

    private void OnRpcActivityTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating) return;
        RefreshRpcPreview();
    }

    private void OnRpcTimestampChanged(object? sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        RefreshRpcPreview();
    }

    private void RefreshRpcPreview()
    {
        PreviewActivityHeader.Text = ActivityTypeSelector.SelectedIndex switch
        {
            1 => "LISTENING TO",
            2 => "WATCHING",
            3 => "COMPETING IN",
            _ => "PLAYING A GAME"
        };

        string appId = AppIdBox.Text?.Trim() ?? "";
        PreviewAppNameText.Text = string.IsNullOrEmpty(appId) ? "DiscordMultiTool" : $"App #{appId}";

        string details = DetailsBox.Text?.Trim() ?? "";
        PreviewDetailsText.Text = string.IsNullOrEmpty(details) ? "Custom Rich Presence" : details;

        string state = StateBox.Text?.Trim() ?? "";
        string partySuffix = "";
        if (int.TryParse(PartySizeBox.Text, out int pSize) && int.TryParse(PartyMaxBox.Text, out int pMax) && pSize > 0 && pMax >= pSize)
        {
            partySuffix = $" ({pSize} of {pMax})";
        }
        PreviewStateText.Text = (string.IsNullOrEmpty(state) ? "Idle" : state) + partySuffix;

        PreviewTimerText.IsVisible = TimestampCheck.IsChecked == true;

        string largeKey = LargeImageKeyBox.Text?.Trim() ?? "";
        PreviewAssetKeyText.Text = string.IsNullOrEmpty(largeKey) ? "app-logo" : largeKey;
        PreviewSmallAssetBadge.IsVisible = !string.IsNullOrWhiteSpace(SmallImageKeyBox.Text);

        string b1 = Btn1LabelBox.Text?.Trim() ?? "";
        PreviewBtn1Border.IsVisible = !string.IsNullOrEmpty(b1);
        PreviewBtn1Text.Text = b1;

        string b2 = Btn2LabelBox.Text?.Trim() ?? "";
        PreviewBtn2Border.IsVisible = !string.IsNullOrEmpty(b2);
        PreviewBtn2Text.Text = b2;
    }

    private void UpdatePresence_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        string appId = AppIdBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(appId))
        {
            StatusBar.Text = _localization.GetString("Status_AppIdRequired");
            return;
        }

        try
        {
            var activityType = _rpcService.UpdatePresence(
                appId,
                DetailsBox.Text ?? "",
                StateBox.Text ?? "",
                ActivityTypeSelector.SelectedIndex,
                TimestampCheck.IsChecked == true,
                LargeImageKeyBox.Text ?? "",
                LargeImageTextBox.Text ?? "",
                SmallImageKeyBox.Text ?? "",
                SmallImageTextBox.Text ?? "",
                Btn1LabelBox.Text ?? "",
                Btn1UrlBox.Text ?? "",
                Btn2LabelBox.Text ?? "",
                Btn2UrlBox.Text ?? "",
                PartySizeBox.Text ?? "",
                PartyMaxBox.Text ?? "");

            StatusBar.Text = $"{_localization.GetString("Status_RPCUpdated")} ({activityType})";
            UpdateOverlayFunctionStates();
            UpdateModuleTelemetryBadges();
        }
        catch (Exception ex)
        {
            StatusBar.Text = string.Format(_localization.GetString("Status_RPCError"), ex.Message);
        }
    }

    private void ClearPresence_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        try
        {
            bool cleared = _rpcService.Clear();
            StatusBar.Text = _localization.GetString(cleared ? "Status_PresenceCleared" : "Status_PresenceNotActive");
            UpdateOverlayFunctionStates();
            UpdateModuleTelemetryBadges();
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error clearing presence: {ex.Message}";
        }
    }

    #endregion

    #region Bot Runners

    private void WireBotRunnerEvents()
    {
        _discordBotRunner.LogReceived += line => Dispatcher.UIThread.Post(() => AppendLog(_discordLogBuffer, DiscordBotLogBox, line));
        _discordBotRunner.StateChanged += (running, _) => Dispatcher.UIThread.Post(() =>
        {
            UpdateBotStatusLabels();
            UpdateOverlayFunctionStates();
            UpdateModuleTelemetryBadges();
            if (!running)
                StatusBar.Text = string.Format(_localization.GetString("Status_BotStopped"), "Discord Bot");
        });

        _telegramBotRunner.LogReceived += line => Dispatcher.UIThread.Post(() => AppendLog(_telegramLogBuffer, TelegramBotLogBox, line));
        _telegramBotRunner.StateChanged += (running, _) => Dispatcher.UIThread.Post(() =>
        {
            UpdateBotStatusLabels();
            UpdateOverlayFunctionStates();
            UpdateModuleTelemetryBadges();
            if (!running)
                StatusBar.Text = string.Format(_localization.GetString("Status_BotStopped"), "Telegram Bot");
        });
    }

    private void UpdateBotStatusLabels()
    {
        if (_discordBotRunner.IsRunning)
        {
            DiscordBotStatusText.Text = string.Format(_localization.GetString("Str_BotRunning"), _discordBotRunner.ProcessId);
            DiscordBotStatusText.Foreground = OnlineBrush;
            DiscordBotBadge.Background = OnlineSoftBrush;
            DiscordBotToggleBtn.Content = _localization.GetString("Str_StopBot");
            DiscordBotToggleBtn.Classes.Set("PrimaryButton", false);
            DiscordBotToggleBtn.Classes.Set("DangerButton", true);
        }
        else
        {
            DiscordBotStatusText.Text = _localization.GetString("Str_BotStopped");
            DiscordBotStatusText.Foreground = MutedTextBrushStatic;
            DiscordBotBadge.Background = NeutralPillBrush;
            DiscordBotToggleBtn.Content = _localization.GetString("Str_LaunchBot");
            DiscordBotToggleBtn.Classes.Set("DangerButton", false);
            DiscordBotToggleBtn.Classes.Set("PrimaryButton", true);
        }

        if (_telegramBotRunner.IsRunning)
        {
            TelegramBotStatusText.Text = string.Format(_localization.GetString("Str_BotRunning"), _telegramBotRunner.ProcessId);
            TelegramBotStatusText.Foreground = OnlineBrush;
            TelegramBotBadge.Background = OnlineSoftBrush;
            TelegramBotToggleBtn.Content = _localization.GetString("Str_StopBot");
            TelegramBotToggleBtn.Classes.Set("PrimaryButton", false);
            TelegramBotToggleBtn.Classes.Set("DangerButton", true);
        }
        else
        {
            TelegramBotStatusText.Text = _localization.GetString("Str_BotStopped");
            TelegramBotStatusText.Foreground = MutedTextBrushStatic;
            TelegramBotBadge.Background = NeutralPillBrush;
            TelegramBotToggleBtn.Content = _localization.GetString("Str_LaunchBot");
            TelegramBotToggleBtn.Classes.Set("DangerButton", false);
            TelegramBotToggleBtn.Classes.Set("PrimaryButton", true);
        }
    }

    private static void AppendLog(StringBuilder buffer, TextBox logBox, string line)
    {
        buffer.AppendLine(line);
        if (buffer.Length > MaxLogLength)
        {
            buffer.Remove(0, buffer.Length - MaxLogLength);
        }
        logBox.Text = buffer.ToString();
        logBox.CaretIndex = logBox.Text.Length;
    }

    private async void BrowseDiscordScript_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await PickPythonScriptAsync();
        if (!string.IsNullOrEmpty(path))
            DiscordBotScriptBox.Text = path;
    }

    private async void BrowseTelegramScript_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await PickPythonScriptAsync();
        if (!string.IsNullOrEmpty(path))
            TelegramBotScriptBox.Text = path;
    }

    private async Task<string?> PickPythonScriptAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Python Bot Script",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Python Scripts") { Patterns = new[] { "*.py" } },
                FilePickerFileTypes.All
            }
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private void ToggleDiscordBot_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        if (_discordBotRunner.IsRunning)
        {
            _discordBotRunner.Stop();
            return;
        }

        try
        {
            int pid = _discordBotRunner.Start(
                DiscordBotScriptBox.Text ?? "",
                DiscordBotTokenBox.Text ?? "",
                DiscordBotPythonBox.Text ?? "python");
            StatusBar.Text = string.Format(_localization.GetString("Status_BotStarted"), "Discord Bot", pid);
        }
        catch (Exception ex)
        {
            StatusBar.Text = string.Format(_localization.GetString("Status_BotError"), "Discord Bot", ex.Message);
            AppendLog(_discordLogBuffer, DiscordBotLogBox, $"[ERR] {ex.Message}");
        }
    }

    private void ToggleTelegramBot_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        if (_telegramBotRunner.IsRunning)
        {
            _telegramBotRunner.Stop();
            return;
        }

        try
        {
            int pid = _telegramBotRunner.Start(
                TelegramBotScriptBox.Text ?? "",
                TelegramBotTokenBox.Text ?? "",
                TelegramBotPythonBox.Text ?? "python");
            StatusBar.Text = string.Format(_localization.GetString("Status_BotStarted"), "Telegram Bot", pid);
        }
        catch (Exception ex)
        {
            StatusBar.Text = string.Format(_localization.GetString("Status_BotError"), "Telegram Bot", ex.Message);
            AppendLog(_telegramLogBuffer, TelegramBotLogBox, $"[ERR] {ex.Message}");
        }
    }

    private async void CopyDiscordBotLogs_Click(object? sender, RoutedEventArgs e)
    {
        if (Clipboard != null && !string.IsNullOrEmpty(DiscordBotLogBox.Text))
        {
            await Clipboard.SetTextAsync(DiscordBotLogBox.Text);
            StatusBar.Text = _localization.GetString("Status_CopiedLogs");
        }
    }

    private async void CopyTelegramBotLogs_Click(object? sender, RoutedEventArgs e)
    {
        if (Clipboard != null && !string.IsNullOrEmpty(TelegramBotLogBox.Text))
        {
            await Clipboard.SetTextAsync(TelegramBotLogBox.Text);
            StatusBar.Text = _localization.GetString("Status_CopiedLogs");
        }
    }

    private void ClearDiscordBotLogs_Click(object? sender, RoutedEventArgs e)
    {
        _discordLogBuffer.Clear();
        DiscordBotLogBox.Text = "";
    }

    private void ClearTelegramBotLogs_Click(object? sender, RoutedEventArgs e)
    {
        _telegramLogBuffer.Clear();
        TelegramBotLogBox.Text = "";
    }

    #endregion

    #region Process Manager

    private void RefreshProcesses_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        _ = LoadProcessesAsync();
    }

    private async Task LoadProcessesAsync()
    {
        try
        {
            _allProcesses = await ProcessService.GetWindowedProcessesAsync();
            ApplyProcessFilter();
            StatusBar.Text = string.Format(_localization.GetString("Status_LoadedProcs"), _allProcesses.Count);
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"Error listing processes: {ex.Message}";
        }
    }

    private void OnProcessSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        ApplyProcessFilter();
    }

    private void ApplyProcessFilter()
    {
        string query = ProcessSearchBox.Text?.Trim() ?? "";
        List<ProcessInfoItem> filtered = string.IsNullOrEmpty(query)
            ? _allProcesses
            : _allProcesses
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.WindowTitle.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Pid.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        ProcessListBox.ItemsSource = filtered;
        ProcessCountBadge.Text = $"{filtered.Count} apps";
    }

    private void KillProcess_Click(object? sender, RoutedEventArgs e)
    {
        if (ProcessListBox.SelectedItem is not ProcessInfoItem selected)
        {
            StatusBar.Text = _localization.GetString("Status_NoProc");
            return;
        }

        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        try
        {
            string name = ProcessService.KillProcess(selected.Pid);
            StatusBar.Text = $"{_localization.GetString("Status_Killed")} {name} ({selected.Pid})";
            _ = LoadProcessesAsync();
        }
        catch (Exception ex)
        {
            StatusBar.Text = $"{_localization.GetString("Status_KillFail")}: {ex.Message}";
        }
    }

    #endregion

    #region Overlay & Color Studio

    private void ToggleOverlay_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);

        if (_overlayWindow == null)
        {
            _overlayWindow = new OverlayWindow();
            _overlayWindow.Closed += (_, _) =>
            {
                _overlayWindow = null;
                UpdateModuleTelemetryBadges();
            };

            string? pos = GetSelectedTag(OverlayPosSelector);
            if (!string.IsNullOrEmpty(pos))
                _overlayWindow.SetPosition(pos);

            ApplyOverlayAppearance();
            _overlayWindow.Show();
            _ = RefreshStatusAsync();

            UpdateModuleTelemetryBadges();
            StatusBar.Text = _localization.GetString("Status_OverlayEnabled");
        }
        else
        {
            _overlayWindow.Close();
            _overlayWindow = null;
            UpdateModuleTelemetryBadges();
            StatusBar.Text = _localization.GetString("Status_OverlayDisabled");
        }
    }

    private void OnOverlayPositionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || _overlayWindow == null) return;
        string? pos = GetSelectedTag(OverlayPosSelector);
        if (!string.IsNullOrEmpty(pos))
            _overlayWindow.SetPosition(pos);
    }

    private void SetColorSlidersInternal(Color color)
    {
        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        AlphaSlider.Value = color.A;
    }

    private void UpdateColorSliders(Color color)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            SetColorSlidersInternal(color);
        }
        finally
        {
            _isUpdating = false;
        }
        UpdateValueLabels(updateHex: true);
    }

    private void UpdateValueLabels(bool updateHex)
    {
        byte r = (byte)RedSlider.Value;
        byte g = (byte)GreenSlider.Value;
        byte b = (byte)BlueSlider.Value;
        byte a = (byte)AlphaSlider.Value;

        RedValue.Text = r.ToString();
        GreenValue.Text = g.ToString();
        BlueValue.Text = b.ToString();
        AlphaValue.Text = a.ToString();

        _colorPreviewBrush.Color = Color.FromArgb(a, r, g, b);

        if (updateHex && !_isUpdating)
        {
            _isUpdating = true;
            try
            {
                HexColorInput.Text = a < 255 ? $"#{a:X2}{r:X2}{g:X2}{b:X2}" : $"#{r:X2}{g:X2}{b:X2}";
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    private void OnColorTargetChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating) return;
        string tag = GetSelectedTag(ColorTargetSelector) ?? "bg";
        Color target = tag switch
        {
            "border" => _borderColor,
            "text" => _textColor,
            _ => _bgColor
        };
        UpdateColorSliders(target);
    }

    private void OnColorSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdating) return;

        UpdateValueLabels(updateHex: true);
        Color newColor = Color.FromArgb(
            (byte)AlphaSlider.Value,
            (byte)RedSlider.Value,
            (byte)GreenSlider.Value,
            (byte)BlueSlider.Value);

        ApplyColorToSelectedTarget(newColor);
    }

    private void OnHexInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        string hex = HexColorInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(hex) || !Color.TryParse(hex, out Color parsed))
            return;

        _isUpdating = true;
        try
        {
            SetColorSlidersInternal(parsed);
        }
        finally
        {
            _isUpdating = false;
        }

        UpdateValueLabels(updateHex: false);
        ApplyColorToSelectedTarget(parsed);
    }

    private void ApplyColorToSelectedTarget(Color newColor)
    {
        switch (GetSelectedTag(ColorTargetSelector))
        {
            case "bg": _bgColor = newColor; break;
            case "border": _borderColor = newColor; break;
            case "text": _textColor = newColor; break;
            default: return;
        }
        ApplyOverlayAppearance();
    }

    private void OnRealTimeSettingChanged(object? sender, RoutedEventArgs e)
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

        bool rainbow = OverlayRainbowSwitch.IsChecked ?? false;
        bool showBorder = OverlayBorderSwitch.IsChecked ?? true;
        double bgOp = OverlayBgOpacitySlider.Value;
        double masterOp = OverlayMasterOpacitySlider.Value;

        _overlayWindow.SetAppearance(_bgColor, _borderColor, _textColor, rainbow, showBorder, bgOp, masterOp);
        UpdateOverlayFunctionStates();
    }

    private void UpdateOverlayFunctionStates()
    {
        _overlayWindow?.UpdateFunctionStates(
            _discordBotRunner.IsRunning,
            _telegramBotRunner.IsRunning,
            _rpcService.IsActive,
            _textColor);
    }

    #endregion

    #region Firebase Auth & Cloud Sync

    private void ShowAuthBanner(string text, IBrush foreground)
    {
        LoginStatusBanner.IsVisible = true;
        LoginStatusText.Text = text;
        LoginStatusText.Foreground = foreground;
    }

    private async void Login_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowAuthBanner("Signing in...", MutedTextBrushStatic);

        var result = await _authService.SignInWithEmailAsync(LoginEmailBox.Text ?? "", LoginPasswordBox.Text ?? "");
        await HandleAuthResultAsync(result, isRegistration: false);
    }

    private async void Register_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowAuthBanner("Creating account...", MutedTextBrushStatic);

        var result = await _authService.RegisterWithEmailAsync(LoginEmailBox.Text ?? "", LoginPasswordBox.Text ?? "");
        await HandleAuthResultAsync(result, isRegistration: true);
    }

    private async void LoginGoogle_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        ShowAuthBanner("Complete Google sign-in in your browser...", MutedTextBrushStatic);

        var result = await _authService.SignInWithGoogleAsync();
        await HandleAuthResultAsync(result, isRegistration: false);
    }

    private void Logout_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn) _ = AnimationHelper.PressAnimation(btn);
        _authService.SignOut();
        LogoutBtn.IsVisible = false;
        SidebarAccountEmailText.Text = _localization.GetString("Str_NotSignedIn");
        SidebarAccountDot.Fill = MutedBrushStatic;
        ShowAuthBanner("Signed out.", MutedTextBrushStatic);
    }

    private async Task HandleAuthResultAsync(AuthResult result, bool isRegistration)
    {
        if (!result.Success)
        {
            ShowAuthBanner($"Error: {result.ErrorMessage}", DangerBrushStatic);
            return;
        }

        LogoutBtn.IsVisible = true;
        SidebarAccountEmailText.Text = result.Email ?? "Connected";
        SidebarAccountDot.Fill = OnlineBrush;

        ShowAuthBanner(
            isRegistration
                ? $"Account created! Signed in as {result.Email}"
                : $"Signed in as {result.Email}",
            OnlineBrush);

        if (isRegistration)
        {
            await _authService.SaveCloudConfigAsync(ReadConfigFromUI());
            return;
        }

        var cloudConfig = await _authService.LoadCloudConfigAsync();
        if (cloudConfig != null)
        {
            ApplyConfigToUI(cloudConfig);
            _configService.Save(cloudConfig);
            LoginStatusText.Text += "\nCloud configuration synced!";
        }
        else
        {
            await _authService.SaveCloudConfigAsync(ReadConfigFromUI());
        }
    }

    #endregion
}
