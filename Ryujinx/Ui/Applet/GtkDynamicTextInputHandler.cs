using Gtk;
using Ryujinx.HLE;
using Ryujinx.Ui.Widgets;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ui.Applet
{
    internal class GtkDynamicTextInputHandler : IDynamicTextInputHandler
    {
        private const int ForceOperationWaitMilliseconds = 3000;

        private readonly Window _parent;
        private readonly RawInputToTextEntry _inputToTextEntry = new RawInputToTextEntry();

        private readonly Gdk.Key _acceptKey;
        private readonly Gdk.Key _cancelKey;

        private CancellationTokenSource _forceEventCancel = null;
        private object _forceEventLock = new object();

        public event DynamicTextChangedEvent TextChanged;

        public GtkDynamicTextInputHandler(Window parent, Gdk.Key acceptKey, Gdk.Key cancelKey)
        {
            _parent = parent;
            _parent.KeyPressEvent += HandleKeyPressEvent;
            _parent.KeyReleaseEvent += HandleKeyReleaseEvent;
            _acceptKey = acceptKey;
            _cancelKey = cancelKey;
        }

        [GLib.ConnectBefore()]
        private void HandleKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (args.Event.Key == _acceptKey)
            {
                InvokeTextChanged(true, false);
                _inputToTextEntry.Text = "";
            }
            else if (args.Event.Key == _cancelKey)
            {
                InvokeTextChanged(false, true);
                _inputToTextEntry.Text = "";
            }
            else
            {
                _inputToTextEntry.SendKeyPressEvent(o, args);
                InvokeTextChanged(false, false);
            }
        }

        private void InvokeTextChanged(bool isAccept, bool isCancel)
        {
            string text = _inputToTextEntry.Text;

            _inputToTextEntry.GetSelectionBounds(out int selectionStart, out int selectionEnd);

            if (isAccept || isCancel)
            {
                lock (_forceEventLock)
                {
                    if (_forceEventCancel == null)
                    {
                        var eventCancel = new CancellationTokenSource();

                        Task.Run(() =>
                        {
                            var token = eventCancel.Token;

                            if (!token.WaitHandle.WaitOne(ForceOperationWaitMilliseconds))
                            {
                                TextChanged?.Invoke(text, selectionStart, selectionEnd, isAccept, isCancel, true);
                            }

                            lock (_forceEventLock)
                            {
                                _forceEventCancel = null;
                            }
                        });

                        _forceEventCancel = eventCancel;
                    }
                }
            }

            TextChanged?.Invoke(text, selectionStart, selectionEnd, isAccept, isCancel, false);
        }

        [GLib.ConnectBefore()]
        private void HandleKeyReleaseEvent(object o, KeyReleaseEventArgs args)
        {
            if (args.Event.Key == _acceptKey || args.Event.Key == _cancelKey)
            {
                lock (_forceEventLock)
                {
                    if (_forceEventCancel != null)
                    {
                        _forceEventCancel.Cancel();
                        _forceEventCancel = null;
                    }
                }
            }

           _inputToTextEntry.SendKeyReleaseEvent(o, args);
        }

        public void SetText(string text)
        {
            _inputToTextEntry.Text = text;
            _inputToTextEntry.Position = text.Length;
        }

        public void SetMaxLength(int maxLength)
        {
            _inputToTextEntry.MaxLength = maxLength;
        }

        public void Dispose()
        {
            _parent.KeyPressEvent -= HandleKeyPressEvent;
            _parent.KeyReleaseEvent -= HandleKeyReleaseEvent;
        }
    }
}