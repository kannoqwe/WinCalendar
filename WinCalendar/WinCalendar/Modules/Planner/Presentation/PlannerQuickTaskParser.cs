using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Planner.App.Modules.Tasks.Entities;

namespace WinCalendar.Modules.Planner.Presentation;

public static partial class PlannerQuickTaskParser
{
    public static PlannerQuickTaskParseResult? Parse(string input, DateOnly selectedDate, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string title = input.Trim();
        DateOnly date = selectedDate;
        TimeOnly? time = null;
        int? durationMinutes = null;
        TaskRecurrencePattern recurrencePattern = TaskRecurrencePattern.None;
        TaskCategory category = TaskCategory.Work;

        foreach ((string token, DateOnly tokenDate) in GetDateTokens(today))
        {
            title = RemoveToken(title, token, () => date = tokenDate);
        }

        foreach ((string token, TaskRecurrencePattern pattern) in GetRecurrenceTokens())
        {
            title = RemoveToken(title, token, () => recurrencePattern = pattern);
        }

        foreach ((string token, TaskCategory tokenCategory) in GetCategoryTokens())
        {
            title = RemoveToken(title, token, () => category = tokenCategory);
        }

        title = TimeRegex().Replace(title, match =>
        {
            if (time is not null)
                return match.Value;

            int hour = int.Parse(match.Groups["hour"].Value);
            int minute = int.Parse(match.Groups["minute"].Value);
            time = new TimeOnly(hour, minute);
            return " ";
        });

        title = DurationRegex().Replace(title, match =>
        {
            if (durationMinutes is not null)
                return match.Value;

            int value = int.Parse(match.Groups["value"].Value);
            string unit = match.Groups["unit"].Value.ToLowerInvariant();
            durationMinutes = unit is "h" or "hour" or "hours" or "ч" or "час" or "часа" or "часов"
                ? value * 60
                : value;
            return " ";
        });

        title = NormalizeTitle(title);
        if (string.IsNullOrWhiteSpace(title))
            return null;

        return new PlannerQuickTaskParseResult(title, date, time, durationMinutes, recurrencePattern, category);
    }

    private static IEnumerable<(string Token, DateOnly Date)> GetDateTokens(DateOnly today)
    {
        yield return ("today", today);
        yield return ("сегодня", today);
        yield return ("tomorrow", today.AddDays(1));
        yield return ("завтра", today.AddDays(1));
    }

    private static IEnumerable<(string Token, TaskRecurrencePattern Pattern)> GetRecurrenceTokens()
    {
        yield return ("daily", TaskRecurrencePattern.Daily);
        yield return ("every day", TaskRecurrencePattern.Daily);
        yield return ("ежедневно", TaskRecurrencePattern.Daily);
        yield return ("каждый день", TaskRecurrencePattern.Daily);
        yield return ("weekly", TaskRecurrencePattern.Weekly);
        yield return ("every week", TaskRecurrencePattern.Weekly);
        yield return ("еженедельно", TaskRecurrencePattern.Weekly);
        yield return ("каждую неделю", TaskRecurrencePattern.Weekly);
        yield return ("monthly", TaskRecurrencePattern.Monthly);
        yield return ("every month", TaskRecurrencePattern.Monthly);
        yield return ("ежемесячно", TaskRecurrencePattern.Monthly);
        yield return ("каждый месяц", TaskRecurrencePattern.Monthly);
    }

    private static string RemoveToken(string input, string token, Action onMatched)
    {
        string pattern = $@"(?<!\p{{L}}){Regex.Escape(token)}(?!\p{{L}})";
        bool matched = false;
        string result = Regex.Replace(
            input,
            pattern,
            match =>
            {
                matched = true;
                return " ";
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (matched)
            onMatched();

        return result;
    }

    private static IEnumerable<(string Token, TaskCategory Category)> GetCategoryTokens()
    {
        yield return ("work", TaskCategory.Work);
        yield return ("работа", TaskCategory.Work);
        yield return ("personal", TaskCategory.Personal);
        yield return ("личное", TaskCategory.Personal);
        yield return ("health", TaskCategory.Health);
        yield return ("здоровье", TaskCategory.Health);
        yield return ("important", TaskCategory.Important);
        yield return ("важно", TaskCategory.Important);
    }

    private static string NormalizeTitle(string title)
    {
        return WhitespaceRegex().Replace(title, " ").Trim();
    }

    [GeneratedRegex(@"\b(?<hour>[01]?\d|2[0-3])[:.](?<minute>[0-5]\d)\b", RegexOptions.CultureInvariant)]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"\b(?<value>\d{1,3})\s?(?<unit>m|min|mins|minute|minutes|мин|h|hour|hours|ч|час|часа|часов)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}

public sealed record PlannerQuickTaskParseResult(
    string Title,
    DateOnly Date,
    TimeOnly? Time,
    int? DurationMinutes,
    TaskRecurrencePattern RecurrencePattern,
    TaskCategory Category);
