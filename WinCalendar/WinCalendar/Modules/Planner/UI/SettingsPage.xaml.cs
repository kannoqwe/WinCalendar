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
        SelectThemePreference();
    }

    private void SelectThemePreference()
    {
        _isInitializing = true;

        try
        {
            string selectedTag = _appSettingsStore.ThemePreference.ToString();

            foreach (object item in ThemeComboBox.Items)
            {
                if (item is ComboBoxItem { Tag: string tag } comboBoxItem
                    && tag == selectedTag)
                {
                    ThemeComboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
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
}
