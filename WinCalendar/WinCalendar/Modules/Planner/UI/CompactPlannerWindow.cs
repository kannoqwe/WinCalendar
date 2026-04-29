using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Shared.Settings;

namespace WinCalendar.Modules.Planner.UI;

public sealed class CompactPlannerWindow : Window
{
    private readonly CompactPlannerPage _page;
    private readonly AppSettingsStore _appSettingsStore;

    public CompactPlannerWindow(
        PlannerStateStore plannerStateStore,
        AppSettingsStore appSettingsStore,
        Action openMainApp)
    {
        _appSettingsStore = appSettingsStore;
        Title = "WinCalendar";
        SystemBackdrop = new DesktopAcrylicBackdrop();
        _page = new CompactPlannerPage(plannerStateStore, openMainApp);
        Content = _page;
        ApplyThemePreference();
        _appSettingsStore.ThemePreferenceChanged += AppSettingsStore_ThemePreferenceChanged;
        Closed += CompactPlannerWindow_Closed;
    }

    public Task PrepareForShowAsync() => _page.PrepareForShowAsync();

    public void PlayOpenAnimation() => _page.PlayOpenAnimation();

    private void AppSettingsStore_ThemePreferenceChanged(object? sender, EventArgs e)
    {
        ApplyThemePreference();
    }

    private void CompactPlannerWindow_Closed(object sender, WindowEventArgs args)
    {
        _appSettingsStore.ThemePreferenceChanged -= AppSettingsStore_ThemePreferenceChanged;
        Closed -= CompactPlannerWindow_Closed;
    }

    private void ApplyThemePreference()
    {
        _page.RequestedTheme = _appSettingsStore.ThemePreference switch
        {
            AppThemePreference.Light => ElementTheme.Light,
            AppThemePreference.Dark => ElementTheme.Dark,
            _ => _appSettingsStore.IsDarkThemeEffective ? ElementTheme.Dark : ElementTheme.Light
        };
    }
}
