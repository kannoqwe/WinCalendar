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

    private readonly CreateTaskUseCase _createTaskUseCase;
    private readonly DeleteTaskUseCase _deleteTaskUseCase;
    private readonly GetTasksForRangeUseCase _getTasksForRangeUseCase;
    private readonly SetTaskCompletionStatusUseCase _setTaskCompletionStatusUseCase;
    private readonly UpdateTaskUseCase _updateTaskUseCase;
    private readonly Dictionary<DateOnly, (bool IsExpanded, bool IsHidden)> _agendaState = [];

    private bool _isInitialized;
    private bool _isCompactSidebarOpen;
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
    private string _editorTitle = string.Empty;
    private bool _editorHasTime;
    private TimeSpan _editorTime = new(9, 0, 0);
    private DateTimeOffset _editorDate = DateTimeOffset.Now;

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
    }

    public ObservableCollection<PlannerMonthDayViewModel> MonthDays { get; }

    public ObservableCollection<PlannerTaskViewModel> SelectedDayTasks { get; }

    public ObservableCollection<PlannerTaskViewModel> TodayTasks { get; }

    public ObservableCollection<PlannerAgendaDayViewModel> WeekAgendaDays { get; }

    public DateOnly SelectedDate => _selectedDate;

    public string SelectedDateText
    {
        get => _selectedDateText;
        private set => SetProperty(ref _selectedDateText, value);
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

    public bool IsCompactSidebarOpen
    {
        get => _isCompactSidebarOpen;
        private set
        {
            if (!SetProperty(ref _isCompactSidebarOpen, value))
                return;

            OnPropertyChanged(nameof(CompactSidebarVisibility));
            OnPropertyChanged(nameof(CompactSidebarToggleText));
            OnPropertyChanged(nameof(CompactPanelRootPadding));
            OnPropertyChanged(nameof(CompactPanelCornerRadius));
            OnPropertyChanged(nameof(CompactSidebarCornerRadius));
        }
    }

    public Visibility CompactSidebarVisibility =>
        IsCompactSidebarOpen ? Visibility.Visible : Visibility.Collapsed;

    public string CompactSidebarToggleText => IsCompactSidebarOpen ? ">" : "<";

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
        }
    }

    public Visibility SelectedTaskVisibility =>
        SelectedTask is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility EmptySelectedTaskVisibility =>
        SelectedTask is null ? Visibility.Visible : Visibility.Collapsed;

    public string EditorTitle
    {
        get => _editorTitle;
        set => SetProperty(ref _editorTitle, value);
    }

    public bool EditorHasTime
    {
        get => _editorHasTime;
        set => SetProperty(ref _editorHasTime, value);
    }

    public TimeSpan EditorTime
    {
        get => _editorTime;
        set => SetProperty(ref _editorTime, value);
    }

    public DateTimeOffset EditorDate
    {
        get => _editorDate;
        set => SetProperty(ref _editorDate, value);
    }

    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await ReloadAsync();
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

    public Task GoToPreviousDayAsync()
    {
        return SelectDateAsync(_selectedDate.AddDays(-1));
    }

    public Task GoToNextDayAsync()
    {
        return SelectDateAsync(_selectedDate.AddDays(1));
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

    public void ToggleAgendaDayHidden(PlannerAgendaDayViewModel day)
    {
        day.IsHidden = !day.IsHidden;

        if (day.IsHidden)
            day.IsExpanded = false;

        UpdateAgendaState(day);
    }

    public async Task AddTaskAsync(string title, DateOnly? date = null, TimeOnly? time = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        DateOnly targetDate = date ?? _selectedDate;
        await _createTaskUseCase.ExecuteAsync(title, targetDate, time);
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
            _selectedTaskId = null;

        await ReloadAsync();
    }

    public void SelectTask(PlannerTaskViewModel? task)
    {
        SelectedTask = task;

        if (task is null)
        {
            EditorTitle = string.Empty;
            EditorHasTime = false;
            EditorTime = new TimeSpan(9, 0, 0);
            EditorDate = new DateTimeOffset(_selectedDate.ToDateTime(TimeOnly.MinValue));
            return;
        }

        EditorTitle = task.Title;
        EditorHasTime = task.HasTime;
        EditorTime = task.Time?.ToTimeSpan() ?? new TimeSpan(9, 0, 0);
        EditorDate = new DateTimeOffset(task.Date.ToDateTime(TimeOnly.MinValue));
    }

    public async Task SaveSelectedTaskAsync()
    {
        if (SelectedTask is null)
            return;

        if (string.IsNullOrWhiteSpace(EditorTitle))
            return;

        DateOnly date = DateOnly.FromDateTime(EditorDate.Date);
        TimeOnly? time = EditorHasTime ? TimeOnly.FromTimeSpan(EditorTime) : null;

        await _updateTaskUseCase.ExecuteAsync(SelectedTask.Id, EditorTitle, date, time);
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
        DateOnly todayWeekStart = GetWeekStart(today);
        DateOnly todayWeekEnd = todayWeekStart.AddDays(6);

        DateOnly rangeStart = Min(monthGridStart, selectedWeekStart, todayWeekStart, today);
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
        BuildWeekAgenda(taskLookup, selectedWeekStart);
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

    private void BuildWeekAgenda(IReadOnlyDictionary<DateOnly, List<PlannerTaskViewModel>> taskLookup, DateOnly weekStart)
    {
        WeekAgendaDays.Clear();

        for (int offset = 0; offset < 7; offset++)
        {
            DateOnly date = weekStart.AddDays(offset);
            _agendaState.TryGetValue(date, out (bool IsExpanded, bool IsHidden) state);
            List<PlannerTaskViewModel> tasksForDay = taskLookup.TryGetValue(date, out List<PlannerTaskViewModel>? tasks)
                ? tasks
                : [];

            WeekAgendaDays.Add(new PlannerAgendaDayViewModel(
                date,
                tasksForDay,
                state.IsExpanded || !_agendaState.ContainsKey(date),
                state.IsHidden,
                date == _selectedDate));
        }
    }

    private void UpdateSummaries(DateOnly weekStart, DateOnly weekEnd)
    {
        SelectedDateText = PlannerDateTimeFormatter.FormatDate(_selectedDate);
        SelectedDateSummary = SelectedDayTasks.Count == 0
            ? "No tasks for selected day"
            : $"{SelectedDayTasks.Count} task{(SelectedDayTasks.Count == 1 ? string.Empty : "s")} on selected day";

        MonthLabel = PlannerDateTimeFormatter.FormatMonthTitle(_displayMonth);
        WeekLabel = PlannerDateTimeFormatter.FormatWeekRange(weekStart, weekEnd);
        TodaySummary = TodayTasks.Count == 0
            ? "Today is clear"
            : $"{TodayTasks.Count} task{(TodayTasks.Count == 1 ? string.Empty : "s")} today";

        int weekTaskCount = WeekAgendaDays.Sum(day => day.Tasks.Count);
        WeekSummary = weekTaskCount == 0
            ? "No tasks this week"
            : $"{weekTaskCount} task{(weekTaskCount == 1 ? string.Empty : "s")} this week";
    }

    private void ReselectTask()
    {
        PlannerTaskViewModel? task = _selectedTaskId is null
            ? SelectedDayTasks.FirstOrDefault()
            : SelectedDayTasks.FirstOrDefault(current => current.Id == _selectedTaskId)
                ?? WeekAgendaDays.SelectMany(day => day.Tasks).FirstOrDefault(current => current.Id == _selectedTaskId);

        SelectTask(task);
    }

    private void UpdateAgendaState(PlannerAgendaDayViewModel day)
    {
        _agendaState[day.Date] = (day.IsExpanded, day.IsHidden);
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
}
