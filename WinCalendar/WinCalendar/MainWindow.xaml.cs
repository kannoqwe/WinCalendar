using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinCalendar.Modules.Calendar.UI;
using WinCalendar.Modules.Planner.Presentation;
using WinCalendar.Modules.Planner.UI;
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
        private AppWindow? _appWindow;
        private FrameworkElement? _titleBarDragRegion;
        private FontIcon? _captionMaximizeIcon;

        public MainWindow(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
        {
            _plannerStateStore = plannerStateStore;
            _windowCoordinator = windowCoordinator;
            InitializeComponent();
            _titleBarDragRegion = ContentRoot.FindName("TitleBarDragRegion") as FrameworkElement;
            _captionMaximizeIcon = ContentRoot.FindName("CaptionMaximizeIcon") as FontIcon;
            ConfigureCustomTitleBar();
            ShellNavigationView.SelectedItem = TodayNavigationItem;
            PageHost.Content = new TodayPage(_plannerStateStore, _windowCoordinator);
            Closed += MainWindow_Closed;
        }

        private void ShellNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is not NavigationViewItem item || item.Tag is not string tag)
                return;

            PageHost.Content = tag switch
            {
                "calendar" => new CalendarPage(_plannerStateStore, _windowCoordinator),
                "notes" => new NotesPage(),
                "settings" => new SettingsPage(),
                _ => new TodayPage(_plannerStateStore, _windowCoordinator)
            };
        }

        private void CompactPanelButton_Click(object sender, RoutedEventArgs e)
        {
            _windowCoordinator.ShowCompactPanel();
        }

        private void MediumViewButton_Click(object sender, RoutedEventArgs e)
        {
            _windowCoordinator.ShowMediumView();
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
            _appWindow.Changed += AppWindow_Changed;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Transparent;
            titleBar.ButtonInactiveForegroundColor = Colors.Transparent;
            titleBar.ButtonHoverForegroundColor = Colors.Transparent;
            titleBar.ButtonPressedForegroundColor = Colors.Transparent;
            titleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            UpdateCaptionButtonIcons();
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!args.DidPresenterChange && !args.DidSizeChange)
                return;

            DispatcherQueue.TryEnqueue(UpdateCaptionButtonIcons);
        }

        private void UpdateCaptionButtonIcons()
        {
            if (_captionMaximizeIcon is null || _appWindow?.Presenter is not OverlappedPresenter presenter)
                return;

            _captionMaximizeIcon.Glyph = presenter.State == OverlappedPresenterState.Maximized
                ? "\uE923"
                : "\uE922";
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_appWindow is not null)
                _appWindow.Changed -= AppWindow_Changed;
        }
    }
}
