using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Planner.App.Modules.Tasks.Entities;
using Windows.UI;

namespace WinCalendar.Modules.Planner.Presentation;

internal static class PlannerTaskPalette
{
    public static PlannerTaskTone GetTone(TaskCategory category)
    {
        bool useDarkPalette = Application.Current.RequestedTheme == ApplicationTheme.Dark;
        return category switch
        {
            TaskCategory.Personal => CreateTone(useDarkPalette, 245, 238, 255, 123, 97, 255, 53, 44, 88, 161, 137, 255),
            TaskCategory.Health => CreateTone(useDarkPalette, 233, 249, 239, 36, 157, 87, 33, 62, 44, 76, 205, 120),
            TaskCategory.Important => CreateTone(useDarkPalette, 255, 243, 224, 238, 132, 35, 82, 52, 30, 255, 171, 76),
            _ => CreateTone(useDarkPalette, 229, 242, 255, 38, 126, 224, 30, 54, 82, 95, 170, 255)
        };
    }

    private static PlannerTaskTone CreateTone(
        bool useDarkPalette,
        byte lightBackgroundRed,
        byte lightBackgroundGreen,
        byte lightBackgroundBlue,
        byte lightAccentRed,
        byte lightAccentGreen,
        byte lightAccentBlue,
        byte darkBackgroundRed,
        byte darkBackgroundGreen,
        byte darkBackgroundBlue,
        byte darkAccentRed,
        byte darkAccentGreen,
        byte darkAccentBlue)
    {
        Color background = useDarkPalette
            ? ColorHelper.FromArgb(255, darkBackgroundRed, darkBackgroundGreen, darkBackgroundBlue)
            : ColorHelper.FromArgb(255, lightBackgroundRed, lightBackgroundGreen, lightBackgroundBlue);
        Color accent = useDarkPalette
            ? ColorHelper.FromArgb(255, darkAccentRed, darkAccentGreen, darkAccentBlue)
            : ColorHelper.FromArgb(255, lightAccentRed, lightAccentGreen, lightAccentBlue);

        return new PlannerTaskTone(
            new SolidColorBrush(background),
            new SolidColorBrush(Blend(background, accent, useDarkPalette ? 0.34 : 0.24)),
            new SolidColorBrush(accent));
    }

    private static Color Blend(Color start, Color end, double amount)
    {
        return ColorHelper.FromArgb(
            255,
            BlendChannel(start.R, end.R, amount),
            BlendChannel(start.G, end.G, amount),
            BlendChannel(start.B, end.B, amount));
    }

    private static byte BlendChannel(byte start, byte end, double amount) =>
        (byte)(start + ((end - start) * amount));

    internal sealed record PlannerTaskTone(
        SolidColorBrush BackgroundBrush,
        SolidColorBrush BorderBrush,
        SolidColorBrush AccentBrush);
}
