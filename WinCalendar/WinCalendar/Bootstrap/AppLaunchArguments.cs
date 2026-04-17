using System;

namespace WinCalendar.Bootstrap;

internal static class AppLaunchArguments
{
    public static AppLaunchMode Parse(string? rawArguments)
    {
        if (ContainsArgument(rawArguments, "--background"))
            return AppLaunchMode.BackgroundShell;

        return AppLaunchMode.FullApp;
    }

    public static string ToPipePayload(AppLaunchMode launchMode) => launchMode switch
    {
        AppLaunchMode.BackgroundShell => "background",
        _ => "full"
    };

    public static bool TryParsePipePayload(string? payload, out AppLaunchMode launchMode)
    {
        switch (payload?.Trim().ToLowerInvariant())
        {
            case "background":
                launchMode = AppLaunchMode.BackgroundShell;
                return true;
            case "full":
                launchMode = AppLaunchMode.FullApp;
                return true;
            default:
                launchMode = AppLaunchMode.FullApp;
                return false;
        }
    }

    private static bool ContainsArgument(string? rawArguments, string argument) =>
        !string.IsNullOrWhiteSpace(rawArguments) &&
        rawArguments.Contains(argument, StringComparison.OrdinalIgnoreCase);
}
