using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;

namespace DiscordMultiTool;

/// <summary>
/// ScrollViewer with critically-damped 2nd-order inertial scrolling.
/// Intercepts PointerWheelChanged in the Tunnel phase BEFORE Avalonia's internal
/// ScrollContentPresenter can snap Offset and mark e.Handled = true.
/// Starts from zero velocity (visibly falls behind initial wheel input), ramps up speed
/// smoothly as wheel ticks accumulate, and glides to rest with zero bounce.
/// </summary>
public class SmoothScrollViewer : ScrollViewer
{
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();

    private double _currentY;
    private double _targetY;
    private double _velocityY;
    private double _lastTickSeconds;
    private bool _isApplyingOffset;

    // Tuned for a noticeable, silky lag-behind ramp-up (~550ms glide) with zero overshoot
    private const double WheelStepPixels = 130.0;
    private const double Omega = 7.5;
    private const double Zeta = 1.05;
    private const double MaxSubStep = 0.004;

    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public SmoothScrollViewer()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(8)
        };
        _timer.Tick += OnTick;

        // Tunnel runs BEFORE ScrollContentPresenter.OnPointerWheelChanged snaps the offset!
        AddHandler(PointerWheelChangedEvent, OnPreviewPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopSmoothScroll();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OffsetProperty && !_isApplyingOffset)
        {
            // Sync when scrolled externally (scrollbar thumb drag, ScrollToHome, keyboard)
            double y = Offset.Y;
            _currentY = y;
            _targetY = y;
            _velocityY = 0;
            StopSmoothScroll();
        }
    }

    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Handled || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        if (Math.Abs(e.Delta.Y) < 0.001)
            return;

        // Let nested scrollable controls (e.g. ProcessListBox, Bot Console TextBox) handle wheel first if they can scroll in this direction
        if (ShouldYieldToNestedScrollViewer(e.Source as Visual, e.Delta.Y))
            return;

        double maxOffset = Math.Max(0.0, Extent.Height - Viewport.Height);
        if (maxOffset <= 0.5)
            return;

        if (!_timer.IsEnabled)
        {
            _currentY = Offset.Y;
            _targetY = _currentY;
            _velocityY = 0;
            _stopwatch.Restart();
            _lastTickSeconds = 0;
        }

        double nextTarget = Math.Clamp(_targetY - (e.Delta.Y * WheelStepPixels), 0.0, maxOffset);

        // If already resting at top/bottom edge and scrolling further into that edge, let outer parent scroll if any
        if (Math.Abs(nextTarget - _currentY) < 0.1 && Math.Abs(_velocityY) < 1.0)
        {
            return;
        }

        _targetY = nextTarget;
        e.Handled = true;

        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    private bool ShouldYieldToNestedScrollViewer(Visual? source, double wheelDeltaY)
    {
        Visual? current = source;
        while (current != null && !ReferenceEquals(current, this))
        {
            if (current is ScrollViewer nested)
            {
                double nestedMax = Math.Max(0.0, nested.Extent.Height - nested.Viewport.Height);
                if (nestedMax > 0.5)
                {
                    bool scrollingUp = wheelDeltaY > 0;
                    bool canScrollUp = nested.Offset.Y > 0.5;
                    bool canScrollDown = nested.Offset.Y < nestedMax - 0.5;

                    if ((scrollingUp && canScrollUp) || (!scrollingUp && canScrollDown))
                    {
                        return true;
                    }
                }
            }
            current = current.GetVisualParent();
        }
        return false;
    }

    private void StopSmoothScroll()
    {
        _timer.Stop();
        _stopwatch.Stop();
        _velocityY = 0;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = _stopwatch.Elapsed.TotalSeconds;
        double dt = now - _lastTickSeconds;
        _lastTickSeconds = now;

        if (dt <= 0.0) dt = 0.008;
        if (dt > 0.04) dt = 0.04;

        double maxOffset = Math.Max(0.0, Extent.Height - Viewport.Height);
        _targetY = Math.Clamp(_targetY, 0.0, maxOffset);

        // Sub-stepped critically-damped spring-damper integration
        double remaining = dt;
        const double omegaSq = Omega * Omega;
        const double damping = 2.0 * Zeta * Omega;

        while (remaining > 0.0)
        {
            double step = Math.Min(remaining, MaxSubStep);
            double error = _targetY - _currentY;
            double accel = (omegaSq * error) - (damping * _velocityY);

            _velocityY += accel * step;
            _currentY += _velocityY * step;
            remaining -= step;
        }

        _currentY = Math.Clamp(_currentY, 0.0, maxOffset);

        if (Math.Abs(_targetY - _currentY) < 0.2 && Math.Abs(_velocityY) < 1.5)
        {
            _currentY = _targetY;
            _velocityY = 0;
            _timer.Stop();
            _stopwatch.Stop();
        }

        _isApplyingOffset = true;
        try
        {
            Offset = new Vector(Offset.X, _currentY);
        }
        finally
        {
            _isApplyingOffset = false;
        }
    }
}
