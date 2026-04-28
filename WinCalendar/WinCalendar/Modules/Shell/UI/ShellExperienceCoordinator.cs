using System;
using Microsoft.UI.Dispatching;
using WinCalendar.Shared.Settings;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Shell.UI;

public sealed class ShellExperienceCoordinator : IDisposable
{
    private readonly PlannerWindowCoordinator _plannerWindowCoordinator;
    private readonly AppSettingsStore _appSettingsStore;
    private readonly Action _requestExit;
    private TaskbarCalendarOverlayHost? _taskbarCalendarOverlayHost;
    private TrayIconHost? _trayIconHost;
    private bool _started;
    private bool _isStopping;

    public ShellExperienceCoordinator(
        PlannerWindowCoordinator plannerWindowCoordinator,
        AppSettingsStore appSettingsStore,
        Action requestExit)
    {
        _plannerWindowCoordinator = plannerWindowCoordinator;
        _appSettingsStore = appSettingsStore;
        _requestExit = requestExit;
        _appSettingsStore.OverlaySettingsChanged += AppSettingsStore_OverlaySettingsChanged;
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _trayIconHost = new TrayIconHost(
            _plannerWindowCoordinator.ShowFullApp,
            _requestExit);

        EnsureOverlayHost();
    }

    public void BeginOverlayResizeMode()
    {
        EnsureOverlayHost();
        _taskbarCalendarOverlayHost?.ToggleResizeMode();
    }

    public void Dispose()
    {
        if (_isStopping)
            return;

        _isStopping = true;
        _appSettingsStore.OverlaySettingsChanged -= AppSettingsStore_OverlaySettingsChanged;
        _taskbarCalendarOverlayHost?.Dispose();
        _taskbarCalendarOverlayHost = null;
        _trayIconHost?.Dispose();
        _trayIconHost = null;
    }

    private void AppSettingsStore_OverlaySettingsChanged(object? sender, EventArgs e)
    {
        if (!_started || _isStopping)
            return;

        EnsureOverlayHost();
    }

    private void EnsureOverlayHost()
    {
        if (!_appSettingsStore.OverlayEnabled)
        {
            _taskbarCalendarOverlayHost?.Dispose();
            _taskbarCalendarOverlayHost = null;
            return;
        }

        if (_taskbarCalendarOverlayHost is not null)
            return;

        DispatcherQueue? dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is null)
            return;

        _taskbarCalendarOverlayHost = new TaskbarCalendarOverlayHost(
            dispatcherQueue,
            _appSettingsStore,
            _plannerWindowCoordinator.ToggleCompactPanel);
    }
}
