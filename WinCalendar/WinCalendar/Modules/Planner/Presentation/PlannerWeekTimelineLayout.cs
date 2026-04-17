using System;
using System.Collections.Generic;
using System.Linq;

namespace WinCalendar.Modules.Planner.Presentation;

internal static class PlannerWeekTimelineLayout
{
    public const double HourHeight = 40;
    public const double DayWidth = 132;
    public const double AnyTimeLaneHeight = 64;
    public const int DefaultTaskDurationMinutes = 45;

    private const double TaskHorizontalPadding = 4;
    private const double TaskColumnGap = 4;
    private const double TaskMinimumHeight = 28;

    public static List<PlannerWeekTaskBlockViewModel> BuildTimedTaskBlocks(IEnumerable<PlannerTaskViewModel> tasks)
    {
        List<WeekTaskLayoutItem> items = tasks
            .Where(task => task.HasTime)
            .OrderBy(task => task.Time)
            .ThenBy(task => task.Title)
            .Select(task => new WeekTaskLayoutItem(task, GetTaskStartMinutes(task), GetTaskEndMinutes(task)))
            .ToList();

        if (items.Count == 0)
            return [];

        foreach (List<WeekTaskLayoutItem> group in BuildTaskGroups(items))
            AssignColumns(group);

        return items
            .Select(CreateTaskBlock)
            .ToList();
    }

    private static int GetTaskStartMinutes(PlannerTaskViewModel task)
    {
        TimeSpan time = task.Time!.Value.ToTimeSpan();
        return (time.Hours * 60) + time.Minutes;
    }

    private static int GetTaskEndMinutes(PlannerTaskViewModel task)
    {
        int durationMinutes = task.DurationMinutes ?? DefaultTaskDurationMinutes;
        return Math.Min(24 * 60, GetTaskStartMinutes(task) + durationMinutes);
    }

    private static List<List<WeekTaskLayoutItem>> BuildTaskGroups(List<WeekTaskLayoutItem> items)
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

    private static void AssignColumns(List<WeekTaskLayoutItem> group)
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

    private static PlannerWeekTaskBlockViewModel CreateTaskBlock(WeekTaskLayoutItem item)
    {
        double usableWidth = DayWidth - (TaskHorizontalPadding * 2);
        double width = (usableWidth - ((item.TotalColumns - 1) * TaskColumnGap)) / item.TotalColumns;
        double clampedWidth = Math.Max(32, width);
        double top = (item.StartMinutes / 60d) * HourHeight;
        double height = Math.Max(
            TaskMinimumHeight,
            ((item.EndMinutes - item.StartMinutes) / 60d) * HourHeight);

        return new PlannerWeekTaskBlockViewModel(
            item.Task,
            top,
            TaskHorizontalPadding + (item.Column * (clampedWidth + TaskColumnGap)),
            clampedWidth,
            height);
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
}
