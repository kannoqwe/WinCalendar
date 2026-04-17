using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinCalendar.Core.Abstractions;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerMonthDayViewModel : ObservableObject
{
    private readonly bool _hasTasks;
    private readonly bool _allCompleted;
    private bool _isSelected;

    public PlannerMonthDayViewModel(
        DateOnly date,
        bool isCurrentMonth,
        bool isToday,
        bool isSelected,
        bool hasTasks,
        bool allCompleted)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = isToday;
        _isSelected = isSelected;
        _hasTasks = hasTasks;
        _allCompleted = allCompleted;
        IndicatorBrush = new SolidColorBrush(
            allCompleted
                ? Colors.SeaGreen
                : ColorHelper.FromArgb(255, 255, 0, 136));
    }

    public DateOnly Date { get; }

    public bool IsCurrentMonth { get; }

    public bool IsToday { get; }

    public string DayNumberText => Date.Day.ToString();

    public string DayButtonText => Date.Day.ToString();

    public double DayOpacity => IsCurrentMonth ? 1.0 : 0.42;

    public SolidColorBrush IndicatorBrush { get; }

    public Visibility TaskIndicatorVisibility => _hasTasks ? Visibility.Visible : Visibility.Collapsed;

    public Visibility TodayVisibility => IsToday ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SelectionVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    public string StateText => !_hasTasks ? "No tasks" : _allCompleted ? "Done" : "Planned";

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
