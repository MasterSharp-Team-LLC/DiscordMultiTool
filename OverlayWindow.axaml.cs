using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace DiscordMultiTool;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private System.Threading.CancellationTokenSource? _hoverCts;

    private bool _rainbowMode = false;
    private double _bgOpacity = 0.75;
    private DispatcherTimer? _rainbowTimer;
    private double _hue = 0;

    public void SetAppearance(Color bg, Color border, Color text, bool rainbow, bool showBorder, double bgOpacity, double masterOpacity)
    {
        this.Opacity = masterOpacity;
        _bgOpacity = bgOpacity;
        _rainbowMode = rainbow;

        var rootBorder = this.FindControl<Border>("RootBorder");
        var rpcText = this.FindControl<TextBlock>("RpcStatusText");
        var titleText = this.FindControl<TextBlock>("TitleText");
        var procText = this.FindControl<TextBlock>("ProcessStatusText");
        var verText = this.FindControl<TextBlock>("VersionText");

        // Border Thickness
        if (rootBorder != null)
        {
             rootBorder.BorderThickness = showBorder ? new Thickness(1) : new Thickness(0);
        }

        // Colors
        if (rootBorder != null)
            rootBorder.BorderBrush = new SolidColorBrush(border);
        
        // Text Color Logic
        {
            var brush = new SolidColorBrush(text);
            if (rpcText != null) rpcText.Foreground = brush;
            if (titleText != null) titleText.Foreground = brush;
            if (procText != null) procText.Foreground = brush;
            if (verText != null) verText.Foreground = brush;
        }

        if (_rainbowMode)
        {
            StartRainbow();
        }
        else
        {
            StopRainbow();
            if (rootBorder != null)
            {
                 // Apply opacity to background color using FromArgb instead of bitwise math
                 var finalColor = Color.FromArgb((byte)(bgOpacity * 255), bg.R, bg.G, bg.B);
                 rootBorder.Background = new SolidColorBrush(finalColor);
            }
        }
    }

    private void StartRainbow()
    {
        if (_rainbowTimer == null)
        {
             _rainbowTimer = new DispatcherTimer();
             _rainbowTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
             _rainbowTimer.Tick += (s, e) =>
             {
                 _hue += 1.0;
                 if (_hue > 360) _hue = 0;
                 
                 var rootBorder = this.FindControl<Border>("RootBorder");
                 var titleText = this.FindControl<TextBlock>("TitleText");
                 var verText = this.FindControl<TextBlock>("VersionText");

                 if (rootBorder != null)
                 {
                     var color = HslToRgb(_hue, 0.8, 0.5);
                     // Apply Opacity to background
                     var finalColor = Color.FromArgb((byte)(_bgOpacity * 255), color.R, color.G, color.B);
                     rootBorder.Background = new SolidColorBrush(finalColor);
                 }

                 // Rainbow text color (offset by 180 degrees for contrast)
                 double textHue = _hue + 180;
                 if (textHue > 360) textHue -= 360;
                 var textColor = HslToRgb(textHue, 1.0, 0.6);
                 var textBrush = new SolidColorBrush(textColor);
                 
                 // Apply rainbow to title and version
                 if (titleText != null) titleText.Foreground = textBrush;
                 if (verText != null) verText.Foreground = textBrush;
                 
                 // Apply rainbow only to ACTIVE function items
                 foreach (var func in _functions)
                 {
                     if (func.IsActive && func.TextBlock != null)
                     {
                         func.TextBlock.Foreground = textBrush;
                     }
                     // Inactive items keep their gray color
                 }
             };
        }
        _rainbowTimer.Start();
    }

    private void StopRainbow()
    {
        _rainbowTimer?.Stop();
    }
    
    // Helper
    private Color HslToRgb(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;
        
        double r = 0, g = 0, b = 0;
        
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        
        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    // Event Handlers

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnWindowPointerEnter(object? sender, PointerEventArgs e)
    {
        _hoverCts?.Cancel();
        var closeBtn = this.FindControl<Button>("CloseButton");
        if (closeBtn != null)
        {
            closeBtn.Opacity = 1;
        }
    }

    private async void OnWindowPointerExit(object? sender, PointerEventArgs e)
    {
        _hoverCts?.Cancel();
        _hoverCts = new System.Threading.CancellationTokenSource();
        var token = _hoverCts.Token;

        try
        {
            await System.Threading.Tasks.Task.Delay(2000, token);
            
            var closeBtn = this.FindControl<Button>("CloseButton");
            if (closeBtn != null)
            {
                closeBtn.Opacity = 0;
            }
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Canceled by re-enter
        }
    }

    private void CloseOverlay(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    // Function State Tracking
    private class FunctionState
    {
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime ActivationTime { get; set; }
        public TextBlock? TextBlock { get; set; }
    }

    private List<FunctionState> _functions = new();
    private Color _userTextColor = Colors.White;
    private readonly Color _inactiveColor = Color.Parse("#64748b");
    private const double ActiveFontSize = 19;
    private const double InactiveFontSize = 14;

    public void UpdateFunctionStates(bool discordBotActive, bool telegramBotActive, bool richPresenceActive, Color userTextColor)
    {
        _userTextColor = userTextColor;

        // Initialize function list if needed
        if (_functions.Count == 0)
        {
            _functions = new List<FunctionState>
            {
                new FunctionState { Name = "Discord Bot", TextBlock = this.FindControl<TextBlock>("DiscordBotText") },
                new FunctionState { Name = "Telegram Bot", TextBlock = this.FindControl<TextBlock>("TelegramBotText") },
                new FunctionState { Name = "Rich Presence", TextBlock = this.FindControl<TextBlock>("RichPresenceText") }
            };
        }

        // Update activation states and timestamps
        UpdateFunctionState(_functions[0], discordBotActive, "Discord Bot");
        UpdateFunctionState(_functions[1], telegramBotActive, "Telegram Bot");
        UpdateFunctionState(_functions[2], richPresenceActive, "Rich Presence");

        // Sort by activation state and time (active items first, sorted by most recent)
        var sortedFunctions = _functions
            .OrderByDescending(f => f.IsActive)
            .ThenByDescending(f => f.ActivationTime)
            .ToList();

        // Reorder visual elements in container
        var container = this.FindControl<StackPanel>("FunctionContainer");
        if (container != null)
        {
            container.Children.Clear();
            foreach (var func in sortedFunctions)
            {
                if (func.TextBlock != null)
                {
                    container.Children.Add(func.TextBlock);
                }
            }
        }

        // Apply visual states
        foreach (var func in _functions)
        {
            ApplyVisualState(func);
        }
    }

    private void UpdateFunctionState(FunctionState state, bool isActive, string functionName)
    {
        bool wasActive = state.IsActive;
        state.IsActive = isActive;

        // Update activation time when transitioning from inactive to active
        if (isActive && !wasActive)
        {
            state.ActivationTime = DateTime.Now;
        }
    }

    private void ApplyVisualState(FunctionState state)
    {
        if (state.TextBlock == null) return;

        // Determine status string key
        string statusKey = state.IsActive ? "Str_Overlay_Active" : "Str_Overlay_Inactive";
        string status = GetString(statusKey);
        
        // Determine function name key
        string functionKey = state.Name switch
        {
            "Discord Bot" => "Str_Overlay_DiscordBot",
            "Telegram Bot" => "Str_Overlay_TelegramBot",
            "Rich Presence" => "Str_Overlay_RichPresence",
            _ => ""
        };
        string functionName = string.IsNullOrEmpty(functionKey) ? state.Name : GetString(functionKey);

        if (state.IsActive)
        {
            // Active state: large font, user's selected color
            state.TextBlock.FontSize = ActiveFontSize;
            state.TextBlock.Foreground = new SolidColorBrush(_userTextColor);
            state.TextBlock.Text = $"{functionName}: {status}";
        }
        else
        {
            // Inactive state: small font, gray color
            state.TextBlock.FontSize = InactiveFontSize;
            state.TextBlock.Foreground = new SolidColorBrush(_inactiveColor);
            state.TextBlock.Text = $"{functionName}: {status}";
        }
    }

    private string GetString(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var value) == true && value is string s)
        {
            return s;
        }
        return key;
    }

    public void SetPosition(string position)
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var bounds = screen.WorkingArea;
        var w = Width;
        var h = Height;
        var margin = 20;

        double x = bounds.X + margin;
        double y = bounds.Y + margin;

        switch (position)
        {
            case "Top Left":
                x = bounds.X + margin;
                y = bounds.Y + margin;
                break;
            case "Top Right":
                x = bounds.Right - w - margin;
                y = bounds.Y + margin;
                break;
            case "Bottom Left":
                x = bounds.X + margin;
                y = bounds.Bottom - h - margin;
                break;
            case "Bottom Right":
                x = bounds.Right - w - margin;
                y = bounds.Bottom - h - margin;
                break;
        }

        Position = new PixelPoint((int)x, (int)y);
    }
}
