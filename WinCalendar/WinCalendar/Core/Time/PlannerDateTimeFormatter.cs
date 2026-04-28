using System;
using WinCalendar.Shared.Settings;

namespace WinCalendar.Core.Time;

internal static class PlannerDateTimeFormatter
{
    public static AppTimeFormatPreference TimeFormatPreference { get; set; } = AppTimeFormatPreference.TwentyFourHour;

    public static string FormatDate(DateOnly date)
    {
        return date.ToString("dd.MM.yyyy");
    }

    public static string FormatShortDay(DateOnly date)
    {
        return date.ToString("ddd dd.MM");
    }

    public static string FormatAgendaHeader(DateOnly date, DateOnly today)
    {
        if (date == today)
            return "Today";

        if (date == today.AddDays(1))
            return "Tomorrow";

        return date.ToString("dddd");
    }

    public static string FormatMonthTitle(DateOnly date)
    {
        return date.ToString("MMMM yyyy");
    }

    public static string FormatWeekRange(DateOnly startDate, DateOnly endDate)
    {
        return $"{FormatDate(startDate)} - {FormatDate(endDate)}";
    }

    public static string FormatTime(TimeOnly? time)
    {
        return time is null
            ? "No time"
            : FormatClockTime(time.Value);
    }

    public static string FormatDuration(int? durationMinutes)
    {
        if (durationMinutes is not > 0)
            return string.Empty;

        int hours = durationMinutes.Value / 60;
        int minutes = durationMinutes.Value % 60;

        if (hours > 0 && minutes > 0)
            return $"{hours}h {minutes}m";

        if (hours > 0)
            return $"{hours}h";

        return $"{minutes} min";
    }

    public static string FormatCompactTime(TimeOnly? time, int? durationMinutes = null)
    {
        if (time is null)
            return "Any time";

        string durationText = FormatDuration(durationMinutes);
        return string.IsNullOrWhiteSpace(durationText)
            ? FormatTime(time)
            : $"{FormatTime(time)} | {durationText}";
    }

    public static string FormatTimeRange(TimeOnly? time, int? durationMinutes = null)
    {
        if (time is null)
            return FormatTime(time);

        if (durationMinutes is not > 0)
            return FormatTime(time);

        int startMinutes = (int)time.Value.ToTimeSpan().TotalMinutes;
        int endMinutes = Math.Min(24 * 60, startMinutes + durationMinutes.Value);
        string endText = endMinutes == 24 * 60
            ? "24:00"
            : FormatClockTime(TimeOnly.MinValue.Add(TimeSpan.FromMinutes(endMinutes)));

        return $"{FormatTime(time)} - {endText}";
    }

    public static string FormatDateTime(DateOnly date, TimeOnly? time, int? durationMinutes = null)
    {
        return time is null
            ? FormatDate(date)
            : $"{FormatDate(date)} {FormatTimeRange(time, durationMinutes)}";
    }

    private static string FormatClockTime(TimeOnly time)
    {
        return TimeFormatPreference == AppTimeFormatPreference.TwelveHour
            ? time.ToString("h:mm tt")
            : time.ToString("HH:mm");
    }
}
