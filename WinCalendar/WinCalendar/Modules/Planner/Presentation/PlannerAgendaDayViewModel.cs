using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using WinCalendar.Core.Abstractions;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerAgendaDayViewModel : ObservableObject
{
    private bool _isExpanded;
    private bool _isHidden;
    private bool _isSelected;

    public PlannerAgendaDayViewModel(
        DateOnly date,
        IEnumerable<PlannerTaskViewModel> tasks,
        bool isExpanded,
        bool isHidden,
        bool isSelected)
    {
        Date = date;
        _isExpanded = isExpanded;
        _isHidden = isHidden;
        _isSelected = isSelected;
        Tasks = [];

        foreach (PlannerTaskViewModel task in tasks)
            Tasks.Add(task);
    }

    public DateOnly Date { get; }

    public ObservableCollection<PlannerTaskViewModel> Tasks { get; }

    public string HeaderText => PlannerDateTimeFormatter.FormatShortDay(Date);

    public string SummaryText => Tasks.Count == 0 ? "No tasks" : $"{Tasks.Count} task{(Tasks.Count == 1 ? string.Empty : "s")}";

    public Visibility ContentVisibility => !_isHidden && _isExpanded ? Visibility.Visible : Visibility.Collapsed;

    public Visibility HiddenVisibility => _isHidden ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ExpandedToggleVisibility => _isHidden ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SelectionVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;

    public string ExpandButtonText => _isExpanded ? "Collapse" : "Expand";

    public string HideButtonText => _isHidden ? "Show" : "Hide";

    public string CompactExpandButtonText => _isExpanded ? "-" : "+";

    public string CountBadgeText => _isHidden ? "H" : Tasks.Count.ToString();

    public string CompactPreviewText
    {
        get
        {
            if (_isHidden)
                return "Day hidden";

            if (Tasks.Count == 0)
                return "No tasks";

            if (!_isExpanded)
                return SummaryText;

            PlannerTaskViewModel firstTask = Tasks[0];
            return firstTask.HasTime
                ? $"{firstTask.TimeText} {firstTask.Title}"
                : firstTask.Title;
        }
    }

    public string CompactOverflowText =>
        !_isHidden && _isExpanded && Tasks.Count > 1 ? $"+{Tasks.Count - 1} more" : string.Empty;

    public Visibility CompactOverflowVisibility =>
        string.IsNullOrEmpty(CompactOverflowText) ? Visibility.Collapsed : Visibility.Visible;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (!SetProperty(ref _isExpanded, value))
                return;

            OnPropertyChanged(nameof(ContentVisibility));
            OnPropertyChanged(nameof(ExpandButtonText));
            OnPropertyChanged(nameof(CompactExpandButtonText));
            OnPropertyChanged(nameof(CompactPreviewText));
            OnPropertyChanged(nameof(CompactOverflowText));
            OnPropertyChanged(nameof(CompactOverflowVisibility));
        }
    }

    public bool IsHidden
    {
        get => _isHidden;
        set
        {
            if (!SetProperty(ref _isHidden, value))
                return;

            OnPropertyChanged(nameof(ContentVisibility));
            OnPropertyChanged(nameof(HiddenVisibility));
            OnPropertyChanged(nameof(ExpandedToggleVisibility));
            OnPropertyChanged(nameof(HideButtonText));
            OnPropertyChanged(nameof(CountBadgeText));
            OnPropertyChanged(nameof(CompactPreviewText));
            OnPropertyChanged(nameof(CompactOverflowText));
            OnPropertyChanged(nameof(CompactOverflowVisibility));
        }
    }

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
