using System;

namespace WinCalendar.Core.Time;

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;

    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(Now);
}
