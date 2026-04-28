using Planner.App.Modules.Tasks.Entities;
using Planner.App.Modules.Tasks.UseCases;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCalendar.Core.Abstractions;
using WinCalendar.Core.Time;
using WinCalendar.Shared.Settings;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerStateStore : ObservableObject
{
    private const int MonthGridCellCount = 42;

    private readonly CreateTaskUseCase _createTaskUseCase;
    private readonly DeleteTaskUseCase _deleteTaskUseCase;
    private readonly GetTasksForRangeUseCase _getTasksForRangeUseCase;
    private readonly AutoCompleteElapsedTimedTasksUseCase _autoCompleteElapsedTimedTasksUseCase;
    private readonly SetTaskCompletionStatusUseCase _setTaskCompletionStatusUseCase;
    private readonly UpdateTaskUseCase _updateTaskUseCase;
    private readonly AppSettingsStore _appSettingsStore;
    private readonly IClock _clock;
    private readonly PlannerTaskEditorState _taskEditor;
    private readonly Dictionary<DateOnly, bool> _agendaState = [];

    private bool _isInitialized;
    private Task? _initializationTask;
    private TaskEditorMode _taskEditorMode;
    private DateOnly _selectedDate;
    private DateOnly _displayMonth;
    private Guid? _selectedTaskId;
    private PlannerTaskViewModel? _selectedTask;
    private AppTimeFormatPreference _appliedTimeFormatPreference;
    private AppWeekStartPreference _appliedWeekStartPreference;
    private string _selectedDateText = string.Empty;
    private string _selectedDateSummary = string.Empty;
    private string _monthLabel = string.Empty;
    private string _weekLabel = string.Empty;
    private string _todaySummary = string.Empty;
    private string _weekSummary = string.Empty;
    private string _weekViewSummary = string.Empty;
    private double _currentTimeIndicatorTop;

    public PlannerStateStore(
        IClock clock,
        CreateTaskUseCase createTaskUseCase,
        GetTasksForRangeUseCase getTasksForRangeUseCase,
        AutoCompleteElapsedTimedTasksUseCase autoCompleteElapsedTimedTasksUseCase,
        SetTaskCompletionStatusUseCase setTaskCompletionStatusUseCase,
        DeleteTaskUseCase deleteTaskUseCase,
        UpdateTaskUseCase updateTaskUseCase,
        AppSettingsStore appSettingsStore)
    {
        _clock = clock;
        _createTaskUseCase = createTaskUseCase;
        _getTasksForRangeUseCase = getTasksForRangeUseCase;
        _autoCompleteElapsedTimedTasksUseCase = autoCompleteElapsedTimedTasksUseCase;
        _setTaskCompletionStatusUseCase = setTaskCompletionStatusUseCase;
        _deleteTaskUseCase = deleteTaskUseCase;
        _updateTaskUseCase = updateTaskUseCase;
        _appSettingsStore = appSettingsStore;
        _appSettingsStore.ThemePreferenceChanged += AppSettingsStore_PreferenceChanged;
        _appSettingsStore.TimeFormatPreferenceChanged += AppSettingsStore_PreferenceChanged;
        _appSettingsStore.WeekStartPreferenceChanged += AppSettingsStore_PreferenceChanged;
        _appliedTimeFormatPreference = _appSettingsStore.TimeFormatPreference;
        _appliedWeekStartPreference = _appSettingsStore.WeekStartPreference;
        PlannerDateTimeFormatter.TimeFormatPreference = _appliedTimeFormatPreference;

        DateOnly today = _clock.Today;
        _selectedDate = today;
        _displayMonth = new DateOnly(today.Year, today.Month, 1);
        _taskEditor = new PlannerTaskEditorState(today);
        _taskEditor.PropertyChanged += TaskEditor_PropertyChanged;

        MonthDays = [];
        SelectedDayTasks = [];
        TodayTasks = [];
        WeekAgendaDays = [];
        WeekTimelineHours = [];
        WeekTimelineDays = [];

        WeekdayLabels = [];
        RefreshDisplayPreferences();
    }

    public ObservableCollection<PlannerMonthDayViewModel> MonthDays { get; }

    public ObservableCollection<PlannerTaskViewModel> SelectedDayTasks { get; }

    public ObservableCollection<PlannerTaskViewModel> TodayTasks { get; }

    public ObservableCollection<PlannerAgendaDayViewModel> WeekAgendaDays { get; }

    public ObservableCollection<PlannerWeekHourViewModel> WeekTimelineHours { get; }

    public ObservableCollection<PlannerWeekDayTimelineViewModel> WeekTimelineDays { get; }

    public ObservableCollection<string> WeekdayLabels { get; }

    public ObservableCollection<PlannerEditorDurationOptionViewModel> EditorDurationOptions => _taskEditor.EditorDurationOptions;

    public DateOnly Today => _clock.Today;

    public DateOnly SelectedDate => _selectedDate;

    public string SelectedDateText
    {
        get => _selectedDateText;
        private set
        {
            if (!SetProperty(ref _selectedDateText, value))
                return;

            OnPropertyChanged(nameof(QuickAddTargetText));
        }
    }

    public string SelectedDateSummary
    {
        get => _selectedDateSummary;
        private set => SetProperty(ref _selectedDateSummary, value);
    }

    public string MonthLabel
    {
        get => _monthLabel;
        private set => SetProperty(ref _monthLabel, value);
    }

    public string WeekLabel
    {
        get => _weekLabel;
        private set => SetProperty(ref _weekLabel, value);
    }

    public string TodaySummary
    {
        get => _todaySummary;
        private set => SetProperty(ref _todaySummary, value);
    }

    public string WeekSummary
    {
        get => _weekSummary;
        private set => SetProperty(ref _weekSummary, value);
    }

    public string WeekViewSummary
    {
        get => _weekViewSummary;
        private set => SetProperty(ref _weekViewSummary, value);
    }

    public string QuickAddTargetText => SelectedDateText;

    public double WeekTimelineHeight => PlannerWeekTimelineLayout.AnyTimeLaneHeight + WeekTimelineTimedHeight;

    public double WeekTimelineTimedHeight => WeekTimelineHours.Count * PlannerWeekTimelineLayout.HourHeight;

    public double WeekTimelineHourHeight => PlannerWeekTimelineLayout.HourHeight;

    public double WeekTimelineDayWidth => PlannerWeekTimelineLayout.DayWidth;

    public double WeekTimelineAnyTimeLaneHeight => PlannerWeekTimelineLayout.AnyTimeLaneHeight;

    public double CurrentTimeIndicatorTop
    {
        get => _currentTimeIndicatorTop;
        private set
        {
            if (!SetProperty(ref _currentTimeIndicatorTop, value))
                return;

            OnPropertyChanged(nameof(CurrentWeekTimeIndicatorTop));
        }
    }

    public double CurrentWeekTimeIndicatorTop => PlannerWeekTimelineLayout.AnyTimeLaneHeight + CurrentTimeIndicatorTop;

    public Visibility CurrentWeekTimeIndicatorVisibility =>
        GetWeekStart(_selectedDate) == GetWeekStart(_clock.Today)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility WeekAgendaVisibility =>
        WeekAgendaDays.Any(day => day.Tasks.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyWeekAgendaVisibility =>
        WeekAgendaDays.Any(day => day.Tasks.Count > 0) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SelectedDayTasksVisibility =>
        SelectedDayTasks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptySelectedDayTasksVisibility =>
        SelectedDayTasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CompactSelectedDayContentVisibility =>
        _taskEditorMode == TaskEditorMode.None ? Visibility.Visible : Visibility.Collapsed;

    public string EmptySelectedDayTasksText =>
        _selectedDate == _clock.Today ? "No tasks for today" : "No tasks for this day";

    public PlannerTaskViewModel? SelectedTask
    {
        get => _selectedTask;
        private set
        {
            if (!SetProperty(ref _selectedTask, value))
                return;

            _selectedTaskId = value?.Id;
            OnPropertyChanged(nameof(SelectedTaskVisibility));
            OnPropertyChanged(nameof(EmptySelectedTaskVisibility));
            OnPropertyChanged(nameof(EditorCompletionButtonText));
        }
    }

    public Visibility SelectedTaskVisibility =>
        _taskEditorMode == TaskEditorMode.None ? Visibility.Collapsed : Visibility.Visible;

    public Visibility EmptySelectedTaskVisibility =>
        _taskEditorMode == TaskEditorMode.None ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EditorDeleteVisibility =>
        _taskEditorMode == TaskEditorMode.Edit ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EditorCompleteVisibility =>
        _taskEditorMode == TaskEditorMode.Edit ? Visibility.Visible : Visibility.Collapsed;

    public string EditorCompletionButtonText =>
        SelectedTask?.IsCompleted == true ? "Mark active" : "Mark done";

    public string EditorTitle
    {
        get => _taskEditor.EditorTitle;
        set => _taskEditor.EditorTitle = value;
    }

    public string EditorDescription
    {
        get => _taskEditor.EditorDescription;
        set => _taskEditor.EditorDescription = value;
    }

    public bool EditorHasTime
    {
        get => _taskEditor.EditorHasTime;
        set => _taskEditor.EditorHasTime = value;
    }

    public TimeSpan EditorTime
    {
        get => _taskEditor.EditorTime;
        set => _taskEditor.EditorTime = value;
    }

    public bool EditorHasDuration
    {
        get => _taskEditor.EditorHasDuration;
        set => _taskEditor.EditorHasDuration = value;
    }

    public double EditorDurationMinutes
    {
        get => _taskEditor.EditorDurationMinutes;
        set => _taskEditor.EditorDurationMinutes = value;
    }

    public DateTimeOffset EditorDate
    {
        get => _taskEditor.EditorDate;
        set => _taskEditor.EditorDate = value;
    }

    public TaskRecurrencePattern EditorRecurrencePattern
    {
        get => _taskEditor.EditorRecurrencePattern;
        set => _taskEditor.EditorRecurrencePattern = value;
    }

    public TaskCategory EditorCategory
    {
        get => _taskEditor.EditorCategory;
        set => _taskEditor.EditorCategory = value;
    }

    public bool EditorIsAllDay
    {
        get => _taskEditor.EditorIsAllDay;
        set => _taskEditor.EditorIsAllDay = value;
    }

    public bool EditorTimeInputEnabled => _taskEditor.EditorTimeInputEnabled;

    public bool EditorDurationToggleEnabled => _taskEditor.EditorDurationToggleEnabled;

    public bool EditorDurationInputEnabled => _taskEditor.EditorDurationInputEnabled;

    public Visibility EditorTimeVisibility => _taskEditor.EditorTimeVisibility;

    public Visibility EditorDurationVisibility => _taskEditor.EditorDurationVisibility;

    public string EditorTimeText => _taskEditor.EditorTimeText;

    public string EditorDurationText => _taskEditor.EditorDurationText;

    public string EditorDateText => _taskEditor.EditorDateText;

    public string EditorRecurrenceText => _taskEditor.EditorRecurrenceText;

    public string EditorCategoryText => _taskEditor.EditorCategoryText;

    public double EditorMaxDurationMinutes => _taskEditor.EditorMaxDurationMinutes;

    public Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return Task.CompletedTask;

        _initializationTask ??= InitializeAsync();
        return _initializationTask;
    }

    private async Task InitializeAsync()
    {
        try
        {
            bool didAutoCompleteTasks = await AutoCompleteElapsedTimedTasksAsync();
            if (!didAutoCompleteTasks)
                await ReloadAsync();

            _isInitialized = true;
        }
        catch
        {
            _initializationTask = null;
            throw;
        }
    }

    public void RefreshCurrentTimeIndicator()
    {
        double top = (_clock.TimeOfDay.ToTimeSpan().TotalMinutes / 60d) * PlannerWeekTimelineLayout.HourHeight;
        CurrentTimeIndicatorTop = Math.Clamp(top, 0d, Math.Max(0d, WeekTimelineTimedHeight - 2d));
        OnPropertyChanged(nameof(CurrentWeekTimeIndicatorVisibility));
    }

    public Task GoToPreviousMonthAsync()
    {
        DateOnly previousMonth = _displayMonth.AddMonths(-1);
        return SelectDateAsync(ClampToMonth(previousMonth, _selectedDate.Day));
    }

    public Task GoToNextMonthAsync()
    {
        DateOnly nextMonth = _displayMonth.AddMonths(1);
        return SelectDateAsync(ClampToMonth(nextMonth, _selectedDate.Day));
    }

    public Task BrowsePreviousMonthAsync()
    {
        _displayMonth = _displayMonth.AddMonths(-1);
        return ReloadAsync();
    }

    public Task BrowseNextMonthAsync()
    {
        _displayMonth = _displayMonth.AddMonths(1);
        return ReloadAsync();
    }

    public Task GoToPreviousDayAsync()
    {
        return SelectDateAsync(_selectedDate.AddDays(-1));
    }

    public Task GoToNextDayAsync()
    {
        return SelectDateAsync(_selectedDate.AddDays(1));
    }

    public Task GoToPreviousWeekAsync()
    {
        return SelectDateAsync(_selectedDate.AddDays(-7));
    }

    public Task GoToNextWeekAsync()
    {
        return SelectDateAsync(_selectedDate.AddDays(7));
    }

    public Task GoToTodayAsync()
    {
        return SelectDateAsync(_clock.Today);
    }

    public bool HasTasksOn(DateOnly date)
    {
        return MonthDays.Any(day => day.Date == date && day.HasTasks);
    }

    public async Task SelectDateAsync(DateOnly date)
    {
        _selectedDate = date;
        _displayMonth = new DateOnly(date.Year, date.Month, 1);
        await ReloadAsync();
    }

    public void ToggleAgendaDayExpanded(PlannerAgendaDayViewModel day)
    {
        bool nextState = !day.IsExpanded;

        foreach (PlannerAgendaDayViewModel currentDay in WeekAgendaDays)
        {
            bool shouldBeExpanded = ReferenceEquals(currentDay, day) && nextState;

            if (currentDay.IsExpanded == shouldBeExpanded)
                continue;

            currentDay.IsExpanded = shouldBeExpanded;
            UpdateAgendaState(currentDay);
        }
    }

    public async Task AddTaskAsync(
        string title,
        DateOnly? date = null,
        TimeOnly? time = null,
        int? durationMinutes = null,
        TaskRecurrencePattern recurrencePattern = TaskRecurrencePattern.None,
        TaskCategory category = TaskCategory.Work)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        DateOnly targetDate = date ?? _selectedDate;
        TaskItem task = await _createTaskUseCase.ExecuteAsync(
            title,
            targetDate,
            time,
            durationMinutes,
            recurrencePattern: recurrencePattern,
            category: category);
        _selectedTaskId = task.Id;
        await SelectDateAsync(targetDate);
    }

    public async Task ToggleTaskCompletionAsync(Guid id, bool isCompleted)
    {
        await _setTaskCompletionStatusUseCase.ExecuteAsync(id, isCompleted);
        await ReloadAsync();
    }

    public async Task<bool> AutoCompleteElapsedTimedTasksAsync()
    {
        bool didCompleteTasks = await _autoCompleteElapsedTimedTasksUseCase.ExecuteAsync(_clock.Now);
        if (!didCompleteTasks)
            return false;

        await ReloadAsync();
        return true;
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        await _deleteTaskUseCase.ExecuteAsync(id);

        if (_selectedTaskId == id)
        {
            _selectedTaskId = null;
            SelectTask(null);
        }

        await ReloadAsync();
    }

    public void BeginNewTaskDraft(DateOnly date, TimeOnly? time = null)
    {
        SelectedTask = null;
        SetTaskEditorMode(TaskEditorMode.New);
        _taskEditor.BeginNewTaskDraft(date, time);
    }

    public void SelectTask(PlannerTaskViewModel? task)
    {
        SelectedTask = task;

        if (task is null)
        {
            SetTaskEditorMode(TaskEditorMode.None);
            _taskEditor.Reset(_selectedDate);
            return;
        }

        SetTaskEditorMode(TaskEditorMode.Edit);
        _taskEditor.LoadTask(task);
    }

    public async Task SaveSelectedTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorTitle))
            return;

        DateOnly date = DateOnly.FromDateTime(EditorDate.Date);
        TimeOnly? time = EditorHasTime ? TimeOnly.FromTimeSpan(EditorTime) : null;
        int? durationMinutes = EditorHasTime && EditorHasDuration
            ? (int)Math.Round(EditorDurationMinutes)
            : null;
        string? description = string.IsNullOrWhiteSpace(EditorDescription)
            ? null
            : EditorDescription;

        TaskItem task = _taskEditorMode switch
        {
            TaskEditorMode.New => await _createTaskUseCase.ExecuteAsync(
                EditorTitle,
                date,
                time,
                durationMinutes,
                description,
                EditorRecurrencePattern,
                EditorCategory),
            TaskEditorMode.Edit when SelectedTask is not null => await _updateTaskUseCase.ExecuteAsync(
                SelectedTask.Id,
                EditorTitle,
                date,
                time,
                durationMinutes,
                description,
                EditorRecurrencePattern,
                EditorCategory),
            _ => throw new InvalidOperationException("Task editor is not ready to save.")
        };

        _selectedTaskId = task.Id;
        _selectedDate = date;
        _displayMonth = new DateOnly(date.Year, date.Month, 1);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        DateOnly today = _clock.Today;

        DateOnly monthGridStart = GetMonthGridStart(_displayMonth);
        DateOnly monthGridEnd = monthGridStart.AddDays(MonthGridCellCount - 1);
        DateOnly selectedWeekStart = GetWeekStart(_selectedDate);
        DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);
        DateOnly todayWeekEnd = GetWeekEnd(today);

        DateOnly rangeStart = Min(monthGridStart, selectedWeekStart, today);
        DateOnly rangeEnd = Max(monthGridEnd, selectedWeekEnd, todayWeekEnd, today);

        IReadOnlyList<TaskItem> tasks = await _getTasksForRangeUseCase.ExecuteAsync(rangeStart, rangeEnd);
        Dictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup = BuildTaskLookup(tasks);

        BuildMonth(taskLookup, today);
        BuildSelectedDay(taskLookup);
        BuildToday(taskLookup, today);
        BuildWeekAgenda(taskLookup, today, today, todayWeekEnd);
        BuildWeekTimeline(taskLookup, selectedWeekStart, selectedWeekEnd, today);
        RefreshCurrentTimeIndicator();
        OnPropertyChanged(nameof(CurrentWeekTimeIndicatorVisibility));
        UpdateSummaries(selectedWeekStart, selectedWeekEnd);
        ReselectTask();
    }

    private void BuildMonth(IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup, DateOnly today)
    {
        MonthDays.Clear();
        DateOnly gridStart = GetMonthGridStart(_displayMonth);

        for (int index = 0; index < MonthGridCellCount; index++)
        {
            DateOnly date = gridStart.AddDays(index);
            taskLookup.TryGetValue(date, out List<PlannerTaskViewModel>? tasksForDay);
            bool hasTasks = tasksForDay is { Count: > 0 };
            bool allCompleted = hasTasks && tasksForDay!.All(task => task.IsCompleted);

            MonthDays.Add(new PlannerMonthDayViewModel(
                date,
                date.Month == _displayMonth.Month,
                date == today,
                date == _selectedDate,
                hasTasks,
                allCompleted));
        }
    }

    private void BuildSelectedDay(IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup)
    {
        SelectedDayTasks.Clear();

        if (!taskLookup.TryGetValue(_selectedDate, out List<PlannerTaskViewModel>? tasksForDay))
            return;

        foreach (PlannerTaskViewModel task in tasksForDay)
            SelectedDayTasks.Add(task);
    }

    private void BuildToday(IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup, DateOnly today)
    {
        TodayTasks.Clear();

        if (!taskLookup.TryGetValue(today, out List<PlannerTaskViewModel>? tasksForToday))
            return;

        foreach (PlannerTaskViewModel task in tasksForToday)
            TodayTasks.Add(task);
    }

    private void BuildWeekAgenda(
        IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup,
        DateOnly today,
        DateOnly startDate,
        DateOnly endDate)
    {
        WeekAgendaDays.Clear();

        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            _agendaState.TryGetValue(date, out bool isExpanded);
            List<PlannerTaskViewModel> tasksForDay = taskLookup.TryGetValue(date, out List<PlannerTaskViewModel>? tasks)
                ? tasks
                : [];

            WeekAgendaDays.Add(new PlannerAgendaDayViewModel(
                date,
                tasksForDay,
                today,
                isExpanded));
        }

        OnPropertyChanged(nameof(WeekAgendaVisibility));
        OnPropertyChanged(nameof(EmptyWeekAgendaVisibility));
    }

    private void BuildWeekTimeline(
        IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup,
        DateOnly weekStart,
        DateOnly weekEnd,
        DateOnly today)
    {
        WeekTimelineDays.Clear();

        for (DateOnly date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            List<PlannerTaskViewModel> tasksForDay = taskLookup.TryGetValue(date, out List<PlannerTaskViewModel>? tasks)
                ? tasks
                : [];

            List<PlannerTaskViewModel> allDayTasks = tasksForDay
                .Where(task => !task.HasTime)
                .ToList();

            List<PlannerWeekTaskBlockViewModel> timedTaskBlocks = PlannerWeekTimelineLayout.BuildTimedTaskBlocks(tasksForDay);

            WeekTimelineDays.Add(new PlannerWeekDayTimelineViewModel(
                date,
                allDayTasks,
                timedTaskBlocks,
                date == today,
                date == _selectedDate));
        }
    }

    private void UpdateSummaries(DateOnly weekStart, DateOnly weekEnd)
    {
        SelectedDateText = PlannerDateTimeFormatter.FormatDate(_selectedDate);
        SelectedDateSummary = SelectedDayTasks.Count == 0
            ? "No tasks"
            : $"{SelectedDayTasks.Count} task{(SelectedDayTasks.Count == 1 ? string.Empty : "s")}";
        OnPropertyChanged(nameof(SelectedDayTasksVisibility));
        OnPropertyChanged(nameof(EmptySelectedDayTasksVisibility));
        OnPropertyChanged(nameof(EmptySelectedDayTasksText));

        MonthLabel = PlannerDateTimeFormatter.FormatMonthTitle(_displayMonth);
        WeekLabel = PlannerDateTimeFormatter.FormatWeekRange(weekStart, weekEnd);
        TodaySummary = TodayTasks.Count == 0
            ? "Today is clear"
            : $"{TodayTasks.Count} task{(TodayTasks.Count == 1 ? string.Empty : "s")} today";

        int weekTaskCount = WeekAgendaDays.Sum(day => day.Tasks.Count);
        WeekSummary = weekTaskCount == 0
            ? "No tasks this week"
            : $"{weekTaskCount} task{(weekTaskCount == 1 ? string.Empty : "s")} this week";

        int weekViewTaskCount = WeekTimelineDays.Sum(day => day.AllDayTasks.Count + day.TimedTaskBlocks.Count);
        WeekViewSummary = weekViewTaskCount == 0
            ? "No tasks for this week"
            : $"{weekViewTaskCount} task{(weekViewTaskCount == 1 ? string.Empty : "s")} from {WeekdayLabels[0]} to {WeekdayLabels[6]}";
    }

    private void ReselectTask()
    {
        if (_selectedTaskId is null)
        {
            if (_taskEditorMode != TaskEditorMode.New)
                SelectTask(null);

            return;
        }

        PlannerTaskViewModel? task = SelectedDayTasks.FirstOrDefault(current => current.Id == _selectedTaskId)
            ?? WeekAgendaDays.SelectMany(day => day.Tasks).FirstOrDefault(current => current.Id == _selectedTaskId);

        if (task is null)
        {
            _selectedTaskId = null;

            if (_taskEditorMode != TaskEditorMode.New)
                SelectTask(null);

            return;
        }

        SelectTask(task);
    }

    private void SetTaskEditorMode(TaskEditorMode mode)
    {
        if (_taskEditorMode == mode)
            return;

        _taskEditorMode = mode;
        OnPropertyChanged(nameof(SelectedTaskVisibility));
        OnPropertyChanged(nameof(EmptySelectedTaskVisibility));
        OnPropertyChanged(nameof(EditorDeleteVisibility));
        OnPropertyChanged(nameof(EditorCompleteVisibility));
        OnPropertyChanged(nameof(EditorCompletionButtonText));
        OnPropertyChanged(nameof(CompactSelectedDayContentVisibility));
    }

    private void TaskEditor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    private void UpdateAgendaState(PlannerAgendaDayViewModel day)
    {
        _agendaState[day.Date] = day.IsExpanded;
    }

    private void RefreshWeekdayLabels()
    {
        string[] labels = _appSettingsStore.WeekStartPreference == AppWeekStartPreference.Sunday
            ? ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"]
            : ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];

        if (WeekdayLabels.Count == labels.Length)
        {
            bool labelsMatch = true;
            for (int index = 0; index < labels.Length; index++)
            {
                if (WeekdayLabels[index] == labels[index])
                    continue;

                labelsMatch = false;
                break;
            }

            if (labelsMatch)
                return;
        }

        WeekdayLabels.Clear();
        foreach (string label in labels)
            WeekdayLabels.Add(label);
    }

    private static Dictionary<DateOnly, List<PlannerTaskViewModel>> BuildTaskLookup(IReadOnlyList<TaskItem> tasks)
    {
        Dictionary<DateOnly, List<TaskItem>> rawLookup = [];

        foreach (TaskItem task in tasks)
        {
            if (!rawLookup.TryGetValue(task.Date, out List<TaskItem>? tasksForDay))
            {
                tasksForDay = [];
                rawLookup[task.Date] = tasksForDay;
            }

            tasksForDay.Add(task);
        }

        Dictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup = new(rawLookup.Count);
        foreach ((DateOnly date, List<TaskItem> tasksForDay) in rawLookup)
        {
            tasksForDay.Sort(CompareTasksForDisplay);

            List<PlannerTaskViewModel> viewModels = new(tasksForDay.Count);
            foreach (TaskItem task in tasksForDay)
                viewModels.Add(new PlannerTaskViewModel(task));

            taskLookup[date] = viewModels;
        }

        return taskLookup;
    }

    private static int CompareTasksForDisplay(TaskItem left, TaskItem right)
    {
        int completedComparison = left.IsCompleted.CompareTo(right.IsCompleted);
        if (completedComparison != 0)
            return completedComparison;

        int timeComparison = (left.Time ?? TimeOnly.MaxValue).CompareTo(right.Time ?? TimeOnly.MaxValue);
        if (timeComparison != 0)
            return timeComparison;

        return left.CreatedAt.CompareTo(right.CreatedAt);
    }

    private void RefreshWeekTimelineHours(bool force)
    {
        if (!force && WeekTimelineHours.Count == 24)
            return;

        WeekTimelineHours.Clear();

        for (int hour = 0; hour < 24; hour++)
            WeekTimelineHours.Add(new PlannerWeekHourViewModel(hour));
    }

    private void RefreshDisplayPreferences()
    {
        bool timeFormatChanged = _appSettingsStore.TimeFormatPreference != _appliedTimeFormatPreference;
        _appliedTimeFormatPreference = _appSettingsStore.TimeFormatPreference;
        _appliedWeekStartPreference = _appSettingsStore.WeekStartPreference;
        PlannerDateTimeFormatter.TimeFormatPreference = _appliedTimeFormatPreference;
        RefreshWeekdayLabels();
        RefreshWeekTimelineHours(timeFormatChanged);
        _taskEditor.UpdateEditorDurationOptions();
        OnPropertyChanged(nameof(EditorTimeText));
        OnPropertyChanged(nameof(EditorDurationText));
    }

    private void AppSettingsStore_PreferenceChanged(object? sender, EventArgs e)
    {
        PlannerTaskPalette.UseDarkPalette = _appSettingsStore.IsDarkThemeEffective;

        if (_appSettingsStore.TimeFormatPreference != _appliedTimeFormatPreference
            || _appSettingsStore.WeekStartPreference != _appliedWeekStartPreference)
        {
            RefreshDisplayPreferences();
        }

        _ = ReloadAsync();
    }

    private DateOnly GetMonthGridStart(DateOnly monthStart)
    {
        int startIndex = GetDayOffset(monthStart.DayOfWeek);
        return monthStart.AddDays(-startIndex);
    }

    private DateOnly GetWeekStart(DateOnly date)
    {
        int startIndex = GetDayOffset(date.DayOfWeek);
        return date.AddDays(-startIndex);
    }

    private DateOnly GetWeekEnd(DateOnly date)
    {
        return GetWeekStart(date).AddDays(6);
    }

    private int GetDayOffset(DayOfWeek dayOfWeek)
    {
        return _appSettingsStore.WeekStartPreference == AppWeekStartPreference.Sunday
            ? (int)dayOfWeek
            : ((int)dayOfWeek + 6) % 7;
    }

    private static DateOnly ClampToMonth(DateOnly monthStart, int preferredDay)
    {
        int daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        return new DateOnly(monthStart.Year, monthStart.Month, Math.Min(preferredDay, daysInMonth));
    }

    private static DateOnly Min(params DateOnly[] values)
    {
        return values.Min();
    }

    private static DateOnly Max(params DateOnly[] values)
    {
        return values.Max();
    }

    private enum TaskEditorMode
    {
        None,
        New,
        Edit
    }
}
