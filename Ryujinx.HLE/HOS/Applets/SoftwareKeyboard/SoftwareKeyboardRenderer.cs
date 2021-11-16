using Ryujinx.HLE.Ui;
using Ryujinx.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp.PixelFormats;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Class that generates the graphics for the software keyboard applet during inline mode.
    /// </summary>
    internal class SoftwareKeyboardRenderer : IDisposable
    {
        const int TextBoxBlinkThreshold            = 8;
        const int TextBoxBlinkSleepMilliseconds    = 100;

        const string MessageText          = "Please use the keyboard to input text";
        const string AcceptText           = "Accept";
        const string CancelText           = "Cancel";
        const string ControllerToggleText = "Toggle input";

        private RenderingSurfaceInfo _surfaceInfo;
        private Image<Argb32>        _surface    = null;
        private object               _renderLock = new object();

        private string _inputText         = "";
        private int    _cursorStart       = 0;
        private int    _cursorEnd         = 0;
        private bool   _acceptPressed     = false;
        private bool   _cancelPressed     = false;
        private bool   _overwriteMode     = false;
        private bool   _typingEnabled     = true;
        private bool   _controllerEnabled = true;

        private Image _ryujinxLogo   = null;
        private Image _padAcceptIcon = null;
        private Image _padCancelIcon = null;
        private Image _keyModeIcon   = null;

        private float _textBoxOutlineWidth;
        private float _padPressedPenWidth;

        private Color _textNormalColor;
        private Color _textSelectedColor;
        private Color _textOverCursorColor;

        private IBrush _panelBrush;
        private IBrush _disabledBrush;
        private IBrush _cursorBrush;
        private IBrush _selectionBoxBrush;

        private Pen _textBoxOutlinePen;
        private Pen _cursorPen;
        private Pen _selectionBoxPen;
        private Pen _padPressedPen;

        private int  _inputTextFontSize;
        private Font _messageFont;
        private Font _inputTextFont;
        private Font _labelsTextFont;

        private RectangleF _panelRectangle;
        private Point      _logoPosition;
        private float      _messagePositionY;

        private TRef<int>   _textBoxBlinkCounter     = new TRef<int>(0);
        private TimedAction _textBoxBlinkTimedAction = new TimedAction();

        public SoftwareKeyboardRenderer(IHostUiTheme uiTheme)
        {
            _surfaceInfo = new RenderingSurfaceInfo(0, 0, 0, 0, 0);

            string ryujinxLogoPath = "Ryujinx.Ui.Resources.Logo_Ryujinx.png";
            int    ryujinxLogoSize = 32;

            _ryujinxLogo = LoadResource(Assembly.GetEntryAssembly(), ryujinxLogoPath, ryujinxLogoSize, ryujinxLogoSize);

            string padAcceptIconPath = "Ryujinx.HLE.HOS.Applets.SoftwareKeyboard.Resources.Icon_BtnA.png";
            string padCancelIconPath = "Ryujinx.HLE.HOS.Applets.SoftwareKeyboard.Resources.Icon_BtnB.png";
            string keyModeIconPath   = "Ryujinx.HLE.HOS.Applets.SoftwareKeyboard.Resources.Icon_KeyF6.png";

            _padAcceptIcon = LoadResource(Assembly.GetExecutingAssembly(), padAcceptIconPath  , 0, 0);
            _padCancelIcon = LoadResource(Assembly.GetExecutingAssembly(), padCancelIconPath  , 0, 0);
            _keyModeIcon   = LoadResource(Assembly.GetExecutingAssembly(), keyModeIconPath    , 0, 0);

            Color panelColor               = ToColor(uiTheme.DefaultBackgroundColor, 255);
            Color panelTransparentColor    = ToColor(uiTheme.DefaultBackgroundColor, 150);
            Color borderColor              = ToColor(uiTheme.DefaultBorderColor);
            Color selectionBackgroundColor = ToColor(uiTheme.SelectionBackgroundColor);

            _textNormalColor     = ToColor(uiTheme.DefaultForegroundColor);
            _textSelectedColor   = ToColor(uiTheme.SelectionForegroundColor);
            _textOverCursorColor = ToColor(uiTheme.DefaultForegroundColor, null, true);

            float cursorWidth = 2;

            _textBoxOutlineWidth = 2;
            _padPressedPenWidth  = 2;

            _panelBrush        = new SolidBrush(panelColor);
            _disabledBrush     = new SolidBrush(panelTransparentColor);
            _cursorBrush       = new SolidBrush(_textNormalColor);
            _selectionBoxBrush = new SolidBrush(selectionBackgroundColor);

            _textBoxOutlinePen = new Pen(borderColor, _textBoxOutlineWidth);
            _cursorPen         = new Pen(_textNormalColor, cursorWidth);
            _selectionBoxPen   = new Pen(selectionBackgroundColor, cursorWidth);
            _padPressedPen     = new Pen(borderColor, _padPressedPenWidth);

            _inputTextFontSize = 20;

            string font = uiTheme.FontFamily;

            _messageFont    = SystemFonts.CreateFont(font, 26,                 FontStyle.Regular);
            _inputTextFont  = SystemFonts.CreateFont(font, _inputTextFontSize, FontStyle.Regular);
            _labelsTextFont = SystemFonts.CreateFont(font, 24,                 FontStyle.Regular);

            StartTextBoxBlinker(_textBoxBlinkTimedAction, _textBoxBlinkCounter);
        }

        private static void StartTextBoxBlinker(TimedAction timedAction, TRef<int> blinkerCounter)
        {
            timedAction.Reset(() =>
            {
                // The blinker is on half of the time and events such as input
                // changes can reset the blinker.
                var value = Volatile.Read(ref blinkerCounter.Value);
                value = (value + 1) % (2 * TextBoxBlinkThreshold);
                Volatile.Write(ref blinkerCounter.Value, value);
            }, TextBoxBlinkSleepMilliseconds);
        }

        private Color ToColor(ThemeColor color, byte? overrideAlpha = null, bool flipRgb = false)
        {
            var a = (byte)(color.A * 255);
            var r = (byte)(color.R * 255);
            var g = (byte)(color.G * 255);
            var b = (byte)(color.B * 255);

            if (flipRgb)
            {
                r = (byte)(255 - r);
                g = (byte)(255 - g);
                b = (byte)(255 - b);
            }

            return Color.FromRgba(r, g, b, overrideAlpha.GetValueOrDefault(a));
        }

        private Image LoadResource(Assembly assembly, string resourcePath, int newWidth, int newHeight)
        {
            Stream resourceStream = assembly.GetManifestResourceStream(resourcePath);

            Debug.Assert(resourceStream != null);

            var image = Image.Load(resourceStream);

            if (newHeight != 0 && newWidth != 0)
            {
                image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
            }

            return image;
        }

#pragma warning disable CS8632
        public void UpdateTextState(string? inputText, int? cursorStart, int? cursorEnd, bool? overwriteMode, bool? typingEnabled)
#pragma warning restore CS8632
        {
            lock (_renderLock)
            {
                // Update the parameters that were provided.
                _inputText     = inputText != null ? inputText : _inputText;
                _cursorStart   = cursorStart.GetValueOrDefault(_cursorStart);
                _cursorEnd     = cursorEnd.GetValueOrDefault(_cursorEnd);
                _overwriteMode = overwriteMode.GetValueOrDefault(_overwriteMode);
                _typingEnabled = typingEnabled.GetValueOrDefault(_typingEnabled);

                // Reset the cursor blink.
                Volatile.Write(ref _textBoxBlinkCounter.Value, 0);
            }
        }

        public void UpdateCommandState(bool? acceptPressed, bool? cancelPressed, bool? controllerEnabled)
        {
            lock (_renderLock)
            {
                // Update the parameters that were provided.
                _acceptPressed     = acceptPressed.GetValueOrDefault(_acceptPressed);
                _cancelPressed     = cancelPressed.GetValueOrDefault(_cancelPressed);
                _controllerEnabled = controllerEnabled.GetValueOrDefault(_controllerEnabled);
            }
        }

        private void Redraw()
        {
            if (_surface == null)
            {
                return;
            }

            _surface.Mutate(context =>
            {
                var    messageRectangle = MeasureString(MessageText, _messageFont);
                float  messagePositionX = (_panelRectangle.Width - messageRectangle.Width) / 2 - messageRectangle.X;
                float  messagePositionY = _messagePositionY - messageRectangle.Y;
                PointF messagePosition  = new PointF(messagePositionX, messagePositionY);

                context.GetGraphicsOptions().Antialias = true;
                context.GetShapeGraphicsOptions().GraphicsOptions.Antialias = true;

                context.Clear(Color.Transparent);
                context.Fill(_panelBrush, _panelRectangle);
                context.DrawImage(_ryujinxLogo, _logoPosition, 1);
                context.DrawText(MessageText, _messageFont, _textNormalColor, messagePosition);

                if (!_typingEnabled)
                {
                    // Just draw a semi-transparent rectangle on top to fade the component with the background.
                    // TODO (caian): This will not work if one decides to add make background semi-transparent as well.

                    RectangleF disabledRectangle = new RectangleF(messagePositionX, messagePositionY, messageRectangle.Width, messageRectangle.Height);

                    context.Fill(_disabledBrush, disabledRectangle);
                }

                DrawTextBox(context);

                float halfWidth = _panelRectangle.Width / 2;
                float buttonsY  = _panelRectangle.Y + 185;

                PointF acceptButtonPosition  = new PointF(halfWidth - 180, buttonsY);
                PointF cancelButtonPosition  = new PointF(halfWidth      , buttonsY);
                PointF disableButtonPosition = new PointF(halfWidth + 180, buttonsY);

                DrawPadButton(context, acceptButtonPosition, _padAcceptIcon, AcceptText, _acceptPressed, _controllerEnabled);
                DrawPadButton(context, cancelButtonPosition, _padCancelIcon, CancelText, _cancelPressed, _controllerEnabled);
                DrawControllerToggle(context, disableButtonPosition, _controllerEnabled);
            });
        }

        private void RecreateSurface()
        {
            Debug.Assert(_surfaceInfo.ColorFormat == Services.SurfaceFlinger.ColorFormat.A8B8G8R8);

            // Use the whole area of the image to draw, even the alignment, otherwise it may shear the final
            // image if the pitch is different.
            uint totalWidth  = _surfaceInfo.Pitch / 4;
            uint totalHeight = _surfaceInfo.Size / _surfaceInfo.Pitch;

            Debug.Assert(_surfaceInfo.Width <= totalWidth);
            Debug.Assert(_surfaceInfo.Height <= totalHeight);
            Debug.Assert(_surfaceInfo.Pitch * _surfaceInfo.Height <= _surfaceInfo.Size);

            _surface = new Image<Argb32>((int)totalWidth, (int)totalHeight);
        }

        private void RecomputeConstants()
        {
            int totalWidth  = (int)_surfaceInfo.Width;
            int totalHeight = (int)_surfaceInfo.Height;

            int panelHeight    = 240;
            int panelPositionY = totalHeight - panelHeight;

            _panelRectangle = new RectangleF(0, panelPositionY, totalWidth, panelHeight);

            _messagePositionY = panelPositionY + 60;

            int logoPositionX = (totalWidth - _ryujinxLogo.Width) / 2;
            int logoPositionY = panelPositionY + 18;

            _logoPosition = new Point(logoPositionX, logoPositionY);
        }

        private RectangleF MeasureString(string text, Font font)
        {
            RendererOptions options = new RendererOptions(font);
            FontRectangle rectangle = TextMeasurer.Measure(text == "" ? " " : text, options);

            if (text == "")
            {
                return new RectangleF(0, rectangle.Y, 0, rectangle.Height);
            }
            else
            {
                return new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            }
        }

        private void DrawTextBox(IImageProcessingContext context)
        {
            var inputTextRectangle = MeasureString(_inputText, _inputTextFont);

            float boxWidth  = (int)(Math.Max(300, inputTextRectangle.Width + inputTextRectangle.X + 8));
            float boxHeight = 32;
            float boxY      = _panelRectangle.Y + 110;
            float boxX      = (int)((_panelRectangle.Width - boxWidth) / 2);

            RectangleF boxRectangle = new RectangleF(boxX, boxY, boxWidth, boxHeight);

            context.Draw(_textBoxOutlinePen, boxRectangle);

            float inputTextX = (_panelRectangle.Width - inputTextRectangle.Width) / 2 - inputTextRectangle.X;
            float inputTextY = boxY + 5;

            var inputTextPosition = new PointF(inputTextX, inputTextY);

            context.DrawText(_inputText, _inputTextFont, _textNormalColor, inputTextPosition);

            // Draw the cursor on top of the text and redraw the text with a different color if necessary.

            Color  cursorTextColor;
            IBrush cursorBrush;
            Pen    cursorPen;

            float cursorPositionYTop    = inputTextY + 1;
            float cursorPositionYBottom = cursorPositionYTop + _inputTextFontSize + 1;
            float cursorPositionXLeft;
            float cursorPositionXRight;

            bool cursorVisible = false;

            if (_cursorStart != _cursorEnd)
            {
                Debug.Assert(_inputText.Length > 0);

                cursorTextColor = _textSelectedColor;
                cursorBrush     = _selectionBoxBrush;
                cursorPen       = _selectionBoxPen;

                string textUntilBegin = _inputText.Substring(0, _cursorStart);
                string textUntilEnd   = _inputText.Substring(0, _cursorEnd);

                var selectionBeginRectangle = MeasureString(textUntilBegin, _inputTextFont);
                var selectionEndRectangle   = MeasureString(textUntilEnd  , _inputTextFont);

                cursorVisible         = true;
                cursorPositionXLeft   = inputTextX + selectionBeginRectangle.Width + selectionBeginRectangle.X;
                cursorPositionXRight  = inputTextX + selectionEndRectangle.Width   + selectionEndRectangle.X;
            }
            else
            {
                cursorTextColor = _textOverCursorColor;
                cursorBrush     = _cursorBrush;
                cursorPen       = _cursorPen;

                if (Volatile.Read(ref _textBoxBlinkCounter.Value) < TextBoxBlinkThreshold)
                {
                    // Show the blinking cursor.

                    int    cursorStart         = Math.Min(_inputText.Length, _cursorStart);
                    string textUntilCursor     = _inputText.Substring(0, cursorStart);
                    var    cursorTextRectangle = MeasureString(textUntilCursor, _inputTextFont);

                    cursorVisible       = true;
                    cursorPositionXLeft = inputTextX + cursorTextRectangle.Width + cursorTextRectangle.X;

                    if (_overwriteMode)
                    {
                        // The blinking cursor is in overwrite mode so it takes the size of a character.

                        if (_cursorStart < _inputText.Length)
                        {
                            textUntilCursor      = _inputText.Substring(0, cursorStart + 1);
                            cursorTextRectangle  = MeasureString(textUntilCursor, _inputTextFont);
                            cursorPositionXRight = inputTextX + cursorTextRectangle.Width + cursorTextRectangle.X;
                        }
                        else
                        {
                            cursorPositionXRight = cursorPositionXLeft + _inputTextFontSize / 2;
                        }
                    }
                    else
                    {
                        // The blinking cursor is in insert mode so it is only a line.
                        cursorPositionXRight = cursorPositionXLeft;
                    }
                }
                else
                {
                    cursorPositionXLeft  = inputTextX;
                    cursorPositionXRight = inputTextX;
                }
            }

            if (_typingEnabled && cursorVisible)
            {
                float cursorWidth  = cursorPositionXRight  - cursorPositionXLeft;
                float cursorHeight = cursorPositionYBottom - cursorPositionYTop;

                if (cursorWidth == 0)
                {
                    PointF[] points = new PointF[]
                    {
                        new PointF(cursorPositionXLeft, cursorPositionYTop),
                        new PointF(cursorPositionXLeft, cursorPositionYBottom),
                    };

                    context.DrawLines(cursorPen, points);
                }
                else
                {
                    var cursorRectangle = new RectangleF(cursorPositionXLeft, cursorPositionYTop, cursorWidth, cursorHeight);

                    context.Draw(cursorPen  , cursorRectangle);
                    context.Fill(cursorBrush, cursorRectangle);

                    Image<Argb32> textOverCursor = new Image<Argb32>((int)cursorRectangle.Width, (int)cursorRectangle.Height);
                    textOverCursor.Mutate(context =>
                    {
                        var textRelativePosition = new PointF(inputTextPosition.X - cursorRectangle.X, inputTextPosition.Y - cursorRectangle.Y);
                        context.DrawText(_inputText, _inputTextFont, cursorTextColor, textRelativePosition);
                    });

                    var cursorPosition = new Point((int)cursorRectangle.X, (int)cursorRectangle.Y);
                    context.DrawImage(textOverCursor, cursorPosition, 1);
                }
            }
            else if (!_typingEnabled)
            {
                // Just draw a semi-transparent rectangle on top to fade the component with the background.
                // TODO (caian): This will not work if one decides to add make background semi-transparent as well.

                RectangleF disabledRectangle = new RectangleF(boxX - _textBoxOutlineWidth, boxY - _textBoxOutlineWidth,
                    boxWidth + 2 * _textBoxOutlineWidth, boxHeight + 2 * _textBoxOutlineWidth);

                context.Fill(_disabledBrush, disabledRectangle);
            }
        }

        private void DrawPadButton(IImageProcessingContext context, PointF point, Image icon, string label, bool pressed, bool enabled)
        {
            // Use relative positions so we can center the the entire drawing later.

            float iconX      = 0;
            float iconY      = 0;
            float iconWidth  = icon.Width;
            float iconHeight = icon.Height;

            var labelRectangle = MeasureString(label, _labelsTextFont);

            float labelPositionX = iconWidth + 8 - labelRectangle.X;
            float labelPositionY = 3;

            float fullWidth  = labelPositionX + labelRectangle.Width + labelRectangle.X;
            float fullHeight = iconHeight;

            // Convert all relative positions into absolute.

            float originX = (int)(point.X - fullWidth  / 2);
            float originY = (int)(point.Y - fullHeight / 2);

            iconX += originX;
            iconY += originY;

            var iconPosition  = new Point((int)iconX, (int)iconY);
            var labelPosition = new PointF(labelPositionX + originX, labelPositionY + originY);

            context.DrawImage(icon, iconPosition, 1);

            context.DrawText(label, _labelsTextFont, _textNormalColor, labelPosition);

            var frame = new RectangleF(originX - 2 * _padPressedPenWidth, originY - 2 * _padPressedPenWidth,
                fullWidth + 4 * _padPressedPenWidth, fullHeight + 4 * _padPressedPenWidth);

            if (enabled)
            {
                if (pressed)
                {
                    context.Draw(_padPressedPen, frame);
                }
            }
            else
            {
                // Just draw a semi-transparent rectangle on top to fade the component with the background.
                // TODO (caian): This will not work if one decides to add make background semi-transparent as well.
                context.Fill(_disabledBrush, frame);
            }
        }

        private void DrawControllerToggle(IImageProcessingContext context, PointF point, bool enabled)
        {
            var labelRectangle = MeasureString(ControllerToggleText, _labelsTextFont);

            // Use relative positions so we can center the the entire drawing later.

            float keyWidth  = _keyModeIcon.Width;
            float keyHeight = _keyModeIcon.Height;

            float labelPositionX = keyWidth + 8 - labelRectangle.X;
            float labelPositionY = -labelRectangle.Y - 1;

            float keyX = 0;
            float keyY = (int)((labelPositionY + labelRectangle.Height - keyHeight) / 2);

            float fullWidth  = labelPositionX + labelRectangle.Width;
            float fullHeight = Math.Max(labelPositionY + labelRectangle.Height, keyHeight);

            // Convert all relative positions into absolute.

            float originX = (int)(point.X - fullWidth  / 2);
            float originY = (int)(point.Y - fullHeight / 2);

            keyX += originX;
            keyY += originY;

            var labelPosition   = new PointF(labelPositionX + originX, labelPositionY + originY);
            var overlayPosition = new Point((int)keyX, (int)keyY);

            context.DrawImage(_keyModeIcon, overlayPosition, 1);

            context.DrawText(ControllerToggleText, _labelsTextFont, _textNormalColor, labelPosition);
        }

        private bool TryCopyTo(IVirtualMemoryManager destination, ulong position)
        {
            if (_surface == null)
            {
                return false;
            }

            // Convert the pixel format used in the image to the one used in the Switch surface.

            if (!_surface.TryGetSinglePixelSpan(out Span<Argb32> pixels))
            {
                return false;
            }

            byte[]       data        = MemoryMarshal.AsBytes(pixels).ToArray();
            Span<uint>   dataConvert = MemoryMarshal.Cast<byte, uint>(data);

            Debug.Assert(data.Length == _surfaceInfo.Size);

            for (int i = 0; i < dataConvert.Length; i++)
            {
                dataConvert[i] = BitOperations.RotateRight(dataConvert[i], 8);
            }

            try
            {
                destination.Write(position, data);
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal bool DrawTo(RenderingSurfaceInfo surfaceInfo, IVirtualMemoryManager destination, ulong position)
        {
            lock (_renderLock)
            {
                if (!_surfaceInfo.Equals(surfaceInfo))
                {
                    _surfaceInfo = surfaceInfo;
                    RecreateSurface();
                    RecomputeConstants();
                }

                Redraw();

                return TryCopyTo(destination, position);
            }
        }

        public void Dispose()
        {
            _textBoxBlinkTimedAction.RequestCancel();
        }
    }
}
