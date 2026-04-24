using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Planner.App.Modules.Tasks.Entities;
using WinCalendar.Core.Abstractions;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

internal sealed class PlannerTaskEditorState : ObservableObject
{
    private const int EditorDefaultDurationMinutes = 15;

    private string _editorTitle = string.Empty;
    private string _editorDescription = string.Empty;
    private bool _editorHasTime;
    private bool _editorHasDuration;
    private TimeSpan _editorTime = new(9, 0, 0);
    private double _editorDurationMinutes = PlannerWeekTimelineLayout.DefaultTaskDurationMinutes;
    private DateTimeOffset _editorDate;
    private TaskRecurrencePattern _editorRecurrencePattern;
    private TaskCategory _editorCategory = TaskCategory.Work;

    public PlannerTaskEditorState(DateOnly initialDate)
    {
        _editorDate = new DateTimeOffset(initialDate.ToDateTime(TimeOnly.MinValue));
        EditorDurationOptions = [];
        UpdateEditorDurationOptions();
    }

    public ObservableCollection<PlannerEditorDurationOptionViewModel> EditorDurationOptions { get; }

    public string EditorTitle
    {
        get => _editorTitle;
        set => SetProperty(ref _editorTitle, value);
    }

    public string EditorDescription
    {
        get => _editorDescription;
        set => SetProperty(ref _editorDescription, value);
    }

    public bool EditorHasTime
    {
        get => _editorHasTime;
        set
        {
            if (!SetProperty(ref _editorHasTime, value))
                return;

            if (value && !EditorHasDuration)
                EditorHasDuration = true;

            if (!value && EditorHasDuration)
                EditorHasDuration = false;

            ClampEditorDuration();
            UpdateEditorDurationOptions();
            OnPropertyChanged(nameof(EditorIsAllDay));
            OnPropertyChanged(nameof(EditorTimeInputEnabled));
            OnPropertyChanged(nameof(EditorDurationToggleEnabled));
            OnPropertyChanged(nameof(EditorDurationInputEnabled));
            OnPropertyChanged(nameof(EditorTimeVisibility));
            OnPropertyChanged(nameof(EditorDurationVisibility));
            OnPropertyChanged(nameof(EditorDurationText));
            OnPropertyChanged(nameof(EditorMaxDurationMinutes));
        }
    }

    public TimeSpan EditorTime
    {
        get => _editorTime;
        set
        {
            if (!SetProperty(ref _editorTime, value))
                return;

            ClampEditorDuration();
            UpdateEditorDurationOptions();
            OnPropertyChanged(nameof(EditorMaxDurationMinutes));
            OnPropertyChanged(nameof(EditorTimeText));
            OnPropertyChanged(nameof(EditorDurationText));
        }
    }

    public bool EditorHasDuration
    {
        get => _editorHasDuration;
        set
        {
            bool normalizedValue = EditorHasTime && value;

            if (!SetProperty(ref _editorHasDuration, normalizedValue))
                return;

            ClampEditorDuration();
            OnPropertyChanged(nameof(EditorDurationInputEnabled));
            OnPropertyChanged(nameof(EditorDurationVisibility));
        }
    }

    public double EditorDurationMinutes
    {
        get => _editorDurationMinutes;
        set
        {
            if (!SetProperty(ref _editorDurationMinutes, NormalizeDurationValue(value, EditorMaxDurationMinutes)))
                return;

            OnPropertyChanged(nameof(EditorDurationText));
        }
    }

    public DateTimeOffset EditorDate
    {
        get => _editorDate;
        set
        {
            if (!SetProperty(ref _editorDate, value))
                return;

            OnPropertyChanged(nameof(EditorDateText));
        }
    }

    public TaskRecurrencePattern EditorRecurrencePattern
    {
        get => _editorRecurrencePattern;
        set
        {
            if (!SetProperty(ref _editorRecurrencePattern, value))
                return;

            OnPropertyChanged(nameof(EditorRecurrenceText));
        }
    }

    public TaskCategory EditorCategory
    {
        get => _editorCategory;
        set
        {
            if (!SetProperty(ref _editorCategory, value))
                return;

            OnPropertyChanged(nameof(EditorCategoryText));
        }
    }

    public bool EditorIsAllDay
    {
        get => !EditorHasTime;
        set => EditorHasTime = !value;
    }

    public bool EditorTimeInputEnabled => EditorHasTime;

    public bool EditorDurationToggleEnabled => EditorHasTime;

    public bool EditorDurationInputEnabled => EditorHasTime && EditorHasDuration;

    public Visibility EditorTimeVisibility =>
        EditorHasTime ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EditorDurationVisibility =>
        EditorHasTime && EditorHasDuration ? Visibility.Visible : Visibility.Collapsed;

    public string EditorTimeText => $"{EditorTime.Hours:00}:{EditorTime.Minutes:00}";

    public string EditorDurationText => FormatEditorDurationDescription((int)NormalizeDurationValue(EditorDurationMinutes, EditorMaxDurationMinutes));

    public string EditorDateText => PlannerDateTimeFormatter.FormatDate(DateOnly.FromDateTime(EditorDate.Date));

    public string EditorRecurrenceText => FormatRecurrence(EditorRecurrencePattern);

    public string EditorCategoryText => FormatCategory(EditorCategory);

    public double EditorMaxDurationMinutes =>
        EditorHasTime
            ? GetMaxDurationMinutes(TimeOnly.FromTimeSpan(EditorTime))
            : 24 * 60;

    public void BeginNewTaskDraft(DateOnly date, TimeOnly? time = null)
    {
        EditorTitle = string.Empty;
        EditorDescription = string.Empty;
        EditorDate = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
        EditorTime = (time ?? new TimeOnly(9, 0)).ToTimeSpan();
        EditorDurationMinutes = EditorDefaultDurationMinutes;
        EditorHasTime = time is not null;
        EditorRecurrencePattern = TaskRecurrencePattern.None;
        EditorCategory = TaskCategory.Work;
    }

    public void Reset(DateOnly selectedDate)
    {
        EditorTitle = string.Empty;
        EditorDescription = string.Empty;
        EditorTime = new TimeSpan(9, 0, 0);
        EditorDurationMinutes = EditorDefaultDurationMinutes;
        EditorHasTime = false;
        EditorDate = new DateTimeOffset(selectedDate.ToDateTime(TimeOnly.MinValue));
        EditorRecurrencePattern = TaskRecurrencePattern.None;
        EditorCategory = TaskCategory.Work;
    }

    public void LoadTask(PlannerTaskViewModel task)
    {
        EditorTitle = task.Title;
        EditorDescription = task.Description;
        EditorTime = task.Time?.ToTimeSpan() ?? new TimeSpan(9, 0, 0);
        EditorDurationMinutes = task.DurationMinutes ?? EditorDefaultDurationMinutes;
        EditorHasTime = task.HasTime;
        EditorDate = new DateTimeOffset(task.Date.ToDateTime(TimeOnly.MinValue));
        EditorRecurrencePattern = task.RecurrencePattern;
        EditorCategory = task.Category;
    }

    private void ClampEditorDuration()
    {
        double normalizedDuration = NormalizeDurationValue(_editorDurationMinutes, EditorMaxDurationMinutes);

        if (Math.Abs(normalizedDuration - _editorDurationMinutes) < double.Epsilon)
            return;

        _editorDurationMinutes = normalizedDuration;
        OnPropertyChanged(nameof(EditorDurationMinutes));
        OnPropertyChanged(nameof(EditorDurationText));
    }

    private static double NormalizeDurationValue(double value, double maxDurationMinutes)
    {
        double normalizedValue = double.IsNaN(value)
            ? EditorDefaultDurationMinutes
            : Math.Round(value / 15d) * 15d;

        return Math.Clamp(normalizedValue, 15d, maxDurationMinutes);
    }

    private void UpdateEditorDurationOptions()
    {
        EditorDurationOptions.Clear();

        TimeOnly startTime = TimeOnly.FromTimeSpan(EditorTime);
        int maxDurationMinutes = (int)GetMaxDurationMinutes(startTime);

        for (int duration = 15; duration <= maxDurationMinutes; duration += 15)
        {
            int endMinutes = Math.Min(24 * 60, (int)startTime.ToTimeSpan().TotalMinutes + duration);
            string endTimeText = endMinutes == 24 * 60
                ? "24:00"
                : TimeOnly.MinValue.Add(TimeSpan.FromMinutes(endMinutes)).ToString("HH:mm");

            EditorDurationOptions.Add(new PlannerEditorDurationOptionViewModel(
                duration,
                endTimeText,
                FormatEditorDurationDescription(duration)));
        }
    }

    private static string FormatEditorDurationDescription(int durationMinutes)
    {
        int hours = durationMinutes / 60;
        int minutes = durationMinutes % 60;

        if (hours == 0)
            return $"{minutes} min";

        if (minutes == 0)
            return hours == 1 ? "1 hour" : $"{hours} hours";

        string hourText = hours == 1 ? "1 hour" : $"{hours} hours";
        return $"{hourText} {minutes} min";
    }

    private static double GetMaxDurationMinutes(TimeOnly time)
    {
        return (24 * 60) - time.ToTimeSpan().TotalMinutes;
    }

    private static string FormatRecurrence(TaskRecurrencePattern recurrencePattern)
    {
        return recurrencePattern switch
        {
            TaskRecurrencePattern.Daily => "Daily",
            TaskRecurrencePattern.Weekly => "Weekly",
            TaskRecurrencePattern.Monthly => "Monthly",
            _ => "Does not repeat"
        };
    }

    private static string FormatCategory(TaskCategory category)
    {
        return category switch
        {
            TaskCategory.Personal => "Personal",
            TaskCategory.Health => "Health",
            TaskCategory.Important => "Important",
            _ => "Work"
        };
    }
}
