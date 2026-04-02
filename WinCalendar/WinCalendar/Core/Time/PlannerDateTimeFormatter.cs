using System;

namespace WinCalendar.Core.Time;

internal static class PlannerDateTimeFormatter
{
    public static string FormatDate(DateOnly date)
    {
        return date.ToString("dd.MM.yyyy");
    }

    public static string FormatShortDay(DateOnly date)
    {
        return date.ToString("ddd dd.MM");
    }

    public static string FormatAgendaHeader(DateOnly date)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

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
        return time?.ToString("HH:mm") ?? "No time";
    }

    public static string FormatDateTime(DateOnly date, TimeOnly? time)
    {
        return time is null
            ? FormatDate(date)
            : $"{FormatDate(date)} {FormatTime(time)}";
    }
}
