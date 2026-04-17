using System;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinCalendar.Core.Time;
using WinCalendar.Shared.Windowing;

namespace WinCalendar.Modules.Shell.UI;

internal sealed class TaskbarCornerButtonHost : IDisposable
{
    private const int ButtonWidth = 132;
    private const int ButtonHeight = 44;
    private const int ButtonCornerRadius = 12;
    private const int ScreenMargin = 10;

    private readonly IClock _clock;
    private readonly Action _onClick;
    private readonly DispatcherQueueTimer _syncTimer;
    private TaskbarCornerButtonWindow? _buttonWindow;
    private bool _isDisposed;

    public TaskbarCornerButtonHost(DispatcherQueue dispatcherQueue, IClock clock, Action onClick)
    {
        _clock = clock;
        _onClick = onClick;
        _syncTimer = dispatcherQueue.CreateTimer();
        _syncTimer.Interval = TimeSpan.FromSeconds(15);
        _syncTimer.IsRepeating = true;
        _syncTimer.Tick += SyncTimer_Tick;

        SyncButtonWindow();
        _syncTimer.Start();
    }

    private void SyncTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        SyncButtonWindow();
    }

    private void SyncButtonWindow()
    {
        if (_isDisposed)
            return;

        if (_buttonWindow is null)
            CreateButtonWindow();

        if (_buttonWindow is null)
            return;

        _buttonWindow.Refresh();
        PositionButtonWindow(_buttonWindow.GetAppWindow());
    }

    private void CreateButtonWindow()
    {
        _buttonWindow = new TaskbarCornerButtonWindow(_clock, _onClick);
        _buttonWindow.Closed += ButtonWindow_Closed;

        AppWindow appWindow = _buttonWindow.GetAppWindow();
        OverlappedPresenter presenter = OverlappedPresenter.CreateForToolWindow();
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = false;
        presenter.SetBorderAndTitleBar(false, false);

        appWindow.SetPresenter(presenter);
        appWindow.Resize(new SizeInt32(ButtonWidth, ButtonHeight));
        PositionButtonWindow(appWindow);
        TransparentWindowHost.Apply(
            _buttonWindow,
            ButtonWidth,
            ButtonHeight,
            ButtonCornerRadius,
            TransparentWindowHost.WindowOutlineShape.AllRounded);

        _buttonWindow.Activate();
    }

    private static void PositionButtonWindow(AppWindow appWindow)
    {
        DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 bounds = displayArea.OuterBounds;

        appWindow.Move(new PointInt32(
            bounds.X + bounds.Width - ButtonWidth - ScreenMargin,
            bounds.Y + bounds.Height - ButtonHeight - ScreenMargin));
    }

    private void ButtonWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is not TaskbarCornerButtonWindow window || !ReferenceEquals(_buttonWindow, window))
            return;

        _buttonWindow.Closed -= ButtonWindow_Closed;
        _buttonWindow = null;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _syncTimer.Stop();
        _syncTimer.Tick -= SyncTimer_Tick;

        if (_buttonWindow is not null)
        {
            _buttonWindow.Closed -= ButtonWindow_Closed;
            _buttonWindow.Close();
            _buttonWindow = null;
        }
    }

    private sealed class TaskbarCornerButtonWindow : Window
    {
        private readonly IClock _clock;
        private readonly Action _onClick;
        private readonly TextBlock _timeText;
        private readonly TextBlock _dateText;

        public TaskbarCornerButtonWindow(IClock clock, Action onClick)
        {
            _clock = clock;
            _onClick = onClick;
            _timeText = CreateTimeText();
            _dateText = CreateDateText();

            Title = "WinCalendar Clock";
            SystemBackdrop = new DesktopAcrylicBackdrop();
            Content = CreateContent();
            Refresh();
        }

        public void Refresh()
        {
            DateTime now = _clock.Now;
            _timeText.Text = now.ToString("HH:mm");
            _dateText.Text = now.ToString("dd.MM");
        }

        private Border CreateContent()
        {
            Grid content = new()
            {
                ColumnSpacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(_timeText, 0);
            Grid.SetColumn(_dateText, 1);
            content.Children.Add(_timeText);
            content.Children.Add(_dateText);

            Border button = new()
            {
                Background = new SolidColorBrush(ColorHelper.FromArgb(224, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(13, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(ButtonCornerRadius),
                Padding = new Thickness(14, 0, 14, 0),
                Child = content
            };

            button.Tapped += Button_Tapped;
            return button;
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            _onClick();
        }

        private static TextBlock CreateTimeText() => new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI Variable Text"),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 26, 26, 26))
        };

        private static TextBlock CreateDateText() => new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI Variable Text"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResolveAccentBrush(),
            TextAlignment = TextAlignment.Right
        };

        private static Brush ResolveAccentBrush()
        {
            if (Application.Current.Resources["AppAccentBrush"] is Brush accentBrush)
                return accentBrush;

            return new SolidColorBrush(ColorHelper.FromArgb(255, 255, 0, 136));
        }
    }
}
