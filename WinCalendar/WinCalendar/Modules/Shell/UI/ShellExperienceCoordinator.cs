using System;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Shell.UI;

public sealed class ShellExperienceCoordinator : IDisposable
{
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;
    private readonly Action _requestExit;
    private TaskbarCalendarOverlayHost? _taskbarCalendarOverlayHost;
    private TrayIconHost? _trayIconHost;
    private bool _started;
    private bool _isStopping;

    public ShellExperienceCoordinator(PlannerWindowCoordinator plannerWindowCoordinator, Action requestExit)
    {
        _plannerWindowCoordinator = plannerWindowCoordinator;
        _requestExit = requestExit;
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _trayIconHost = new TrayIconHost(
            _plannerWindowCoordinator.ShowFullApp,
            _requestExit);

        Microsoft.UI.Dispatching.DispatcherQueue? dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is not null)
        {
            _taskbarCalendarOverlayHost = new TaskbarCalendarOverlayHost(
                dispatcherQueue,
                _plannerWindowCoordinator.ToggleCompactPanel);
        }
    }

    public void Dispose()
    {
        if (_isStopping)
            return;

        _isStopping = true;
        _taskbarCalendarOverlayHost?.Dispose();
        _taskbarCalendarOverlayHost = null;
        _trayIconHost?.Dispose();
        _trayIconHost = null;
    }
}
