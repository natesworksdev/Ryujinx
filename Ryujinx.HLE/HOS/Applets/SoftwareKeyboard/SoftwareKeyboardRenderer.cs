using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Class that generates the graphics for the software keyboard applet during inline mode.
    /// </summary>
    internal class SoftwareKeyboardRenderer
    {
        private int    _graphicsWidth  = 0;
        private int    _graphicsHeight = 0;
        private int    _graphicsPitch  = 0;
        private int    _graphicsSize   = 0;
        private byte[] _graphicsData   = null;

        private string _line1Text;
        private string _line2Text;
        private string _line3Text;

        public SoftwareKeyboardRenderer(string acceptKeyName, string cancelKeyName)
        {
            _line1Text = "Please use the keyboard to input text...";
            _line2Text = $"Press {acceptKeyName} to accept or {cancelKeyName} to cancel.";
            _line3Text = "Hold any of the keys to exit soft-lock.";
        }

        private void RedrawGraphicsA8B8G8R8(int width, int height, int pitch, int size)
        {
            // Use the whole area of the image to draw, even the alignment, otherwise it will shear the final image.
            int availableWidth = pitch / 4;
            int availableHeight = size / pitch;

            Debug.Assert(width <= availableWidth);
            Debug.Assert(height <= availableHeight);
            Debug.Assert(pitch * height <= size);

            _graphicsWidth = width;
            _graphicsHeight = height;
            _graphicsPitch = pitch;
            _graphicsSize = size;

            // WARNING: The color format is wrong because the system uses ABGR instead of ARGB, but
            // this is not a problem for black and white drawings.
            Bitmap bitmap = new Bitmap(availableWidth, availableHeight, PixelFormat.Format32bppArgb);

            using (var gfx = System.Drawing.Graphics.FromImage(bitmap))
            {
                gfx.Clear(Color.Transparent);
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // The keyboard layer does not cover the entire visible area.
                const float keyboardHeightPercent = 0.625f;
                int keyboardHeight = (int)(keyboardHeightPercent * height);
                int keyboardTop = height - keyboardHeight;
                int keyboardWidth = width;
                int keyboardLeft = 0;

                RectangleF keyboardRectangle = new RectangleF(keyboardLeft, keyboardTop, keyboardWidth, keyboardHeight);

                // Fill the keyboard area with a transparent black color.
                SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                gfx.FillRectangle(backgroundBrush, keyboardRectangle);

                // Draw the first line larger then the second and third.
                System.Drawing.Font line1Font = new System.Drawing.Font("Tahoma", 40);
                System.Drawing.Font line2Font = new System.Drawing.Font("Tahoma", 20);
                System.Drawing.Font line3Font = line2Font;

                // Draw the first line in the center of the rectangle area, the second just below the first 
                // and the third below the second.
                RectangleF line1Rectangle = keyboardRectangle;
                RectangleF line2Rectangle = line1Rectangle;
                RectangleF line3Rectangle = line1Rectangle;
                line2Rectangle.Y += 45;
                line3Rectangle.Y += 90;

                StringFormat stringFormat = new StringFormat(StringFormatFlags.NoClip);
                stringFormat.LineAlignment = StringAlignment.Center;
                stringFormat.Alignment = StringAlignment.Center;

                gfx.DrawString(_line1Text, line1Font, Brushes.White, line1Rectangle, stringFormat);
                gfx.DrawString(_line2Text, line2Font, Brushes.White, line2Rectangle, stringFormat);
                gfx.DrawString(_line3Text, line3Font, Brushes.White, line3Rectangle, stringFormat);
            }

            Rectangle lockRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(lockRectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            Debug.Assert(data.Stride == pitch);

            byte[] newGraphicsData = new byte[size];
            Marshal.Copy(data.Scan0, newGraphicsData, 0, size);
            Volatile.Write(ref _graphicsData, newGraphicsData);
        }

        public Span<byte> GetGraphicsA8B8G8R8(int width, int height, int pitch, int size)
        {
            if (_graphicsWidth != width || _graphicsHeight != height ||
                _graphicsPitch != pitch || _graphicsSize != size)
            {
                RedrawGraphicsA8B8G8R8(width, height, pitch, size);
            }

            return _graphicsData;
        }
    }
}
