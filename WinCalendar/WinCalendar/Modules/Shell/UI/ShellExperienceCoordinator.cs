using System;
using Microsoft.UI.Dispatching;
using WinCalendar.Modules.Shell.Infrastructure.Win32;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Shell.UI;

public sealed class ShellExperienceCoordinator : IDisposable
{
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;
    private readonly Action _requestExit;
    private TaskbarClockClickInterceptor? _taskbarClockClickInterceptor;
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

        DispatcherQueue? dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is not null)
        {
            _taskbarClockClickInterceptor = new TaskbarClockClickInterceptor(
                dispatcherQueue,
                _plannerWindowCoordinator.ShowCompactPanel);
        }
    }

    public void Dispose()
    {
        if (_isStopping)
            return;

        _isStopping = true;
        _taskbarClockClickInterceptor?.Dispose();
        _taskbarClockClickInterceptor = null;
        _trayIconHost?.Dispose();
        _trayIconHost = null;
    }
}
