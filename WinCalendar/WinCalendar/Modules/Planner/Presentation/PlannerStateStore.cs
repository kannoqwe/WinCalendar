using Planner.App.Modules.Tasks.Entities;
using Planner.App.Modules.Tasks.UseCases;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinCalendar.Core.Abstractions;
using WinCalendar.Core.Time;

namespace WinCalendar.Modules.Planner.Presentation;

public sealed class PlannerStateStore : ObservableObject
{
    private const int MonthGridCellCount = 42;
    private const double WeekTimelineHourHeightValue = 40;
    private const double WeekTimelineDayWidthValue = 132;
    private const double WeekTimelineAnyTimeLaneHeightValue = 64;
    private const double WeekTaskHorizontalPaddingValue = 4;
    private const double WeekTaskColumnGapValue = 4;
    private const int WeekTaskDefaultDurationMinutes = 45;
    private const double WeekTaskMinimumHeightValue = 28;

    private readonly CreateTaskUseCase _createTaskUseCase;
    private readonly DeleteTaskUseCase _deleteTaskUseCase;
    private readonly GetTasksForRangeUseCase _getTasksForRangeUseCase;
    private readonly SetTaskCompletionStatusUseCase _setTaskCompletionStatusUseCase;
    private readonly UpdateTaskUseCase _updateTaskUseCase;
    private readonly Dictionary<DateOnly, bool> _agendaState = [];

    private bool _isInitialized;
    private bool _isCompactSidebarOpen;
    private TaskEditorMode _taskEditorMode;
    private DateOnly _selectedDate;
    private DateOnly _displayMonth;
    private Guid? _selectedTaskId;
    private PlannerTaskViewModel? _selectedTask;
    private string _selectedDateText = string.Empty;
    private string _selectedDateSummary = string.Empty;
    private string _monthLabel = string.Empty;
    private string _weekLabel = string.Empty;
    private string _todaySummary = string.Empty;
    private string _weekSummary = string.Empty;
    private string _weekViewSummary = string.Empty;
    private string _editorTitle = string.Empty;
    private bool _editorHasTime;
    private bool _editorHasDuration;
    private TimeSpan _editorTime = new(9, 0, 0);
    private double _editorDurationMinutes = WeekTaskDefaultDurationMinutes;
    private DateTimeOffset _editorDate = DateTimeOffset.Now;
    private double _currentTimeIndicatorTop;

    public PlannerStateStore(
        CreateTaskUseCase createTaskUseCase,
        GetTasksForRangeUseCase getTasksForRangeUseCase,
        SetTaskCompletionStatusUseCase setTaskCompletionStatusUseCase,
        DeleteTaskUseCase deleteTaskUseCase,
        UpdateTaskUseCase updateTaskUseCase)
    {
        _createTaskUseCase = createTaskUseCase;
        _getTasksForRangeUseCase = getTasksForRangeUseCase;
        _setTaskCompletionStatusUseCase = setTaskCompletionStatusUseCase;
        _deleteTaskUseCase = deleteTaskUseCase;
        _updateTaskUseCase = updateTaskUseCase;

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        _selectedDate = today;
        _displayMonth = new DateOnly(today.Year, today.Month, 1);
        _editorDate = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue));

        MonthDays = [];
        SelectedDayTasks = [];
        TodayTasks = [];
        WeekAgendaDays = [];
        WeekTimelineHours = [];
        WeekTimelineDays = [];

        for (int hour = 0; hour < 24; hour++)
            WeekTimelineHours.Add(new PlannerWeekHourViewModel(hour));
    }

    public ObservableCollection<PlannerMonthDayViewModel> MonthDays { get; }

    public ObservableCollection<PlannerTaskViewModel> SelectedDayTasks { get; }

    public ObservableCollection<PlannerTaskViewModel> TodayTasks { get; }

    public ObservableCollection<PlannerAgendaDayViewModel> WeekAgendaDays { get; }

    public ObservableCollection<PlannerWeekHourViewModel> WeekTimelineHours { get; }

    public ObservableCollection<PlannerWeekDayTimelineViewModel> WeekTimelineDays { get; }

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

    public double WeekTimelineHeight => WeekTimelineAnyTimeLaneHeightValue + WeekTimelineTimedHeight;

    public double WeekTimelineTimedHeight => WeekTimelineHours.Count * WeekTimelineHourHeightValue;

    public double WeekTimelineHourHeight => WeekTimelineHourHeightValue;

    public double WeekTimelineDayWidth => WeekTimelineDayWidthValue;

    public double WeekTimelineAnyTimeLaneHeight => WeekTimelineAnyTimeLaneHeightValue;

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

    public double CurrentWeekTimeIndicatorTop => WeekTimelineAnyTimeLaneHeightValue + CurrentTimeIndicatorTop;

    public Visibility CurrentWeekTimeIndicatorVisibility =>
        GetWeekStart(_selectedDate) == GetWeekStart(DateOnly.FromDateTime(DateTime.Today))
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility WeekAgendaVisibility =>
        WeekAgendaDays.Any(day => day.Tasks.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyWeekAgendaVisibility =>
        WeekAgendaDays.Any(day => day.Tasks.Count > 0) ? Visibility.Collapsed : Visibility.Visible;

    public bool IsCompactSidebarOpen
    {
        get => _isCompactSidebarOpen;
        private set
        {
            if (!SetProperty(ref _isCompactSidebarOpen, value))
                return;

            OnPropertyChanged(nameof(CompactSidebarVisibility));
            OnPropertyChanged(nameof(CompactPanelRootPadding));
            OnPropertyChanged(nameof(CompactPanelCornerRadius));
            OnPropertyChanged(nameof(CompactSidebarCornerRadius));
        }
    }

    public Visibility CompactSidebarVisibility =>
        IsCompactSidebarOpen ? Visibility.Visible : Visibility.Collapsed;

    public Thickness CompactPanelRootPadding =>
        IsCompactSidebarOpen ? new Thickness(0, 10, 10, 10) : new Thickness(10);

    public CornerRadius CompactPanelCornerRadius =>
        IsCompactSidebarOpen ? new CornerRadius(0, 22, 22, 0) : new CornerRadius(22);

    public CornerRadius CompactSidebarCornerRadius =>
        IsCompactSidebarOpen ? new CornerRadius(22, 0, 0, 22) : new CornerRadius(22);

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
        get => _editorTitle;
        set
        {
            if (!SetProperty(ref _editorTitle, value))
                return;
        }
    }

    public bool EditorHasTime
    {
        get => _editorHasTime;
        set
        {
            if (!SetProperty(ref _editorHasTime, value))
                return;

            if (!value)
                EditorHasDuration = false;

            ClampEditorDuration();
            OnPropertyChanged(nameof(EditorDurationToggleEnabled));
            OnPropertyChanged(nameof(EditorDurationInputEnabled));
            OnPropertyChanged(nameof(EditorTimeVisibility));
            OnPropertyChanged(nameof(EditorDurationVisibility));
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
            OnPropertyChanged(nameof(EditorMaxDurationMinutes));
            OnPropertyChanged(nameof(EditorTimeText));
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
        }
    }

    public DateTimeOffset EditorDate
    {
        get => _editorDate;
        set
        {
            SetProperty(ref _editorDate, value);
        }
    }

    public bool EditorDurationToggleEnabled => EditorHasTime;

    public bool EditorDurationInputEnabled => EditorHasTime && EditorHasDuration;

    public Visibility EditorTimeVisibility =>
        EditorHasTime ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EditorDurationVisibility =>
        EditorHasTime && EditorHasDuration ? Visibility.Visible : Visibility.Collapsed;

    public string EditorTimeText => $"{EditorTime.Hours:00}:{EditorTime.Minutes:00}";

    public double EditorMaxDurationMinutes =>
        EditorHasTime
            ? GetMaxDurationMinutes(TimeOnly.FromTimeSpan(EditorTime))
            : 24 * 60;

    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await ReloadAsync();
    }

    public void RefreshCurrentTimeIndicator()
    {
        double top = (TimeOnly.FromDateTime(DateTime.Now).ToTimeSpan().TotalMinutes / 60d) * WeekTimelineHourHeightValue;
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
        return SelectDateAsync(DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task SelectDateAsync(DateOnly date)
    {
        _selectedDate = date;
        _displayMonth = new DateOnly(date.Year, date.Month, 1);
        await ReloadAsync();
    }

    public void ToggleCompactSidebar()
    {
        IsCompactSidebarOpen = !IsCompactSidebarOpen;
    }

    public void SetCompactSidebarOpen(bool isOpen)
    {
        IsCompactSidebarOpen = isOpen;
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

    public async Task AddTaskAsync(string title, DateOnly? date = null, TimeOnly? time = null, int? durationMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        DateOnly targetDate = date ?? _selectedDate;
        TaskItem task = await _createTaskUseCase.ExecuteAsync(title, targetDate, time, durationMinutes);
        _selectedTaskId = task.Id;
        await SelectDateAsync(targetDate);
    }

    public async Task ToggleTaskCompletionAsync(Guid id, bool isCompleted)
    {
        await _setTaskCompletionStatusUseCase.ExecuteAsync(id, isCompleted);
        await ReloadAsync();
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
        EditorTitle = string.Empty;
        EditorDate = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
        EditorHasTime = time is not null;
        EditorTime = (time ?? new TimeOnly(9, 0)).ToTimeSpan();
        EditorHasDuration = false;
        EditorDurationMinutes = WeekTaskDefaultDurationMinutes;
    }

    public void SelectTask(PlannerTaskViewModel? task)
    {
        SelectedTask = task;

        if (task is null)
        {
            SetTaskEditorMode(TaskEditorMode.None);
            EditorTitle = string.Empty;
            EditorHasTime = false;
            EditorTime = new TimeSpan(9, 0, 0);
            EditorHasDuration = false;
            EditorDurationMinutes = WeekTaskDefaultDurationMinutes;
            EditorDate = new DateTimeOffset(_selectedDate.ToDateTime(TimeOnly.MinValue));
            return;
        }

        SetTaskEditorMode(TaskEditorMode.Edit);
        EditorTitle = task.Title;
        EditorHasTime = task.HasTime;
        EditorTime = task.Time?.ToTimeSpan() ?? new TimeSpan(9, 0, 0);
        EditorHasDuration = task.HasDuration;
        EditorDurationMinutes = task.DurationMinutes ?? WeekTaskDefaultDurationMinutes;
        EditorDate = new DateTimeOffset(task.Date.ToDateTime(TimeOnly.MinValue));
    }

    public async Task SaveSelectedTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorTitle))
            return;

        DateOnly date = DateOnly.FromDateTime(EditorDate.Date);
        TimeOnly? time = EditorHasTime ? TimeOnly.FromTimeSpan(EditorTime) : null;
        int? durationMinutes = EditorHasTime && EditorHasDuration
            ? (int)NormalizeDurationValue(EditorDurationMinutes, EditorMaxDurationMinutes)
            : null;

        TaskItem task = _taskEditorMode switch
        {
            TaskEditorMode.New => await _createTaskUseCase.ExecuteAsync(EditorTitle, date, time, durationMinutes),
            TaskEditorMode.Edit when SelectedTask is not null => await _updateTaskUseCase.ExecuteAsync(
                SelectedTask.Id,
                EditorTitle,
                date,
                time,
                durationMinutes),
            _ => throw new InvalidOperationException("Task editor is not ready to save.")
        };

        _selectedTaskId = task.Id;
        _selectedDate = date;
        _displayMonth = new DateOnly(date.Year, date.Month, 1);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly monthGridStart = GetMonthGridStart(_displayMonth);
        DateOnly monthGridEnd = monthGridStart.AddDays(MonthGridCellCount - 1);
        DateOnly selectedWeekStart = GetWeekStart(_selectedDate);
        DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);
        DateOnly todayWeekEnd = GetWeekEnd(today);

        DateOnly rangeStart = Min(monthGridStart, selectedWeekStart, today);
        DateOnly rangeEnd = Max(monthGridEnd, selectedWeekEnd, todayWeekEnd, today);

        IReadOnlyList<TaskItem> tasks = await _getTasksForRangeUseCase.ExecuteAsync(rangeStart, rangeEnd);
        Dictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup = tasks
            .OrderBy(task => task.Time ?? TimeOnly.MaxValue)
            .ThenBy(task => task.CreatedAt)
            .GroupBy(task => task.Date)
            .ToDictionary(
                group => group.Key,
                group => group.Select(task => new PlannerTaskViewModel(task)).ToList());

        BuildMonth(taskLookup);
        BuildSelectedDay(taskLookup);
        BuildToday(taskLookup, today);
        BuildWeekAgenda(taskLookup, today, todayWeekEnd);
        BuildWeekTimeline(taskLookup, selectedWeekStart, selectedWeekEnd, today);
        RefreshCurrentTimeIndicator();
        OnPropertyChanged(nameof(CurrentWeekTimeIndicatorVisibility));
        UpdateSummaries(selectedWeekStart, selectedWeekEnd);
        ReselectTask();
    }

    private void BuildMonth(IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup)
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
                date == DateOnly.FromDateTime(DateTime.Today),
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

            List<PlannerWeekTaskBlockViewModel> timedTaskBlocks = BuildWeekTimedTaskBlocks(tasksForDay);

            WeekTimelineDays.Add(new PlannerWeekDayTimelineViewModel(
                date,
                allDayTasks,
                timedTaskBlocks,
                date == today,
                date == _selectedDate));
        }
    }

    private static int GetTaskStartMinutes(PlannerTaskViewModel task)
    {
        TimeSpan time = task.Time!.Value.ToTimeSpan();
        return (time.Hours * 60) + time.Minutes;
    }

    private static int GetTaskEndMinutes(PlannerTaskViewModel task)
    {
        int durationMinutes = task.DurationMinutes ?? WeekTaskDefaultDurationMinutes;
        return Math.Min(24 * 60, GetTaskStartMinutes(task) + durationMinutes);
    }

    private static List<List<WeekTaskLayoutItem>> BuildWeekTaskGroups(List<WeekTaskLayoutItem> items)
    {
        List<List<WeekTaskLayoutItem>> groups = [];

        foreach (WeekTaskLayoutItem item in items)
        {
            if (groups.Count == 0)
            {
                groups.Add([item]);
                continue;
            }

            List<WeekTaskLayoutItem> currentGroup = groups[^1];
            int currentGroupEnd = currentGroup.Max(current => current.EndMinutes);

            if (item.StartMinutes < currentGroupEnd)
            {
                currentGroup.Add(item);
                continue;
            }

            groups.Add([item]);
        }

        return groups;
    }

    private static void AssignWeekTaskColumns(List<WeekTaskLayoutItem> group)
    {
        List<WeekTaskLayoutItem> activeItems = [];
        int totalColumns = 1;

        foreach (WeekTaskLayoutItem item in group.OrderBy(current => current.StartMinutes))
        {
            activeItems.RemoveAll(current => current.EndMinutes <= item.StartMinutes);

            int column = 0;
            while (activeItems.Any(current => current.Column == column))
                column++;

            item.Column = column;
            activeItems.Add(item);
            totalColumns = Math.Max(totalColumns, activeItems.Max(current => current.Column) + 1);
        }

        foreach (WeekTaskLayoutItem item in group)
            item.TotalColumns = totalColumns;
    }

    private static PlannerWeekTaskBlockViewModel CreateWeekTaskBlock(WeekTaskLayoutItem item)
    {
        double usableWidth = WeekTimelineDayWidthValue - (WeekTaskHorizontalPaddingValue * 2);
        double width = (usableWidth - ((item.TotalColumns - 1) * WeekTaskColumnGapValue)) / item.TotalColumns;
        double clampedWidth = Math.Max(32, width);
        double top = (item.StartMinutes / 60d) * WeekTimelineHourHeightValue;
        double height = Math.Max(
            WeekTaskMinimumHeightValue,
            ((item.EndMinutes - item.StartMinutes) / 60d) * WeekTimelineHourHeightValue);

        return new PlannerWeekTaskBlockViewModel(
            item.Task,
            top,
            WeekTaskHorizontalPaddingValue + (item.Column * (clampedWidth + WeekTaskColumnGapValue)),
            clampedWidth,
            height);
    }

    private static List<PlannerWeekTaskBlockViewModel> BuildWeekTimedTaskBlocks(IEnumerable<PlannerTaskViewModel> tasks)
    {
        List<WeekTaskLayoutItem> items = tasks
            .Where(task => task.HasTime)
            .OrderBy(task => task.Time)
            .ThenBy(task => task.Title)
            .Select(task => new WeekTaskLayoutItem(task, GetTaskStartMinutes(task), GetTaskEndMinutes(task)))
            .ToList();

        if (items.Count == 0)
            return [];

        foreach (List<WeekTaskLayoutItem> group in BuildWeekTaskGroups(items))
            AssignWeekTaskColumns(group);

        return items
            .Select(CreateWeekTaskBlock)
            .ToList();
    }

    private void UpdateSummaries(DateOnly weekStart, DateOnly weekEnd)
    {
        SelectedDateText = PlannerDateTimeFormatter.FormatDate(_selectedDate);
        SelectedDateSummary = SelectedDayTasks.Count == 0
            ? "No tasks"
            : $"{SelectedDayTasks.Count} task{(SelectedDayTasks.Count == 1 ? string.Empty : "s")}";

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
            : $"{weekViewTaskCount} task{(weekViewTaskCount == 1 ? string.Empty : "s")} from Monday to Sunday";
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
    }

    private void UpdateAgendaState(PlannerAgendaDayViewModel day)
    {
        _agendaState[day.Date] = day.IsExpanded;
    }

    private void ClampEditorDuration()
    {
        double normalizedDuration = NormalizeDurationValue(_editorDurationMinutes, EditorMaxDurationMinutes);

        if (Math.Abs(normalizedDuration - _editorDurationMinutes) < double.Epsilon)
            return;

        _editorDurationMinutes = normalizedDuration;
        OnPropertyChanged(nameof(EditorDurationMinutes));
    }

    private static double NormalizeDurationValue(double value, double maxDurationMinutes)
    {
        double normalizedValue = double.IsNaN(value)
            ? WeekTaskDefaultDurationMinutes
            : Math.Round(value / 15d) * 15d;

        return Math.Clamp(normalizedValue, 15d, maxDurationMinutes);
    }

    private static double GetMaxDurationMinutes(TimeOnly time)
    {
        return (24 * 60) - time.ToTimeSpan().TotalMinutes;
    }

    private sealed class WeekTaskLayoutItem
    {
        public WeekTaskLayoutItem(PlannerTaskViewModel task, int startMinutes, int endMinutes)
        {
            Task = task;
            StartMinutes = startMinutes;
            EndMinutes = endMinutes;
        }

        public PlannerTaskViewModel Task { get; }

        public int StartMinutes { get; }

        public int EndMinutes { get; }

        public int Column { get; set; }

        public int TotalColumns { get; set; } = 1;
    }

    private static DateOnly GetMonthGridStart(DateOnly monthStart)
    {
        int mondayIndex = ((int)monthStart.DayOfWeek + 6) % 7;
        return monthStart.AddDays(-mondayIndex);
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int mondayIndex = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-mondayIndex);
    }

    private static DateOnly GetWeekEnd(DateOnly date)
    {
        return GetWeekStart(date).AddDays(6);
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
