using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace WinCalendar.Bootstrap;

internal sealed class AppSingleInstanceRelay : IDisposable
{
    private readonly Mutex _singleInstanceMutex;
    private readonly string _pipeName;
    private readonly bool _isPrimaryInstance;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isListening;
    private bool _isDisposed;

    public AppSingleInstanceRelay()
    {
        string userKey = Environment.UserName
            .Replace('\\', '_')
            .Replace('/', '_')
            .Replace(' ', '_');

        string instanceKey = $"WinCalendar.{userKey}";
        _singleInstanceMutex = new Mutex(initiallyOwned: true, $@"Local\{instanceKey}", out _isPrimaryInstance);
        _pipeName = $"{instanceKey}.ActivationPipe";
    }

    public bool IsPrimaryInstance => _isPrimaryInstance;

    public bool TryForwardToPrimary(AppLaunchMode launchMode)
    {
        if (_isPrimaryInstance)
            return false;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            using NamedPipeClientStream pipeClient = new(".", _pipeName, PipeDirection.Out);

            try
            {
                pipeClient.Connect(250);

                using StreamWriter writer = new(pipeClient) { AutoFlush = true };
                writer.WriteLine(AppLaunchArguments.ToPipePayload(launchMode));
                return true;
            }
            catch (IOException)
            {
            }
            catch (TimeoutException)
            {
            }

            Thread.Sleep(150);
        }

        return false;
    }

    public void StartListening(Action<AppLaunchMode> activationHandler)
    {
        if (!_isPrimaryInstance || _isListening)
            return;

        _isListening = true;
        _ = Task.Run(() => ListenAsync(activationHandler, _cancellationTokenSource.Token));
    }

    private async Task ListenAsync(Action<AppLaunchMode> activationHandler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeServerStream pipeServer = new(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(cancellationToken);

                using StreamReader reader = new(pipeServer);
                string? payload = await reader.ReadLineAsync();

                if (AppLaunchArguments.TryParsePipePayload(payload, out AppLaunchMode launchMode))
                    activationHandler(launchMode);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _cancellationTokenSource.Cancel();
        _singleInstanceMutex.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
