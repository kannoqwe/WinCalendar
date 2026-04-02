using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using WinCalendar.Core.Abstractions;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerWeekDayTimelineViewModel : ObservableObject
{
    private const int InlineAllDayTaskLimit = 1;
    private bool _isSelected;

    public PlannerWeekDayTimelineViewModel(
        DateOnly date,
        IEnumerable<PlannerTaskViewModel> allDayTasks,
        IEnumerable<PlannerWeekTaskBlockViewModel> timedTaskBlocks,
        bool isToday,
        bool isSelected)
    {
        Date = date;
        IsToday = isToday;
        _isSelected = isSelected;
        List<PlannerTaskViewModel> allDayTaskList = allDayTasks.ToList();

        AllDayTasks = [];
        VisibleAllDayTasks = [];
        TimedTaskBlocks = [];

        foreach (PlannerTaskViewModel task in allDayTaskList)
            AllDayTasks.Add(task);

        foreach (PlannerTaskViewModel task in allDayTaskList.Take(InlineAllDayTaskLimit))
            VisibleAllDayTasks.Add(task);

        foreach (PlannerWeekTaskBlockViewModel taskBlock in timedTaskBlocks)
            TimedTaskBlocks.Add(taskBlock);
    }

    public DateOnly Date { get; }

    public bool IsToday { get; }

    public ObservableCollection<PlannerTaskViewModel> AllDayTasks { get; }

    public ObservableCollection<PlannerTaskViewModel> VisibleAllDayTasks { get; }

    public ObservableCollection<PlannerWeekTaskBlockViewModel> TimedTaskBlocks { get; }

    public string DayNameText => Date.ToString("ddd").ToUpperInvariant();

    public string DayNumberText => Date.Day.ToString();

    public string FullDateText => Date.ToString("dddd, dd MMMM");

    public Visibility SelectionVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    public Visibility TodayVisibility => IsToday ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyAllDayVisibility => AllDayTasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public int HiddenAllDayTaskCount => Math.Max(0, AllDayTasks.Count - VisibleAllDayTasks.Count);

    public string HiddenAllDayTaskCountText => $"+{HiddenAllDayTaskCount}";

    public Visibility HiddenAllDayTaskCountVisibility =>
        HiddenAllDayTaskCount == 0 ? Visibility.Collapsed : Visibility.Visible;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
                return;

            OnPropertyChanged(nameof(SelectionVisibility));
        }
    }
}
