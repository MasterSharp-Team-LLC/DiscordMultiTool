using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using System;

namespace DiscordMultiTool
{
    public class SmoothSlider : Slider
    {
        private Track? _track;
        private DispatcherTimer? _animationTimer;
        private double _displayedValue;
        private double _velocity;
        
        // Spring physics parameters
        private const double SpringStiffness = 150.0;
        private const double SpringDamping = 15.0;
        private const double TimeStep = 0.016;

        protected override Type StyleKeyOverride => typeof(Slider);

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _track = e.NameScope.Find<Track>("PART_Track");
            _displayedValue = Value;
        }

        public SmoothSlider()
        {
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(TimeStep)
            };
            _animationTimer.Tick += UpdatePhysics;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty)
            {
                // When Value changes (User Input), start the physics loop
                if (_animationTimer != null && !_animationTimer.IsEnabled)
                {
                    _animationTimer.Start();
                }
            }
        }

        private void UpdatePhysics(object? sender, EventArgs e)
        {
            if (_track == null)
            {
                _animationTimer?.Stop();
                return;
            }

            double target = Value;
            double current = _displayedValue;

            // Spring Physics
            double displacement = current - target;
            double force = -SpringStiffness * displacement;
            double dampingForce = -SpringDamping * _velocity;
            double acceleration = force + dampingForce;

            _velocity += acceleration * TimeStep;
            current += _velocity * TimeStep;

            // Snap
            if (Math.Abs(displacement) < 0.0001 && Math.Abs(_velocity) < 0.0001)
            {
                current = target;
                _velocity = 0;
                _animationTimer?.Stop();
            }

            _displayedValue = current;

            UpdateVisuals(current, target);
        }

        private void UpdateVisuals(double visualValue, double targetValue)
        {
            if (_track == null) return;

            // 1. Move the Thumb (Translate)
            UpdateThumbPosition(visualValue, targetValue);

            // 2. Scale the Fill (Scale)
            UpdateFillSize(visualValue, targetValue);
        }

        private void UpdateThumbPosition(double visualValue, double targetValue)
        {
            if (_track.Thumb == null) return;

            double diff = visualValue - targetValue;
            
            if (Math.Abs(diff) < 0.0001)
            {
                _track.Thumb.RenderTransform = null;
                return;
            }

            double range = Maximum - Minimum;
            if (range <= 0) return;

            double trackLength = (Orientation == Avalonia.Layout.Orientation.Vertical) 
                ? _track.Bounds.Height 
                : _track.Bounds.Width;
                
            double thumbLength = (Orientation == Avalonia.Layout.Orientation.Vertical)
                ? _track.Thumb.Bounds.Height
                : _track.Thumb.Bounds.Width;

            double availableLength = trackLength - thumbLength;
            if (availableLength <= 0) return;

            double pixelsPerUnit = availableLength / range;
            double pixelOffset = diff * pixelsPerUnit;
            
            // Adjust for direction
            double directionFactor = 1.0;
            if (Orientation == Avalonia.Layout.Orientation.Vertical) directionFactor = -1.0;
            if (IsDirectionReversed) directionFactor *= -1.0;

            if (Orientation == Avalonia.Layout.Orientation.Vertical)
                _track.Thumb.RenderTransform = new Avalonia.Media.TranslateTransform(0, pixelOffset * directionFactor);
            else
                _track.Thumb.RenderTransform = new Avalonia.Media.TranslateTransform(pixelOffset * directionFactor, 0);
        }

        private void UpdateFillSize(double visualValue, double targetValue)
        {
            // The "Fill" is typically the DecreaseButton of the Track
            var fill = _track.DecreaseButton;
            if (fill == null) return;

            if (Math.Abs(targetValue) < 0.0001 && Math.Abs(visualValue) < 0.0001)
            {
                fill.RenderTransform = null;
                return;
            }

            // Calculate Scale Factor
            // Target is where the Layout put the Fill (Logical Value).
            // Visual is where we want it to be.
            // Scale = Visual / Target.
            // CAUTION: If Target is 0, Scale is Infinity.
            // CAUTION: Minimum might not be 0.
            
            // Normalize values to 0-based range
            double normTarget = targetValue - Minimum;
            double normVisual = visualValue - Minimum;
            
            if (normTarget <= 0.0001)
            {
                // If target is 0, we can't scale 0 to something else using a multiplier.
                // But if visual is also 0, scale 1.
                // If visual > 0, we have a problem. 0 * X = 0.
                // We cannot fix the Fill if the Slider thinks it should be 0 width.
                // In this edge case, the Thumb offset handles the "Spring out of 0", 
                // but the Fill will disappear until Target > 0.
                // This is acceptable visual degradation for the 0-bound case.
                fill.RenderTransform = null;
                return;
            }

            double scale = normVisual / normTarget;
            
            // Limit scale to prevent explosion or negative size
            if (scale < 0) scale = 0;
            
            // Apply Scale Transform
            // We need to set the Origin to the "Start" of the fill.
            // Horizontal: Left (0,0)?
            // Vertical: Bottom (0,1)?
            
            // Default TransformOrigin is Center (0.5, 0.5) usually? 
            // We should ensure it scales from the correct edge.
            
            if (Orientation == Avalonia.Layout.Orientation.Vertical)
            {
                // Vertical Fill usually grows from Bottom.
                // Origin: Bottom Center? (0.5, 1.0) relative.
                fill.RenderTransformOrigin = new Avalonia.RelativePoint(0.5, 1.0, Avalonia.RelativeUnit.Relative);
                fill.RenderTransform = new Avalonia.Media.ScaleTransform(1.0, scale);
            }
            else
            {
                // Horizontal Fill grows from Left.
                // Origin: Left Center? (0.0, 0.5) relative.
                fill.RenderTransformOrigin = new Avalonia.RelativePoint(0.0, 0.5, Avalonia.RelativeUnit.Relative);
                fill.RenderTransform = new Avalonia.Media.ScaleTransform(scale, 1.0);
            }
        }
    }

}
