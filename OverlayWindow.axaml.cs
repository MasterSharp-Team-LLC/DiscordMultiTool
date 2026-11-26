using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using DiscordMultiTool.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMultiTool;

public partial class OverlayWindow : Window
{
    private sealed class FunctionState
    {
        public string Name { get; init; } = "";
        public string ResourceKey { get; init; } = "";
        public bool IsActive { get; set; }
        public DateTime ActivationTime { get; set; }
        public TextBlock? TextBlock { get; init; }
    }

    private Border? _rootBorder;
    private TextBlock? _titleText;
    private TextBlock? _versionText;
    private Button? _closeButton;
    private StackPanel? _functionContainer;

    // Reusable mutable brushes — zero per-frame allocations during Rainbow Mode
    private readonly SolidColorBrush _bgBrush = new(Color.Parse("#BF0f172a"));
    private readonly SolidColorBrush _borderBrush = new(Color.Parse("#334155"));
    private readonly SolidColorBrush _textBrush = new(Colors.White);
    private readonly SolidColorBrush _rainbowTextBrush = new(Colors.White);
    private readonly SolidColorBrush _inactiveBrush = new(Color.Parse("#64748b"));

    private CancellationTokenSource? _hoverCts;
    private DispatcherTimer? _rainbowTimer;
    private bool _rainbowMode;
    private double _bgOpacity = 0.75;
    private double _hue;

    private readonly List<FunctionState> _functions = new();
    private Color _userTextColor = Colors.White;

    private const double ActiveFontSize = 19;
    private const double InactiveFontSize = 14;

    public OverlayWindow()
    {
        InitializeComponent();
        CacheControls();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void CacheControls()
    {
        _rootBorder = this.FindControl<Border>("RootBorder");
        _titleText = this.FindControl<TextBlock>("TitleText");
        _versionText = this.FindControl<TextBlock>("VersionText");
        _closeButton = this.FindControl<Button>("CloseButton");
        _functionContainer = this.FindControl<StackPanel>("FunctionContainer");

        if (_rootBorder != null)
        {
            _rootBorder.Background = _bgBrush;
            _rootBorder.BorderBrush = _borderBrush;
        }

        if (_versionText != null)
        {
            _versionText.Text = $"v{UpdateService.CurrentVersionString}";
        }

        _functions.Add(new FunctionState
        {
            Name = "Discord Bot",
            ResourceKey = "Str_Overlay_DiscordBot",
            TextBlock = this.FindControl<TextBlock>("DiscordBotText")
        });
        _functions.Add(new FunctionState
        {
            Name = "Telegram Bot",
            ResourceKey = "Str_Overlay_TelegramBot",
            TextBlock = this.FindControl<TextBlock>("TelegramBotText")
        });
        _functions.Add(new FunctionState
        {
            Name = "Rich Presence",
            ResourceKey = "Str_Overlay_RichPresence",
            TextBlock = this.FindControl<TextBlock>("RichPresenceText")
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        StopRainbow();
        _hoverCts?.Cancel();
        _hoverCts?.Dispose();
        _hoverCts = null;
        base.OnClosed(e);
    }

    public void SetAppearance(Color bg, Color border, Color text, bool rainbow, bool showBorder, double bgOpacity, double masterOpacity)
    {
        Opacity = masterOpacity;
        _bgOpacity = bgOpacity;
        _rainbowMode = rainbow;
        _userTextColor = text;

        if (_rootBorder != null)
        {
            _rootBorder.BorderThickness = showBorder ? new Thickness(1) : new Thickness(0);
            _borderBrush.Color = border;
        }

        _textBrush.Color = text;

        if (_rainbowMode)
        {
            StartRainbow();
        }
        else
        {
            StopRainbow();
            _bgBrush.Color = Color.FromArgb((byte)(bgOpacity * 255), bg.R, bg.G, bg.B);
            if (_titleText != null) _titleText.Foreground = _textBrush;
            if (_versionText != null) _versionText.Foreground = _textBrush;
            foreach (var func in _functions)
            {
                if (func.IsActive && func.TextBlock != null)
                    func.TextBlock.Foreground = _textBrush;
            }
        }
    }

    private void StartRainbow()
    {
        if (_rainbowTimer == null)
        {
            _rainbowTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _rainbowTimer.Tick += OnRainbowTick;
        }

        if (_titleText != null) _titleText.Foreground = _rainbowTextBrush;
        if (_versionText != null) _versionText.Foreground = _rainbowTextBrush;
        foreach (var func in _functions)
        {
            if (func.IsActive && func.TextBlock != null)
                func.TextBlock.Foreground = _rainbowTextBrush;
        }

        if (!_rainbowTimer.IsEnabled)
        {
            _rainbowTimer.Start();
        }
    }

    private void OnRainbowTick(object? sender, EventArgs e)
    {
        _hue = (_hue + 1.0) % 360.0;

        var bgColor = HslToRgb(_hue, 0.8, 0.5);
        _bgBrush.Color = Color.FromArgb((byte)(_bgOpacity * 255), bgColor.R, bgColor.G, bgColor.B);

        double textHue = (_hue + 180.0) % 360.0;
        _rainbowTextBrush.Color = HslToRgb(textHue, 1.0, 0.6);
    }

    private void StopRainbow()
    {
        _rainbowTimer?.Stop();
    }

    private static Color HslToRgb(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;

        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnWindowPointerEnter(object? sender, PointerEventArgs e)
    {
        _hoverCts?.Cancel();
        if (_closeButton != null)
            _closeButton.Opacity = 1;
    }

    private async void OnWindowPointerExit(object? sender, PointerEventArgs e)
    {
        _hoverCts?.Cancel();
        _hoverCts?.Dispose();
        _hoverCts = new CancellationTokenSource();
        var token = _hoverCts.Token;

        try
        {
            await Task.Delay(2000, token);
            if (_closeButton != null)
                _closeButton.Opacity = 0;
        }
        catch (TaskCanceledException)
        {
            // Pointer re-entered
        }
    }

    private void CloseOverlay(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void UpdateFunctionStates(bool discordBotActive, bool telegramBotActive, bool richPresenceActive, Color userTextColor)
    {
        _userTextColor = userTextColor;
        _textBrush.Color = userTextColor;

        UpdateFunctionState(_functions[0], discordBotActive);
        UpdateFunctionState(_functions[1], telegramBotActive);
        UpdateFunctionState(_functions[2], richPresenceActive);

        var sorted = _functions
            .OrderByDescending(f => f.IsActive)
            .ThenByDescending(f => f.ActivationTime)
            .ToList();

        if (_functionContainer != null)
        {
            _functionContainer.Children.Clear();
            foreach (var func in sorted)
            {
                if (func.TextBlock != null)
                    _functionContainer.Children.Add(func.TextBlock);
            }
        }

        foreach (var func in _functions)
        {
            ApplyVisualState(func);
        }
    }

    private static void UpdateFunctionState(FunctionState state, bool isActive)
    {
        if (isActive && !state.IsActive)
        {
            state.ActivationTime = DateTime.Now;
        }
        state.IsActive = isActive;
    }

    private void ApplyVisualState(FunctionState state)
    {
        if (state.TextBlock == null) return;

        string statusKey = state.IsActive ? "Str_Overlay_Active" : "Str_Overlay_Inactive";
        string status = GetString(statusKey);
        string functionName = string.IsNullOrEmpty(state.ResourceKey) ? state.Name : GetString(state.ResourceKey);

        state.TextBlock.Text = $"{functionName}: {status}";
        if (state.IsActive)
        {
            state.TextBlock.FontSize = ActiveFontSize;
            state.TextBlock.Foreground = _rainbowMode ? _rainbowTextBrush : _textBrush;
        }
        else
        {
            state.TextBlock.FontSize = InactiveFontSize;
            state.TextBlock.Foreground = _inactiveBrush;
        }
    }

    private static string GetString(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var value) == true && value is string s)
            return s;
        return key;
    }

    public void SetPosition(string position)
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var bounds = screen.WorkingArea;
        double w = Width;
        double h = Height;
        const int margin = 20;

        (double x, double y) = position switch
        {
            "Top Right" => (bounds.Right - w - margin, bounds.Y + margin),
            "Bottom Left" => (bounds.X + margin, bounds.Bottom - h - margin),
            "Bottom Right" => (bounds.Right - w - margin, bounds.Bottom - h - margin),
            _ => (bounds.X + margin, bounds.Y + margin)
        };

        Position = new PixelPoint((int)x, (int)y);
    }
}
