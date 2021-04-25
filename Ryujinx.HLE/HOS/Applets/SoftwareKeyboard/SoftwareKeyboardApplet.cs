using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Applets.SoftwareKeyboard;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class SoftwareKeyboardApplet : IApplet
    {
        private const string DefaultText = "Ryujinx";

        private const int SoftUnlockerDelayMilliseconds = 500;

        private readonly Switch _device;

        private const int StandardBufferSize    = 0x7D8;
        private const int InteractiveBufferSize = 0x7D4;
        private const int MaxUserWords          = 0x1388;

        private SoftwareKeyboardState _foregroundState = SoftwareKeyboardState.Uninitialized;
        private volatile InlineKeyboardState _backgroundState = InlineKeyboardState.Uninitialized;

        private bool _isBackground = false;

        private AppletSession _normalSession;
        private AppletSession _interactiveSession;

        // Configuration for foreground mode.
        private SoftwareKeyboardConfig _keyboardForegroundConfig;

        // Configuration for background (inline) mode.
        private SoftwareKeyboardInitialize   _keyboardBackgroundInitialize;
        private SoftwareKeyboardCalc         _keyboardBackgroundCalc;
        private SoftwareKeyboardCustomizeDic _keyboardBackgroundDic;
        private SoftwareKeyboardDictSet      _keyboardBackgroundDictSet;
        private SoftwareKeyboardUserWord[]   _keyboardBackgroundUserWords;

        private byte[] _transferMemory;

        private string   _textValue = "";
        private bool     _okPressed = false;
        private Encoding _encoding  = Encoding.Unicode;

        private IDynamicTextInputHandler _dynamicTextInputHandler = null;

        private SoftwareKeyboardRenderer _keyboardRenderer = null;

        public event EventHandler AppletStateChanged;

        public SoftwareKeyboardApplet(Horizon system)
        {
            _device = system.Device;
        }

        public ResultCode Start(AppletSession normalSession,
                                AppletSession interactiveSession)
        {
            _normalSession      = normalSession;
            _interactiveSession = interactiveSession;

            _interactiveSession.DataAvailable += OnInteractiveData;

            var launchParams   = _normalSession.Pop();
            var keyboardConfig = _normalSession.Pop();

            if (keyboardConfig.Length == Marshal.SizeOf<SoftwareKeyboardInitialize>())
            {
                // Initialize the keyboard applet in background mode.

                _isBackground = true;

                _keyboardBackgroundInitialize = ReadStruct<SoftwareKeyboardInitialize>(keyboardConfig);
                InlineKeyboardState state = InlineKeyboardState.Uninitialized;
                SetInlineState(state);

                string acceptKeyName;
                string cancelKeyName;

                if (_device.UiHandler != null)
                {
                    _dynamicTextInputHandler = _device.UiHandler.CreateDynamicTextInputHandler();
                    _dynamicTextInputHandler.TextChanged += DynamicTextChanged;

                    acceptKeyName = _dynamicTextInputHandler.AcceptKeyName;
                    cancelKeyName = _dynamicTextInputHandler.CancelKeyName;
                }
                else
                {
                    Logger.Error?.Print(LogClass.ServiceAm, "GUI Handler is not set, software keyboard applet will not work properly");

                    acceptKeyName = "";
                    cancelKeyName = "";
                }

                _keyboardRenderer = new SoftwareKeyboardRenderer(acceptKeyName, cancelKeyName);

                _interactiveSession.Push(InlineResponses.FinishedInitialize(state));

                return ResultCode.Success;
            }
            else
            {
                // Initialize the keyboard applet in foreground mode.

                _isBackground = false;

                if (keyboardConfig.Length < Marshal.SizeOf<SoftwareKeyboardConfig>())
                {
                    Logger.Error?.Print(LogClass.ServiceAm, $"SoftwareKeyboardConfig size mismatch. Expected {Marshal.SizeOf<SoftwareKeyboardConfig>():x}. Got {keyboardConfig.Length:x}");
                }
                else
                {
                    _keyboardForegroundConfig = ReadStruct<SoftwareKeyboardConfig>(keyboardConfig);
                }

                if (!_normalSession.TryPop(out _transferMemory))
                {
                    Logger.Error?.Print(LogClass.ServiceAm, "SwKbd Transfer Memory is null");
                }

                if (_keyboardForegroundConfig.UseUtf8)
                {
                    _encoding = Encoding.UTF8;
                }

                _foregroundState = SoftwareKeyboardState.Ready;

                ExecuteForegroundKeyboard();

                return ResultCode.Success;
            }
        }

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        private InlineKeyboardState GetInlineState()
        {
            return _backgroundState;
        }

        private void SetInlineState(InlineKeyboardState state)
        {
            _backgroundState = state;
        }

        public Span<byte> GetGraphicsA8B8G8R8(int width, int height, int pitch, int size)
        {
            return _keyboardRenderer.GetGraphicsA8B8G8R8(width, height, pitch, size);
        }

        private void ExecuteForegroundKeyboard()
        {
            string initialText = null;

            // Initial Text is always encoded as a UTF-16 string in the work buffer (passed as transfer memory)
            // InitialStringOffset points to the memory offset and InitialStringLength is the number of UTF-16 characters
            if (_transferMemory != null && _keyboardForegroundConfig.InitialStringLength > 0)
            {
                initialText = Encoding.Unicode.GetString(_transferMemory, _keyboardForegroundConfig.InitialStringOffset,
                    2 * _keyboardForegroundConfig.InitialStringLength);
            }

            // If the max string length is 0, we set it to a large default
            // length.
            if (_keyboardForegroundConfig.StringLengthMax == 0)
            {
                _keyboardForegroundConfig.StringLengthMax = 100;
            }

            var args = new SoftwareKeyboardUiArgs
            {
                HeaderText = _keyboardForegroundConfig.HeaderText,
                SubtitleText = _keyboardForegroundConfig.SubtitleText,
                GuideText = _keyboardForegroundConfig.GuideText,
                SubmitText = (!string.IsNullOrWhiteSpace(_keyboardForegroundConfig.SubmitText) ?
                    _keyboardForegroundConfig.SubmitText : "OK"),
                StringLengthMin = _keyboardForegroundConfig.StringLengthMin,
                StringLengthMax = _keyboardForegroundConfig.StringLengthMax,
                InitialText = initialText
            };

            // Call the configured GUI handler to get user's input
            if (_device.UiHandler == null)
            {
                Logger.Warning?.Print(LogClass.Application, "GUI Handler is not set. Falling back to default");
                _okPressed = true;
            }
            else
            {
                _okPressed = _device.UiHandler.DisplayInputDialog(args, out _textValue);
            }

            _textValue ??= initialText ?? DefaultText;

            // If the game requests a string with a minimum length less
            // than our default text, repeat our default text until we meet
            // the minimum length requirement.
            // This should always be done before the text truncation step.
            while (_textValue.Length < _keyboardForegroundConfig.StringLengthMin)
            {
                _textValue = String.Join(" ", _textValue, _textValue);
            }

            // If our default text is longer than the allowed length,
            // we truncate it.
            if (_textValue.Length > _keyboardForegroundConfig.StringLengthMax)
            {
                _textValue = _textValue.Substring(0, (int)_keyboardForegroundConfig.StringLengthMax);
            }

            // Does the application want to validate the text itself?
            if (_keyboardForegroundConfig.CheckText)
            {
                // The application needs to validate the response, so we
                // submit it to the interactive output buffer, and poll it
                // for validation. Once validated, the application will submit
                // back a validation status, which is handled in OnInteractiveDataPushIn.
                _foregroundState = SoftwareKeyboardState.ValidationPending;

                _interactiveSession.Push(BuildResponse(_textValue, true));
            }
            else
            {
                // If the application doesn't need to validate the response,
                // we push the data to the non-interactive output buffer
                // and poll it for completion.
                _foregroundState = SoftwareKeyboardState.Complete;

                _normalSession.Push(BuildResponse(_textValue, false));

                AppletStateChanged?.Invoke(this, null);
            }
        }

        private void OnInteractiveData(object sender, EventArgs e)
        {
            // Obtain the validation status response.
            var data = _interactiveSession.Pop();

            if (_isBackground)
            {
                OnBackgroundInteractiveData(data);
            }
            else
            {
                OnForegroundInteractiveData(data);
            }
        }

        private void OnForegroundInteractiveData(byte[] data)
        {
            if (_foregroundState == SoftwareKeyboardState.ValidationPending)
            {
                // TODO(jduncantor):
                // If application rejects our "attempt", submit another attempt,
                // and put the applet back in PendingValidation state.

                // For now we assume success, so we push the final result
                // to the standard output buffer and carry on our merry way.
                _normalSession.Push(BuildResponse(_textValue, false));

                AppletStateChanged?.Invoke(this, null);

                _foregroundState = SoftwareKeyboardState.Complete;
            }
            else if(_foregroundState == SoftwareKeyboardState.Complete)
            {
                // If we have already completed, we push the result text
                // back on the output buffer and poll the application.
                _normalSession.Push(BuildResponse(_textValue, false));

                AppletStateChanged?.Invoke(this, null);
            }
            else
            {
                // We shouldn't be able to get here through standard swkbd execution.
                throw new InvalidOperationException("Software Keyboard is in an invalid state.");
            }
        }

        private void OnBackgroundInteractiveData(byte[] data)
        {
            // WARNING: Only invoke applet state changes after an explicit finalization
            // request from the game, this is because the inline keyboard is expected to
            // keep running in the background sending data by itself.

            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                InlineKeyboardRequest request = (InlineKeyboardRequest)reader.ReadUInt32();
                InlineKeyboardState state = GetInlineState();
                long remaining;

                Logger.Debug?.Print(LogClass.ServiceAm, $"Keyboard received command {request} in state {state}");

                switch (request)
                {
                    case InlineKeyboardRequest.UseChangedStringV2:
                        Logger.Stub?.Print(LogClass.ServiceAm, "Keyboard response ChangedStringV2");
                        break;
                    case InlineKeyboardRequest.UseMovedCursorV2:
                        Logger.Stub?.Print(LogClass.ServiceAm, "Keyboard response MovedCursorV2");
                        break;
                    case InlineKeyboardRequest.SetUserWordInfo:
                        // Read the user word info data.
                        remaining = stream.Length - stream.Position;
                        if (remaining < sizeof(int))
                        {
                            Logger.Warning?.Print(LogClass.ServiceAm, $"Received invalid Software Keyboard User Word Info of {remaining} bytes");
                        }
                        else
                        {
                            int wordsCount = reader.ReadInt32();
                            int wordSize = Marshal.SizeOf<SoftwareKeyboardUserWord>();
                            remaining = stream.Length - stream.Position;

                            if (wordsCount > MaxUserWords)
                            {
                                Logger.Warning?.Print(LogClass.ServiceAm, $"Received {wordsCount} User Words but the maximum is {MaxUserWords}");
                            }
                            else if (wordsCount * wordSize != remaining)
                            {
                                Logger.Warning?.Print(LogClass.ServiceAm, $"Received invalid Software Keyboard User Word Info data of {remaining} bytes for {wordsCount} words");
                            }
                            else
                            {
                                _keyboardBackgroundUserWords = new SoftwareKeyboardUserWord[wordsCount];

                                for (int word = 0; word < wordsCount; word++)
                                {
                                    byte[] wordData = reader.ReadBytes(wordSize);
                                    _keyboardBackgroundUserWords[word] = ReadStruct<SoftwareKeyboardUserWord>(wordData);
                                }
                            }
                        }
                        _interactiveSession.Push(InlineResponses.ReleasedUserWordInfo(state));
                        break;
                    case InlineKeyboardRequest.SetCustomizeDic:
                        // Read the custom dic data.
                        remaining = stream.Length - stream.Position;
                        if (remaining != Marshal.SizeOf<SoftwareKeyboardCustomizeDic>())
                        {
                            Logger.Warning?.Print(LogClass.ServiceAm, $"Received invalid Software Keyboard Customize Dic of {remaining} bytes");
                        }
                        else
                        {
                            var keyboardDicData = reader.ReadBytes((int)remaining);
                            _keyboardBackgroundDic = ReadStruct<SoftwareKeyboardCustomizeDic>(keyboardDicData);
                        }
                        break;
                    case InlineKeyboardRequest.SetCustomizedDictionaries:
                        // Read the custom dictionaries data.
                        remaining = stream.Length - stream.Position;
                        if (remaining != Marshal.SizeOf<SoftwareKeyboardDictSet>())
                        {
                            Logger.Warning?.Print(LogClass.ServiceAm, $"Received invalid Software Keyboard DictSet of {remaining} bytes");
                        }
                        else
                        {
                            var keyboardDictData = reader.ReadBytes((int)remaining);
                            _keyboardBackgroundDictSet = ReadStruct<SoftwareKeyboardDictSet>(keyboardDictData);
                        }
                        break;
                    case InlineKeyboardRequest.Calc:
                        // The Calc request tells the Applet to enter the main input handling loop, which will end
                        // with either a text being submitted or a cancel request from the user.

                        // Read the Calc data.
                        SoftwareKeyboardCalc newCalc;
                        remaining = stream.Length - stream.Position;
                        if (remaining != Marshal.SizeOf<SoftwareKeyboardCalc>())
                        {
                            Logger.Error?.Print(LogClass.ServiceAm, $"Received invalid Software Keyboard Calc of {remaining} bytes");
                            newCalc = new SoftwareKeyboardCalc();
                        }
                        else
                        {
                            var keyboardCalcData = reader.ReadBytes((int)remaining);
                            newCalc = ReadStruct<SoftwareKeyboardCalc>(keyboardCalcData);
                        }

                        // Make the state transition.
                        if (state < InlineKeyboardState.Ready)
                        {
                            // This field consistently is -1 when the calc is not meant to be shown.
                            if (newCalc.Appear.Padding1 == -1)
                            {
                                state = InlineKeyboardState.Initialized;

                                Logger.Debug?.Print(LogClass.ServiceAm, $"Calc during state {state} is probably a dummy");
                            }
                            else
                            {
                                // Set the new calc
                                _keyboardBackgroundCalc = newCalc;

                                // Check if the application expects UTF8 encoding instead of UTF16.
                                if (_keyboardBackgroundCalc.UseUtf8)
                                {
                                    _encoding = Encoding.UTF8;
                                }

                                string newText = _keyboardBackgroundCalc.InputText;
                                uint cursorPosition = (uint)_keyboardBackgroundCalc.CursorPos;
                                _dynamicTextInputHandler?.SetText(newText);

                                state = InlineKeyboardState.Ready;
                                PushChangedString(newText, cursorPosition, state);
                            }

                            SetInlineState(state);
                        }
                        else if (state == InlineKeyboardState.Complete)
                        {
                            state = InlineKeyboardState.Initialized;
                        }

                        // Send the response to the Calc
                        _interactiveSession.Push(InlineResponses.Default(state));
                        break;
                    case InlineKeyboardRequest.Finalize:
                        // Destroy the dynamic text input handler
                        if (_dynamicTextInputHandler != null)
                        {
                            _dynamicTextInputHandler.TextChanged -= DynamicTextChanged;
                            _dynamicTextInputHandler.Dispose();
                        }
                        // The calling application wants to close the keyboard applet and will wait for a state change.
                        SetInlineState(InlineKeyboardState.Uninitialized);
                        AppletStateChanged?.Invoke(this, null);
                        break;
                    default:
                        // We shouldn't be able to get here through standard swkbd execution.
                        Logger.Warning?.Print(LogClass.ServiceAm, $"Invalid Software Keyboard request {request} during state {state}");
                        _interactiveSession.Push(InlineResponses.Default(state));
                        break;
                }
            }
        }

        private void DynamicTextChanged(string text, int cursorBegin, int cursorEnd, bool isAccept, bool isCancel, bool force)
        {
            // Launch as a task to avoid blocking the UI
            Task.Run(() =>
            {
                if (force)
                {
                    Logger.Warning?.Print(LogClass.ServiceAm, "Forcing keyboard out of soft-lock...");

                    // Repeat the response sequence from a Calc to try to exit a soft-lock.

                    text = DefaultText;

                    PushChangedString(text, 0, InlineKeyboardState.Ready);

                    _interactiveSession.Push(InlineResponses.Default(InlineKeyboardState.Ready));

                    Thread.Sleep(SoftUnlockerDelayMilliseconds);
                }

                InlineKeyboardState state = GetInlineState();
                if (!force && (state < InlineKeyboardState.Ready || state == InlineKeyboardState.Complete))
                {
                    return;
                }

                if (isAccept == false && isCancel == false)
                {
                    Logger.Debug?.Print(LogClass.ServiceAm, $"Updating keyboard text to {text} and cursor position to {cursorBegin}");

                    state = InlineKeyboardState.Complete;
                    PushChangedString(text, (uint)cursorBegin, state);
                }
                else
                {
                    // The 'Complete' state indicates the Calc request has been fulfilled by the applet,
                    // but do not change the state of the entire applet, only the responses.
                    state = InlineKeyboardState.Complete;

                    if (isAccept)
                    {
                        Logger.Debug?.Print(LogClass.ServiceAm, $"Sending keyboard OK with text {text}");

                        DecidedEnter(text, state);
                    }
                    else if (isCancel)
                    {
                        Logger.Debug?.Print(LogClass.ServiceAm, "Sending keyboard Cancel");

                        DecidedCancel(state);
                    }

                    _interactiveSession.Push(InlineResponses.Default(state));

                    Logger.Debug?.Print(LogClass.ServiceAm, $"Resetting state of the keyboard to {state}");

                    // Set the state of the applet to 'Initialized' as it is the only known state so far
                    // that does not soft-lock the keyboard after use.
                    state = InlineKeyboardState.Initialized;

                    _interactiveSession.Push(InlineResponses.Default(state));

                    SetInlineState(state);
                }
            });
        }

        private void PushChangedString(string text, uint cursor, InlineKeyboardState state)
        {
            // TODO (Caian): The *V2 methods are not supported because the applications that request
            // them do not seem to accept them. The regular methods seem to work just fine in all cases.

            if (_encoding == Encoding.UTF8)
            {
                _interactiveSession.Push(InlineResponses.ChangedStringUtf8(text, cursor, state));
            }
            else
            {
                _interactiveSession.Push(InlineResponses.ChangedString(text, cursor, state));
            }
        }

        private void DecidedEnter(string text, InlineKeyboardState state)
        {
            if (_encoding == Encoding.UTF8)
            {
                _interactiveSession.Push(InlineResponses.DecidedEnterUtf8(text, state));
            }
            else
            {
                _interactiveSession.Push(InlineResponses.DecidedEnter(text, state));
            }
        }

        private void DecidedCancel(InlineKeyboardState state)
        {
            _interactiveSession.Push(InlineResponses.DecidedCancel(state));
        }

        private byte[] BuildResponse(string text, bool interactive)
        {
            int bufferSize = interactive ? InteractiveBufferSize : StandardBufferSize;

            using (MemoryStream stream = new MemoryStream(new byte[bufferSize]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                byte[] output = _encoding.GetBytes(text);

                if (!interactive)
                {
                    // Result Code
                    writer.Write(_okPressed ? 0U : 1U);
                }
                else
                {
                    // In interactive mode, we write the length of the text as a long, rather than
                    // a result code. This field is inclusive of the 64-bit size.
                    writer.Write((long)output.Length + 8);
                }

                writer.Write(output);

                return stream.ToArray();
            }
        }

        private static T ReadStruct<T>(byte[] data)
            where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
