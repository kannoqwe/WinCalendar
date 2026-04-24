using System;
using Windows.Storage;

namespace WinCalendar.Shared.Settings;

public sealed class AppSettingsStore
{
    private const string ThemePreferenceKey = "ThemePreference";

    private readonly ApplicationDataContainer _localSettings;

    public AppSettingsStore()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public event EventHandler? ThemePreferenceChanged;

    public AppThemePreference ThemePreference
    {
        get => ReadThemePreference();
        set
        {
            AppThemePreference currentValue = ReadThemePreference();
            if (currentValue == value)
                return;

            _localSettings.Values[ThemePreferenceKey] = value.ToString();
            ThemePreferenceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private AppThemePreference ReadThemePreference()
    {
        if (_localSettings.Values.TryGetValue(ThemePreferenceKey, out object? value)
            && value is string text
            && Enum.TryParse(text, out AppThemePreference themePreference))
        {
            return themePreference;
        }

        return AppThemePreference.System;
    }
}
