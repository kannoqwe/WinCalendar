using System;
using Windows.Storage;

namespace WinCalendar.Shared.Settings;

public sealed class AppSettingsStore
{
    private const string ThemePreferenceKey = "ThemePreference";
    private const string TimeFormatPreferenceKey = "TimeFormatPreference";
    private const string WeekStartPreferenceKey = "WeekStartPreference";

    private readonly ApplicationDataContainer _localSettings;

    public AppSettingsStore()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public event EventHandler? ThemePreferenceChanged;

    public event EventHandler? TimeFormatPreferenceChanged;

    public event EventHandler? WeekStartPreferenceChanged;

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

    public AppTimeFormatPreference TimeFormatPreference
    {
        get => ReadEnum(TimeFormatPreferenceKey, AppTimeFormatPreference.TwentyFourHour);
        set
        {
            AppTimeFormatPreference currentValue = TimeFormatPreference;
            if (currentValue == value)
                return;

            _localSettings.Values[TimeFormatPreferenceKey] = value.ToString();
            TimeFormatPreferenceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AppWeekStartPreference WeekStartPreference
    {
        get => ReadEnum(WeekStartPreferenceKey, AppWeekStartPreference.Monday);
        set
        {
            AppWeekStartPreference currentValue = WeekStartPreference;
            if (currentValue == value)
                return;

            _localSettings.Values[WeekStartPreferenceKey] = value.ToString();
            WeekStartPreferenceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private AppThemePreference ReadThemePreference()
    {
        return ReadEnum(ThemePreferenceKey, AppThemePreference.System);
    }

    private TEnum ReadEnum<TEnum>(string key, TEnum defaultValue)
        where TEnum : struct
    {
        if (_localSettings.Values.TryGetValue(key, out object? value)
            && value is string text
            && Enum.TryParse(text, out TEnum parsedValue))
        {
            return parsedValue;
        }

        return defaultValue;
    }
}
