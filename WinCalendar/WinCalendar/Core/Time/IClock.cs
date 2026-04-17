using System;

namespace WinCalendar.Core.Time;

public interface IClock
{
    DateTime Now { get; }

    DateTime UtcNow { get; }

    DateOnly Today { get; }

    TimeOnly TimeOfDay { get; }
}
