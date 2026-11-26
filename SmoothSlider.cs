using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace DiscordMultiTool;

public class SmoothSlider : Slider
{
    private Track? _track;
    private DispatcherTimer? _animationTimer;
    private readonly TranslateTransform _thumbTransform = new();
    private readonly ScaleTransform _fillTransform = new();
    private double _displayedValue;
    private double _velocity;

    // Spring physics parameters
    private const double SpringStiffness = 150.0;
    private const double SpringDamping = 15.0;
    private const double TimeStep = 0.016;

    protected override Type StyleKeyOverride => typeof(Slider);

    public SmoothSlider()
    {
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(TimeStep)
        };
        _animationTimer.Tick += UpdatePhysics;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _track = e.NameScope.Find<Track>("PART_Track");
        _displayedValue = Value;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animationTimer?.Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty && _animationTimer is { IsEnabled: false })
        {
            _animationTimer.Start();
        }
    }

    private void UpdatePhysics(object? sender, EventArgs e)
    {
        var track = _track;
        if (track == null)
        {
            _animationTimer?.Stop();
            return;
        }

        double target = Value;
        double current = _displayedValue;

        double displacement = current - target;
        double acceleration = (-SpringStiffness * displacement) + (-SpringDamping * _velocity);

        _velocity += acceleration * TimeStep;
        current += _velocity * TimeStep;

        if (Math.Abs(displacement) < 0.0001 && Math.Abs(_velocity) < 0.0001)
        {
            current = target;
            _velocity = 0;
            _animationTimer?.Stop();
        }

        _displayedValue = current;

        UpdateThumbPosition(track, current, target);
        UpdateFillSize(track, current, target);
    }

    private void UpdateThumbPosition(Track track, double visualValue, double targetValue)
    {
        var thumb = track.Thumb;
        if (thumb == null) return;

        double diff = visualValue - targetValue;
        if (Math.Abs(diff) < 0.0001)
        {
            thumb.RenderTransform = null;
            return;
        }

        double range = Maximum - Minimum;
        if (range <= 0) return;

        bool isVertical = Orientation == Orientation.Vertical;
        double trackLength = isVertical ? track.Bounds.Height : track.Bounds.Width;
        double thumbLength = isVertical ? thumb.Bounds.Height : thumb.Bounds.Width;
        double availableLength = trackLength - thumbLength;
        if (availableLength <= 0) return;

        double pixelOffset = diff * (availableLength / range);
        double directionFactor = isVertical ? -1.0 : 1.0;
        if (IsDirectionReversed) directionFactor *= -1.0;

        if (isVertical)
        {
            _thumbTransform.X = 0;
            _thumbTransform.Y = pixelOffset * directionFactor;
        }
        else
        {
            _thumbTransform.X = pixelOffset * directionFactor;
            _thumbTransform.Y = 0;
        }

        if (!ReferenceEquals(thumb.RenderTransform, _thumbTransform))
        {
            thumb.RenderTransform = _thumbTransform;
        }
    }

    private void UpdateFillSize(Track track, double visualValue, double targetValue)
    {
        var fill = track.DecreaseButton;
        if (fill == null) return;

        if (Math.Abs(targetValue) < 0.0001 && Math.Abs(visualValue) < 0.0001)
        {
            fill.RenderTransform = null;
            return;
        }

        double normTarget = targetValue - Minimum;
        double normVisual = visualValue - Minimum;
        if (normTarget <= 0.0001)
        {
            fill.RenderTransform = null;
            return;
        }

        double scale = Math.Max(0, normVisual / normTarget);

        if (Orientation == Orientation.Vertical)
        {
            fill.RenderTransformOrigin = new RelativePoint(0.5, 1.0, RelativeUnit.Relative);
            _fillTransform.ScaleX = 1.0;
            _fillTransform.ScaleY = scale;
        }
        else
        {
            fill.RenderTransformOrigin = new RelativePoint(0.0, 0.5, RelativeUnit.Relative);
            _fillTransform.ScaleX = scale;
            _fillTransform.ScaleY = 1.0;
        }

        if (!ReferenceEquals(fill.RenderTransform, _fillTransform))
        {
            fill.RenderTransform = _fillTransform;
        }
    }
}
