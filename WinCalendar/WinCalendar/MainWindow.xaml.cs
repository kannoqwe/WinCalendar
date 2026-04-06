using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
        private TextBlock? _currentSectionTextBlock;

        public MainWindow(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
        {
            _plannerStateStore = plannerStateStore;
            _windowCoordinator = windowCoordinator;
            InitializeComponent();
            _titleBarDragRegion = ContentRoot.FindName("TitleBarDragRegion") as FrameworkElement;
            _currentSectionTextBlock = ContentRoot.FindName("CurrentSectionTextBlock") as TextBlock;
            ConfigureCustomTitleBar();
            ShellNavigationView.SelectedItem = TodayNavigationItem;
            PageHost.Content = new TodayPage(_plannerStateStore, _windowCoordinator);
            UpdateCurrentSectionTitle(TodayNavigationItem);
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

            UpdateCurrentSectionTitle(item);
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
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = ResolveBrushColor("AppTextMutedBrush", Colors.Gray);
            titleBar.ButtonInactiveForegroundColor = ResolveBrushColor("AppTextMutedBrush", Colors.Gray);
            titleBar.ButtonHoverForegroundColor = ResolveBrushColor("TextFillColorPrimaryBrush", Colors.Black);
            titleBar.ButtonPressedForegroundColor = ResolveBrushColor("TextFillColorPrimaryBrush", Colors.Black);
            titleBar.ButtonHoverBackgroundColor = ResolveColor(0x12, 0x00, 0x00, 0x00);
            titleBar.ButtonPressedBackgroundColor = ResolveColor(0x1E, 0x00, 0x00, 0x00);
            titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        }

        private void UpdateCurrentSectionTitle(NavigationViewItem item)
        {
            if (_currentSectionTextBlock is not null)
                _currentSectionTextBlock.Text = item.Content?.ToString() ?? "WinCalendar";
        }

        private static Windows.UI.Color ResolveColor(byte alpha, byte red, byte green, byte blue)
        {
            return Windows.UI.Color.FromArgb(alpha, red, green, blue);
        }

        private static Windows.UI.Color ResolveBrushColor(string resourceKey, Windows.UI.Color fallback)
        {
            object? resource = Application.Current.Resources[resourceKey];
            return resource is SolidColorBrush brush ? brush.Color : fallback;
        }
    }
}
