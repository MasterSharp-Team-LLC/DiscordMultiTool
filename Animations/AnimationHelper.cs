using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;

namespace DiscordMultiTool.Animations;

public static class AnimationHelper
{
    public const double FastDuration = 0.18;
    public const double MediumDuration = 0.28;
    public const double SlowDuration = 0.45;

    public static async Task FadeIn(Control? control, double duration = MediumDuration)
    {
        if (control == null) return;
        await AnimateDoubleAsync(control, Visual.OpacityProperty, 0.0, 1.0, duration);
    }

    public static async Task FadeOut(Control? control, double duration = MediumDuration)
    {
        if (control == null) return;
        double current = control.Opacity;
        if (current <= 0.0) return;
        await AnimateDoubleAsync(control, Visual.OpacityProperty, current, 0.0, duration);
    }

    public static async Task ScaleIn(Control? control, double duration = MediumDuration, double startScale = 0.92)
    {
        if (control == null) return;

        var scaleTransform = EnsureScaleTransform(control);
        scaleTransform.ScaleX = startScale;
        scaleTransform.ScaleY = startScale;
        control.Opacity = 0;

        await Task.WhenAll(
            AnimateDoubleAsync(scaleTransform, ScaleTransform.ScaleXProperty, startScale, 1.0, duration),
            AnimateDoubleAsync(scaleTransform, ScaleTransform.ScaleYProperty, startScale, 1.0, duration),
            AnimateDoubleAsync(control, Visual.OpacityProperty, 0.0, 1.0, duration));
    }

    public static async Task SlideInFromTop(Control? control, double duration = MediumDuration)
    {
        if (control == null) return;

        var translate = EnsureTranslateTransform(control);
        translate.Y = -24;
        control.Opacity = 0;

        await Task.WhenAll(
            AnimateDoubleAsync(translate, TranslateTransform.YProperty, -24.0, 0.0, duration),
            AnimateDoubleAsync(control, Visual.OpacityProperty, 0.0, 1.0, duration));
    }

    public static async Task MoveHorizontal(Control? control, double from, double to, double duration = MediumDuration)
    {
        if (control == null) return;

        var translate = EnsureTranslateTransform(control);
        translate.X = from;
        await AnimateDoubleAsync(translate, TranslateTransform.XProperty, from, to, duration);
    }

    public static async Task PressAnimation(Button? button)
    {
        if (button == null) return;

        var scale = EnsureScaleTransform(button);
        double half = FastDuration / 2.0;

        await Task.WhenAll(
            AnimateDoubleAsync(scale, ScaleTransform.ScaleXProperty, 1.0, 0.96, half),
            AnimateDoubleAsync(scale, ScaleTransform.ScaleYProperty, 1.0, 0.96, half));

        await Task.WhenAll(
            AnimateDoubleAsync(scale, ScaleTransform.ScaleXProperty, 0.96, 1.0, half),
            AnimateDoubleAsync(scale, ScaleTransform.ScaleYProperty, 0.96, 1.0, half));
    }

    private static ScaleTransform EnsureScaleTransform(Control control)
    {
        if (control.RenderTransform is ScaleTransform st)
            return st;

        var created = new ScaleTransform(1.0, 1.0);
        control.RenderTransform = created;
        return created;
    }

    private static TranslateTransform EnsureTranslateTransform(Control control)
    {
        if (control.RenderTransform is TranslateTransform tt)
            return tt;

        var created = new TranslateTransform();
        control.RenderTransform = created;
        return created;
    }

    private static async Task AnimateDoubleAsync(
        Animatable target,
        AvaloniaProperty property,
        double startValue,
        double endValue,
        double durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            target.SetValue(property, endValue);
            return;
        }

        // Pre-set local value to endValue so FillMode.None lands cleanly with zero flicker
        target.SetValue(property, endValue);

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(durationSeconds),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(property, startValue) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(property, endValue) }
                }
            }
        };

        await animation.RunAsync(target);
    }
}
