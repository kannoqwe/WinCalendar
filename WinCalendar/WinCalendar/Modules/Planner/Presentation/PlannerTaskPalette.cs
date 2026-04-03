using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinCalendar.Modules.Planner.Presentation;

internal static class PlannerTaskPalette
{
    private static readonly double[] HueOffsets = [-28d, -14d, 0d, 12d, 26d, 42d];
    private static IReadOnlyList<PlannerTaskTone>? _lightTones;
    private static IReadOnlyList<PlannerTaskTone>? _darkTones;

    public static PlannerTaskTone GetTone(Guid taskId)
    {
        IReadOnlyList<PlannerTaskTone> tones = GetTones();
        int index = Math.Abs(GetStableHash(taskId)) % tones.Count;
        return tones[index];
    }

    private static IReadOnlyList<PlannerTaskTone> GetTones()
    {
        bool useDarkPalette = Application.Current.RequestedTheme == ApplicationTheme.Dark;

        if (useDarkPalette)
            return _darkTones ??= BuildTones(useDarkPalette);

        return _lightTones ??= BuildTones(useDarkPalette);
    }

    private static IReadOnlyList<PlannerTaskTone> BuildTones(bool useDarkPalette)
    {
        HslColor accent = ToHsl(ResolveAccentColor());
        List<PlannerTaskTone> tones = [];

        foreach (double hueOffset in HueOffsets)
            tones.Add(CreateTone(accent.Hue + hueOffset, useDarkPalette));

        return tones;
    }

    private static PlannerTaskTone CreateTone(double hue, bool useDarkPalette)
    {
        hue = NormalizeHue(hue);

        HslColor surface = useDarkPalette
            ? new(hue, 0.36, 0.22)
            : new(hue, 0.72, 0.96);
        HslColor border = useDarkPalette
            ? new(hue, 0.42, 0.34)
            : new(hue, 0.60, 0.84);
        HslColor accent = useDarkPalette
            ? new(hue, 0.62, 0.67)
            : new(hue, 0.58, 0.52);

        return new PlannerTaskTone(
            new SolidColorBrush(FromHsl(surface)),
            new SolidColorBrush(FromHsl(border)),
            new SolidColorBrush(FromHsl(accent)));
    }

    private static Color ResolveAccentColor()
    {
        if (Application.Current.Resources["AppAccentBrush"] is SolidColorBrush accentBrush)
            return accentBrush.Color;

        return ColorHelper.FromArgb(255, 255, 0, 136);
    }

    private static int GetStableHash(Guid taskId)
    {
        int hash = 17;

        foreach (byte value in taskId.ToByteArray())
            hash = (hash * 31) + value;

        return hash;
    }

    private static HslColor ToHsl(Color color)
    {
        double red = color.R / 255d;
        double green = color.G / 255d;
        double blue = color.B / 255d;
        double max = Math.Max(red, Math.Max(green, blue));
        double min = Math.Min(red, Math.Min(green, blue));
        double delta = max - min;
        double lightness = (max + min) / 2d;

        if (delta == 0)
            return new HslColor(0d, 0d, lightness);

        double saturation = lightness > 0.5
            ? delta / (2d - max - min)
            : delta / (max + min);

        double hue = max switch
        {
            _ when max == red => ((green - blue) / delta + (green < blue ? 6d : 0d)) * 60d,
            _ when max == green => ((blue - red) / delta + 2d) * 60d,
            _ => ((red - green) / delta + 4d) * 60d
        };

        return new HslColor(NormalizeHue(hue), saturation, lightness);
    }

    private static Color FromHsl(HslColor color)
    {
        double hue = NormalizeHue(color.Hue);
        double saturation = Math.Clamp(color.Saturation, 0d, 1d);
        double lightness = Math.Clamp(color.Lightness, 0d, 1d);

        if (saturation == 0)
        {
            byte channel = ToByte(lightness);
            return ColorHelper.FromArgb(255, channel, channel, channel);
        }

        double q = lightness < 0.5
            ? lightness * (1d + saturation)
            : lightness + saturation - lightness * saturation;
        double p = 2d * lightness - q;
        double normalizedHue = hue / 360d;

        double red = HueToRgb(p, q, normalizedHue + (1d / 3d));
        double green = HueToRgb(p, q, normalizedHue);
        double blue = HueToRgb(p, q, normalizedHue - (1d / 3d));

        return ColorHelper.FromArgb(255, ToByte(red), ToByte(green), ToByte(blue));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0d)
            t += 1d;

        if (t > 1d)
            t -= 1d;

        if (t < 1d / 6d)
            return p + (q - p) * 6d * t;

        if (t < 1d / 2d)
            return q;

        if (t < 2d / 3d)
            return p + (q - p) * ((2d / 3d) - t) * 6d;

        return p;
    }

    private static double NormalizeHue(double hue)
    {
        double normalizedHue = hue % 360d;
        return normalizedHue < 0d ? normalizedHue + 360d : normalizedHue;
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Round(Math.Clamp(value, 0d, 1d) * 255d);
    }

    internal sealed record PlannerTaskTone(
        SolidColorBrush BackgroundBrush,
        SolidColorBrush BorderBrush,
        SolidColorBrush AccentBrush);

    private sealed record HslColor(double Hue, double Saturation, double Lightness);
}
