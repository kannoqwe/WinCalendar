using System;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Shared.Settings;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class SettingsPage : Page
{
    private readonly AppSettingsStore _appSettingsStore;
    private bool _isInitializing;

    public SettingsPage(AppSettingsStore appSettingsStore)
    {
        _appSettingsStore = appSettingsStore;
        InitializeComponent();
        SelectPreferences();
    }

    private void SelectPreferences()
    {
        _isInitializing = true;

        try
        {
            SelectComboBoxItem(ThemeComboBox, _appSettingsStore.ThemePreference.ToString());
            SelectComboBoxItem(TimeFormatComboBox, _appSettingsStore.TimeFormatPreference.ToString());
            SelectComboBoxItem(WeekStartComboBox, _appSettingsStore.WeekStartPreference.ToString());
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing
            || ThemeComboBox.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !Enum.TryParse(tag, out AppThemePreference themePreference))
        {
            return;
        }

        _appSettingsStore.ThemePreference = themePreference;
    }

    private void TimeFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing
            || TimeFormatComboBox.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !Enum.TryParse(tag, out AppTimeFormatPreference timeFormatPreference))
        {
            return;
        }

        _appSettingsStore.TimeFormatPreference = timeFormatPreference;
    }

    private void WeekStartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing
            || WeekStartComboBox.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !Enum.TryParse(tag, out AppWeekStartPreference weekStartPreference))
        {
            return;
        }

        _appSettingsStore.WeekStartPreference = weekStartPreference;
    }

    private static void SelectComboBoxItem(ComboBox comboBox, string selectedTag)
    {
        foreach (object item in comboBox.Items)
        {
            if (item is ComboBoxItem { Tag: string tag } comboBoxItem
                && tag == selectedTag)
            {
                comboBox.SelectedItem = comboBoxItem;
                return;
            }
        }
    }
}
