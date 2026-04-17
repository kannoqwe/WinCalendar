using System;
using WinCalendar.Core.Time;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Shell.UI;

public sealed class ShellExperienceCoordinator : IDisposable
{
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;
    private readonly IClock _clock;
    private readonly Action _requestExit;
    private TaskbarCornerButtonHost? _taskbarCornerButtonHost;
    private TrayIconHost? _trayIconHost;
    private bool _started;
    private bool _isStopping;

    public ShellExperienceCoordinator(PlannerWindowCoordinator plannerWindowCoordinator, IClock clock, Action requestExit)
    {
        _plannerWindowCoordinator = plannerWindowCoordinator;
        _clock = clock;
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
            _taskbarCornerButtonHost = new TaskbarCornerButtonHost(
                dispatcherQueue,
                _clock,
                _plannerWindowCoordinator.ShowFullApp);
        }
    }

    public void Dispose()
    {
        if (_isStopping)
            return;

        _isStopping = true;
        _taskbarCornerButtonHost?.Dispose();
        _taskbarCornerButtonHost = null;
        _trayIconHost?.Dispose();
        _trayIconHost = null;
    }
}
