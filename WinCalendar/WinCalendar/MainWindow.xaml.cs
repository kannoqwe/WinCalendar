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

        public MainWindow(PlannerStateStore plannerStateStore, PlannerWindowCoordinator windowCoordinator)
        {
            _plannerStateStore = plannerStateStore;
            _windowCoordinator = windowCoordinator;
            InitializeComponent();
            ShellNavigationView.SelectedItem = TodayNavigationItem;
            PageHost.Content = new TodayPage(_plannerStateStore, _windowCoordinator);
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
    }
}
