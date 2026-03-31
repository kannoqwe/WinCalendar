using System;
using System.IO;

namespace WinCalendar.Bootstrap;

internal static class AppPaths
{
    public static string GetDatabasePath()
    {
        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinCalendar");

        Directory.CreateDirectory(appDataDirectory);

        return Path.Combine(appDataDirectory, "wincalendar.db");
    }
}
