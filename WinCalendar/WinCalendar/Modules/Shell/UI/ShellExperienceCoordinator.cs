using System;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Shell.UI;

public sealed class ShellExperienceCoordinator : IDisposable
{
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;
    private readonly Action _requestExit;
    private TaskbarClockOverlayHost? _taskbarClockOverlayHost;
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
            _plannerWindowCoordinator.ShowCompactPanel,
            _requestExit);

        Microsoft.UI.Dispatching.DispatcherQueue? dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is not null)
        {
            _taskbarClockOverlayHost = new TaskbarClockOverlayHost(
                dispatcherQueue,
                _plannerWindowCoordinator.ToggleCompactPanel);
        }
    }

    public void Dispose()
    {
        if (_isStopping)
            return;

        _isStopping = true;
        _taskbarClockOverlayHost?.Dispose();
        _taskbarClockOverlayHost = null;
        _trayIconHost?.Dispose();
        _trayIconHost = null;
    }
}
