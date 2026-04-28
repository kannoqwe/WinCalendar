using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Calendar.UI;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;
using WinCalendar.Shared.Settings;
using WinCalendar.Shared.Windowing;

namespace WinCalendar
{
    /// <summary>
    /// Main application window hosting the full planner shell.
    /// </summary>
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private readonly PlannerStateStore _plannerStateStore;
        private readonly PlannerWindowCoordinator _windowCoordinator;
        private readonly AppSettingsStore _appSettingsStore;
        private readonly Action _beginOverlayResizeMode;
        private AppWindow? _appWindow;
        private FrameworkElement? _titleBarDragRegion;

        public MainWindow(
            PlannerStateStore plannerStateStore,
            PlannerWindowCoordinator windowCoordinator,
            AppSettingsStore appSettingsStore,
            Action beginOverlayResizeMode)
        {
            _plannerStateStore = plannerStateStore;
            _windowCoordinator = windowCoordinator;
            _appSettingsStore = appSettingsStore;
            _beginOverlayResizeMode = beginOverlayResizeMode;
            InitializeComponent();
            _titleBarDragRegion = ContentRoot.FindName("TitleBarDragRegion") as FrameworkElement;
            ApplyThemePreference();
            _appSettingsStore.ThemePreferenceChanged += AppSettingsStore_ThemePreferenceChanged;
            _appSettingsStore.OverlaySettingsChanged += AppSettingsStore_OverlaySettingsChanged;
            Closed += MainWindow_Closed;
            ConfigureCustomTitleBar();
            ShellNavigationView.SelectedItem = TodayNavigationItem;
            PageHost.Content = new TodayPage(_plannerStateStore, _windowCoordinator);
        }

        private void ShellNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                PageHost.Content = new SettingsPage(_appSettingsStore, _beginOverlayResizeMode);
                return;
            }

            if (args.SelectedItemContainer is not NavigationViewItem item || item.Tag is not string tag)
                return;

            PageHost.Content = tag switch
            {
                "calendar" => new CalendarPage(_plannerStateStore),
                _ => new TodayPage(_plannerStateStore, _windowCoordinator)
            };
        }

        private void AppSettingsStore_ThemePreferenceChanged(object? sender, System.EventArgs e)
        {
            ApplyThemePreference();
        }

        private void AppSettingsStore_OverlaySettingsChanged(object? sender, System.EventArgs e)
        {
            if (PageHost.Content is SettingsPage settingsPage)
                settingsPage.RefreshOverlaySettings();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _appSettingsStore.ThemePreferenceChanged -= AppSettingsStore_ThemePreferenceChanged;
            _appSettingsStore.OverlaySettingsChanged -= AppSettingsStore_OverlaySettingsChanged;
            Closed -= MainWindow_Closed;
        }

        private void ApplyThemePreference()
        {
            ElementTheme effectiveTheme = _appSettingsStore.ThemePreference switch
            {
                AppThemePreference.Light => ElementTheme.Light,
                AppThemePreference.Dark => ElementTheme.Dark,
                _ => _appSettingsStore.IsDarkThemeEffective ? ElementTheme.Dark : ElementTheme.Light
            };

            ContentRoot.RequestedTheme = effectiveTheme;
            PlannerTaskPalette.UseDarkPalette = effectiveTheme == ElementTheme.Dark;
        }

        private void ConfigureCustomTitleBar()
        {
            _appWindow = this.GetAppWindow();
            ExtendsContentIntoTitleBar = true;
            if (_titleBarDragRegion is not null)
                SetTitleBar(_titleBarDragRegion);

            if (!AppWindowTitleBar.IsCustomizationSupported() || _appWindow is null)
                return;

            AppWindowTitleBar titleBar = _appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(0x14, 0x00, 0x00, 0x00);
            titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(0x22, 0x00, 0x00, 0x00);
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonInactiveForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        }
    }
}
