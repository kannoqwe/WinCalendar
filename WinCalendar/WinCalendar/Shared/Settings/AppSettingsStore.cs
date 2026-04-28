using System;
using Microsoft.Win32;
using Windows.Storage;

namespace WinCalendar.Shared.Settings;

public sealed class AppSettingsStore
{
    private const string ThemePreferenceKey = "ThemePreference";
    private const string TimeFormatPreferenceKey = "TimeFormatPreference";
    private const string WeekStartPreferenceKey = "WeekStartPreference";
    private const string OverlayEnabledKey = "OverlayEnabled";
    private const string OverlayWidthKey = "OverlayWidth";
    private const string OverlayHeightKey = "OverlayHeight";
    private const string OverlayRightOffsetKey = "OverlayRightOffset";
    private const string OverlayBottomOffsetKey = "OverlayBottomOffset";
    public const int DefaultOverlayWidth = 140;
    public const int DefaultOverlayHeight = 52;
    public const int DefaultOverlayRightOffset = 4;
    public const int DefaultOverlayBottomOffset = 0;
    public const int MinimumOverlayWidth = 80;
    public const int MinimumOverlayHeight = 32;
    public const int MaximumOverlayWidth = 420;
    public const int MaximumOverlayHeight = 180;
    public const int MinimumOverlayOffset = 0;
    public const int MaximumOverlayOffset = 2000;

    private readonly ApplicationDataContainer _localSettings;

    public AppSettingsStore()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public event EventHandler? ThemePreferenceChanged;

    public event EventHandler? TimeFormatPreferenceChanged;

    public event EventHandler? WeekStartPreferenceChanged;

    public event EventHandler? OverlaySettingsChanged;

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

    public bool OverlayEnabled
    {
        get => ReadBool(OverlayEnabledKey, true);
        set
        {
            bool currentValue = OverlayEnabled;
            if (currentValue == value)
                return;

            _localSettings.Values[OverlayEnabledKey] = value;
            OverlaySettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int OverlayWidth
    {
        get => ReadInt(OverlayWidthKey, DefaultOverlayWidth, MinimumOverlayWidth, MaximumOverlayWidth);
        set => SetOverlaySize(value, OverlayHeight);
    }

    public int OverlayHeight
    {
        get => ReadInt(OverlayHeightKey, DefaultOverlayHeight, MinimumOverlayHeight, MaximumOverlayHeight);
        set => SetOverlaySize(OverlayWidth, value);
    }

    public int OverlayRightOffset =>
        ReadInt(OverlayRightOffsetKey, DefaultOverlayRightOffset, MinimumOverlayOffset, MaximumOverlayOffset);

    public int OverlayBottomOffset =>
        ReadInt(OverlayBottomOffsetKey, DefaultOverlayBottomOffset, MinimumOverlayOffset, MaximumOverlayOffset);

    public bool IsDarkThemeEffective => ThemePreference switch
    {
        AppThemePreference.Dark => true,
        AppThemePreference.Light => false,
        _ => IsSystemAppThemeDark()
    };

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

    public void SetOverlaySize(int width, int height)
    {
        SetOverlayBounds(width, height, OverlayRightOffset, OverlayBottomOffset);
    }

    public void SetOverlayBounds(int width, int height, int rightOffset, int bottomOffset)
    {
        int clampedWidth = Math.Clamp(width, MinimumOverlayWidth, MaximumOverlayWidth);
        int clampedHeight = Math.Clamp(height, MinimumOverlayHeight, MaximumOverlayHeight);
        int clampedRightOffset = Math.Clamp(rightOffset, MinimumOverlayOffset, MaximumOverlayOffset);
        int clampedBottomOffset = Math.Clamp(bottomOffset, MinimumOverlayOffset, MaximumOverlayOffset);

        if (OverlayWidth == clampedWidth
            && OverlayHeight == clampedHeight
            && OverlayRightOffset == clampedRightOffset
            && OverlayBottomOffset == clampedBottomOffset)
        {
            return;
        }

        _localSettings.Values[OverlayWidthKey] = clampedWidth;
        _localSettings.Values[OverlayHeightKey] = clampedHeight;
        _localSettings.Values[OverlayRightOffsetKey] = clampedRightOffset;
        _localSettings.Values[OverlayBottomOffsetKey] = clampedBottomOffset;
        OverlaySettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool ReadBool(string key, bool defaultValue)
    {
        if (_localSettings.Values.TryGetValue(key, out object? value)
            && value is bool boolValue)
        {
            return boolValue;
        }

        return defaultValue;
    }

    private int ReadInt(string key, int defaultValue, int minimumValue, int maximumValue)
    {
        if (_localSettings.Values.TryGetValue(key, out object? value)
            && value is int intValue)
        {
            return Math.Clamp(intValue, minimumValue, maximumValue);
        }

        return defaultValue;
    }

    private static bool IsSystemAppThemeDark()
    {
        object? value = Registry.CurrentUser
            .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
            ?.GetValue("AppsUseLightTheme");

        return value is int appsUseLightTheme && appsUseLightTheme == 0;
    }
}
