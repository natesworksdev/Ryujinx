using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Ryujinx.Ava.Common.Locale;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Ava.UI.Helpers
{
    public static class NotificationHelper
    {
        private const int MaxNotifications      = 4;
        private const int NotificationDelayInMs = 5000;

        private static WindowNotificationManager _notificationManager;

        private static readonly ManualResetEventSlim             _templateAppliedEvent = new(false);
        private static readonly BlockingCollection<Notification> _notifications        = new();

        public static void SetNotificationManager(Window host)
        {
            _notificationManager = new WindowNotificationManager(host)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = MaxNotifications,
                Margin   = new Thickness(0, 0, 15, 40)
            };

            _notificationManager.TemplateApplied += (sender, args) =>
            {
                _templateAppliedEvent.Set();
            };

            // Ordinarily you'd want to Dispose() these, but we're using this one until the application start shutting down.
            // The process will be ending soon (right?!) so we're not too concerned about proper disposal.
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            host.Closing += (sender, args) =>
            {
                cancellationTokenSource.Cancel();
                _notifications.CompleteAdding();
            };

            var notificationThread = new Thread(() =>
            {
                _templateAppliedEvent.Wait(cancellationTokenSource.Token);

                try
                {
                    foreach (var notification in _notifications.GetConsumingEnumerable(cancellationTokenSource.Token))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            _notificationManager.Show(notification);
                        });

                        bool isCancelled = cancellationTokenSource.Token.WaitHandle.WaitOne(NotificationDelayInMs / MaxNotifications);
                        if (isCancelled)
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Do nothing.
                }
            })
            {
                Name = "UI.NotificationThread",
                IsBackground = true,
            };

            notificationThread.Start();
        }

        public static void Show(string title, string text, NotificationType type, bool waitingExit = false, Action onClick = null, Action onClose = null)
        {
            var delay = waitingExit ? TimeSpan.FromMilliseconds(0) : TimeSpan.FromMilliseconds(NotificationDelayInMs);

            _notifications.Add(new Notification(title, text, type, delay, onClick, onClose));
        }

        public static void ShowError(string message)
        {
            Show(LocaleManager.Instance[LocaleKeys.DialogErrorTitle], $"{LocaleManager.Instance[LocaleKeys.DialogErrorMessage]}\n\n{message}", NotificationType.Error);
        }
    }
}