using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinCalendar.Modules.Planner.Presentation;

namespace WinCalendar.Modules.Planner.UI;

public sealed partial class CompactPlannerPage : Page
{
    private static readonly Color AccentColor = ColorHelper.FromArgb(255, 255, 0, 255);
    private static readonly Color[] TaskDensityColors = [AccentColor];
    private static readonly Color[] EmptyDensityColors = [];

    private readonly PlannerStateStore _plannerStateStore;
    private bool _initialized;
    private bool _isSynchronizingCalendarSelection;

    public CompactPlannerPage(PlannerStateStore plannerStateStore)
    {
        _plannerStateStore = plannerStateStore;
        InitializeComponent();
        DataContext = _plannerStateStore;
        ConfigureCalendarVisuals();
        _plannerStateStore.MonthDays.CollectionChanged += MonthDays_CollectionChanged;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await _plannerStateStore.EnsureInitializedAsync();

        if (_plannerStateStore.SelectedDate != _plannerStateStore.Today)
            await _plannerStateStore.GoToTodayAsync();

        SelectTodayInCalendar();
        HideCalendarHeaderAndNavigation();
        RefreshVisibleDayItems();
    }

    private void Root_Unloaded(object sender, RoutedEventArgs e)
    {
        _plannerStateStore.MonthDays.CollectionChanged -= MonthDays_CollectionChanged;
    }

    private void CompactCalendarView_CalendarViewDayItemChanging(
        CalendarView sender,
        CalendarViewDayItemChangingEventArgs args)
    {
        UpdateDayItem(args.Item);
    }

    private void CompactCalendarView_SelectedDatesChanged(
        CalendarView sender,
        CalendarViewSelectedDatesChangedEventArgs args)
    {
        if (_isSynchronizingCalendarSelection)
            return;

        SelectTodayInCalendar();
    }

    private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        await TaskComposerDialogService.ShowAddTaskAsync(XamlRoot, _plannerStateStore, _plannerStateStore.Today);

        if (_plannerStateStore.SelectedDate != _plannerStateStore.Today)
            await _plannerStateStore.GoToTodayAsync();

        SelectTodayInCalendar();
        RefreshVisibleDayItems();
    }

    private void MonthDays_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_initialized)
            return;

        _ = DispatcherQueue.TryEnqueue(RefreshVisibleDayItems);
    }

    private void ConfigureCalendarVisuals()
    {
        SolidColorBrush accentBrush = new(AccentColor);
        SolidColorBrush whiteBrush = new(Colors.White);
        SolidColorBrush transparentBrush = new(Colors.Transparent);

        CompactCalendarView.Background = transparentBrush;
        CompactCalendarView.BorderBrush = transparentBrush;
        CompactCalendarView.BorderThickness = new Thickness(0);
        CompactCalendarView.CalendarItemBackground = transparentBrush;
        CompactCalendarView.CalendarItemBorderBrush = transparentBrush;
        CompactCalendarView.DayItemFontFamily = new FontFamily("Segoe UI Variable Text");
        CompactCalendarView.DayItemFontSize = 14;
        CompactCalendarView.DayItemMargin = new Thickness(0, 2, 0, 0);
        CompactCalendarView.SelectedBorderBrush = accentBrush;
        CompactCalendarView.SelectedForeground = whiteBrush;
        CompactCalendarView.SelectedHoverBorderBrush = accentBrush;
        CompactCalendarView.SelectedHoverForeground = whiteBrush;
        CompactCalendarView.SelectedPressedBorderBrush = accentBrush;
        CompactCalendarView.SelectedPressedForeground = whiteBrush;
        CompactCalendarView.TodayBackground = accentBrush;
        CompactCalendarView.TodayForeground = whiteBrush;
        CompactCalendarView.TodayFontWeight = FontWeights.SemiBold;
        CompactCalendarView.TodayHoverBackground = accentBrush;
        CompactCalendarView.TodayPressedBackground = accentBrush;
        CompactCalendarView.TodaySelectedInnerBorderBrush = whiteBrush;
    }

    private void HideCalendarHeaderAndNavigation()
    {
        CompactCalendarView.ApplyTemplate();

        CollapseNamedCalendarElement("HeaderButton");
        CollapseNamedCalendarElement("PreviousButton");
        CollapseNamedCalendarElement("NextButton");

        foreach (Border border in FindDescendants<Border>(CompactCalendarView))
        {
            if (Math.Abs(border.Height - 1) < 0.1)
                border.Visibility = Visibility.Collapsed;
        }
    }

    private void CollapseNamedCalendarElement(string name)
    {
        FrameworkElement? element = FindDescendantByName(CompactCalendarView, name);
        if (element is not null)
            element.Visibility = Visibility.Collapsed;
    }

    private void SelectTodayInCalendar()
    {
        DateTimeOffset today = ToDateTimeOffset(_plannerStateStore.Today);
        _isSynchronizingCalendarSelection = true;

        try
        {
            CompactCalendarView.SetDisplayDate(today);
            CompactCalendarView.SelectedDates.Clear();
            CompactCalendarView.SelectedDates.Add(today);
        }
        finally
        {
            _isSynchronizingCalendarSelection = false;
        }
    }

    private void RefreshVisibleDayItems()
    {
        foreach (CalendarViewDayItem item in FindDescendants<CalendarViewDayItem>(CompactCalendarView))
            UpdateDayItem(item);
    }

    private void UpdateDayItem(CalendarViewDayItem item)
    {
        DateOnly date = DateOnly.FromDateTime(item.Date.Date);
        item.SetDensityColors(_plannerStateStore.HasTasksOn(date) ? TaskDensityColors : EmptyDensityColors);
    }

    private static DateTimeOffset ToDateTimeOffset(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < childCount; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);

            if (child is T typedChild)
                yield return typedChild;

            foreach (T descendant in FindDescendants<T>(child))
                yield return descendant;
        }
    }

    private static FrameworkElement? FindDescendantByName(DependencyObject root, string name)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < childCount; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);

            if (child is FrameworkElement { Name: var elementName } frameworkElement && elementName == name)
                return frameworkElement;

            FrameworkElement? descendant = FindDescendantByName(child, name);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }
}
