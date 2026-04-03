using Microsoft.UI.Xaml;
using WinCalendar.Bootstrap;

namespace WinCalendar
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly AppBootstrapper _bootstrapper;
        private readonly AppSingleInstanceRelay _singleInstanceRelay;
        private Microsoft.UI.Dispatching.DispatcherQueue? _uiDispatcherQueue;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            _bootstrapper = new AppBootstrapper(RequestExit);
            _singleInstanceRelay = new AppSingleInstanceRelay();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _uiDispatcherQueue ??= Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            AppLaunchMode launchMode = AppLaunchArguments.Parse(args.Arguments);

            if (!_singleInstanceRelay.IsPrimaryInstance)
            {
                _singleInstanceRelay.TryForwardToPrimary(launchMode);
                Exit();
                return;
            }

            _singleInstanceRelay.StartListening(mode =>
            {
                _uiDispatcherQueue?.TryEnqueue(() => _bootstrapper.Launch(mode));
            });

            _bootstrapper.Launch(launchMode);
        }

        private void RequestExit()
        {
            _bootstrapper.Shutdown();
            _singleInstanceRelay.Dispose();
            Exit();
        }
    }
}
