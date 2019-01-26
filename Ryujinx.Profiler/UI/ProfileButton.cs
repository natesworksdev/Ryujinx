using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Profiler.UI.SharpFontHelpers;

namespace Ryujinx.Profiler.UI
{
    public class ProfileButton
    {
        private int padding;
        private FontService FontService;
        private int X, Y, Right, Top, Height;
        private int textX, textY;
        private string Label;
        private Action Clicked;
        private bool Visible;

        public ProfileButton(FontService fontService, Action clicked)
            : this(fontService, clicked, 0, 0, 0, 0, 0)
        {
            Visible = false;
        }

        public ProfileButton(FontService fontService, Action clicked, int x, int y, int padding, int height, int width)
            : this(fontService, "", clicked, x, y, padding, height, width)
        {
            Visible = false;
        }

        public ProfileButton(FontService fontService, string label, Action clicked, int x, int y, int padding, int height, int width = -1)
        {
            Visible = true;
            FontService = fontService;
            Label = label;
            Clicked = clicked;

            if (width == -1)
            {
                // Dummy draw to measure size
                width = (int)fontService.DrawText(Label, 0, 0, height, false);
            }

            UpdateSize(x, y, padding, width, height);
        }

        public void UpdateSize(int x, int y, int padding, int width, int height)
        {
            Height = height;
            X = x;
            Y = y;
            textX = x + padding / 2;
            textY = y + padding / 2;
            Top = y + height + padding;
            Right = x + width + padding;
        }

        public void Draw()
        {
            if (!Visible)
            {
                return;
            }

            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(Color.Blue);
            GL.Vertex2(X, Y);
            GL.Vertex2(X, Top);
            GL.Vertex2(Right, Top);

            GL.Vertex2(Right, Top);
            GL.Vertex2(Right, Y);
            GL.Vertex2(X, Y);
            GL.End();

            FontService.DrawText(Label, textX, textY, Height);
        }

        public bool ProcessClick(int x, int y)
        {
            if (x > X && x < Right &&
                y > Y && y < Top)
            {
                Clicked();
                return true;
            }

            return false;
        }
    }
}
