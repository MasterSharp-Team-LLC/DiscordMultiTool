using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace DiscordMultiTool.Animations;

public static class AnimationHelper
{
    public const double FastDuration = 0.2;
    public const double MediumDuration = 0.35;
    public const double SlowDuration = 0.5;

    public static async Task FadeIn(Control control, double duration = MediumDuration)
    {
        control.Opacity = 0;
        await AnimateProperty(control, Control.OpacityProperty, 0, 1, duration);
    }

    public static async Task FadeOut(Control control, double duration = MediumDuration)
    {
        var current = control.Opacity;
        await AnimateProperty(control, Control.OpacityProperty, current, 0, duration);
    }

    public static async Task ScaleIn(Control control, double duration = MediumDuration, double startScale = 0.8)
    {
        var scaleTransform = new ScaleTransform { ScaleX = startScale, ScaleY = startScale };
        control.RenderTransform = scaleTransform;
        control.Opacity = 0;

        var scaleTask = Task.Run(async () =>
        {
            await AnimateProperty(scaleTransform, ScaleTransform.ScaleXProperty, startScale, 1.0, duration);
        });

        var opacityTask = AnimateProperty(control, Control.OpacityProperty, 0, 1, duration);

        await Task.WhenAll(scaleTask, opacityTask);
    }

    public static async Task SlideInFromTop(Control control, double duration = MediumDuration)
    {
        var translateTransform = new TranslateTransform { Y = -50 };
        control.RenderTransform = translateTransform;
        control.Opacity = 0;

        var translateTask = AnimateProperty(translateTransform, TranslateTransform.YProperty, -50, 0, duration);
        var opacityTask = AnimateProperty(control, Control.OpacityProperty, 0, 1, duration);

        await Task.WhenAll(translateTask, opacityTask);
    }

    public static async Task MoveHorizontal(Control control, double from, double to, double duration = MediumDuration)
    {
        TranslateTransform transform;
        if (control.RenderTransform is TranslateTransform tt)
        {
            transform = tt;
        }
        else
        {
            transform = new TranslateTransform();
            control.RenderTransform = transform;
        }
        
        // Ensure starting value
        transform.X = from;
        
        await AnimateProperty(transform, TranslateTransform.XProperty, from, to, duration);
    }

    public static async Task AnimateValue(Avalonia.Controls.Primitives.RangeBase control, double from, double to, double duration = MediumDuration)
    {
        await AnimateProperty(control, Avalonia.Controls.Primitives.RangeBase.ValueProperty, from, to, duration);
    }

    public static void AddHoverAnimation(Button button)
    {
        var originalBackground = button.Background;

        button.PointerEntered += (s, e) =>
        {
            if (button.Background is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                var newColor = new Color(
                    color.A,
                    (byte)Math.Min(255, color.R + 15),
                    (byte)Math.Min(255, color.G + 15),
                    (byte)Math.Min(255, color.B + 15)
                );
                button.Background = new SolidColorBrush(newColor);
            }
        };

        button.PointerExited += (s, e) =>
        {
            button.Background = originalBackground;
        };
    }

    public static async Task PressAnimation(Button button)
    {
        var scale = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
        button.RenderTransform = scale;

        await AnimateProperty(scale, ScaleTransform.ScaleXProperty, 1.0, 0.95, FastDuration / 2);
        await AnimateProperty(scale, ScaleTransform.ScaleYProperty, 1.0, 0.95, FastDuration / 2);

        await AnimateProperty(scale, ScaleTransform.ScaleXProperty, 0.95, 1.0, FastDuration / 2);
        await AnimateProperty(scale, ScaleTransform.ScaleYProperty, 0.95, 1.0, FastDuration / 2);
    }

    private static async Task AnimateProperty(
        AvaloniaObject target, 
        AvaloniaProperty property, 
        double startValue, 
        double endValue, 
        double duration)
    {
        const int frameRate = 60;
        int frameCount = (int)(duration * frameRate);
        var frameDuration = TimeSpan.FromSeconds(duration / frameCount);

        for (int i = 0; i <= frameCount; i++)
        {
            double progress = (double)i / frameCount;
            double easeProgress = EaseOutCubic(progress);
            double value = startValue + (endValue - startValue) * easeProgress;

            target.SetValue(property, value);

            if (i < frameCount)
            {
                await Task.Delay(frameDuration);
            }
        }

        target.SetValue(property, endValue);
    }

    private static double EaseOutCubic(double t)
    {
        double p = t - 1;
        return p * p * p + 1;
    }
}
