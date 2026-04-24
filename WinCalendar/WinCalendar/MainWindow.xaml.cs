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
        private AppWindow? _appWindow;
        private FrameworkElement? _titleBarDragRegion;

        public MainWindow(
            PlannerStateStore plannerStateStore,
            PlannerWindowCoordinator windowCoordinator,
            AppSettingsStore appSettingsStore)
        {
            _plannerStateStore = plannerStateStore;
            _windowCoordinator = windowCoordinator;
            _appSettingsStore = appSettingsStore;
            InitializeComponent();
            _titleBarDragRegion = ContentRoot.FindName("TitleBarDragRegion") as FrameworkElement;
            ApplyThemePreference();
            _appSettingsStore.ThemePreferenceChanged += AppSettingsStore_ThemePreferenceChanged;
            Closed += MainWindow_Closed;
            ConfigureCustomTitleBar();
            ShellNavigationView.SelectedItem = TodayNavigationItem;
            PageHost.Content = new TodayPage(_plannerStateStore, _windowCoordinator);
        }

        private void ShellNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is not NavigationViewItem item || item.Tag is not string tag)
                return;

            PageHost.Content = tag switch
            {
                "calendar" => new CalendarPage(_plannerStateStore),
                "settings" => new SettingsPage(_appSettingsStore),
                _ => new TodayPage(_plannerStateStore, _windowCoordinator)
            };
        }

        private void AppSettingsStore_ThemePreferenceChanged(object? sender, System.EventArgs e)
        {
            ApplyThemePreference();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _appSettingsStore.ThemePreferenceChanged -= AppSettingsStore_ThemePreferenceChanged;
            Closed -= MainWindow_Closed;
        }

        private void ApplyThemePreference()
        {
            ContentRoot.RequestedTheme = _appSettingsStore.ThemePreference switch
            {
                AppThemePreference.Light => ElementTheme.Light,
                AppThemePreference.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
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
