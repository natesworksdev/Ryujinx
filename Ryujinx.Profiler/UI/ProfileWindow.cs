using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OpenTK.Input;
using Ryujinx.Profiler;
using Ryujinx.Profiler.UI;
using Ryujinx.Profiler.UI.SharpFontHelpers;

namespace Ryujinx
{
    public class ProfileWindow : GameWindow
    {
        private enum ButtonIndex
        {
            TagTitle = 0,
            InstantTitle = 1,
            AverageTitle = 2,
            TotalTitle = 3,
        }

        private bool visible = true, initComplete = false;
        public bool visibleChanged;
        private FontService fontService;
        private List<KeyValuePair<ProfileConfig, TimingInfo>> profileData;

        private float scrollPos = 0;
        private float minScroll = 0, maxScroll = 0;

        private ProfileButton[] buttons;
        private IComparer<KeyValuePair<ProfileConfig, TimingInfo>> sortAction;

        public ProfileWindow()
            : base(400, 720)
        {
            Location = new Point(DisplayDevice.Default.Width - 400, (DisplayDevice.Default.Height - 720) / 2);
            Title = "Profiler";
            sortAction = null;
        }

        #region Public Methods
        public void ToggleVisible()
        {
            visible = !visible;
            visibleChanged = true;
        }
        #endregion

        #region OnLoad
        /// <summary>
        /// Setup OpenGL and load resources
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color.MidnightBlue);
            fontService = new FontService();
            fontService.InitalizeTextures();

            buttons = new ProfileButton[4];
            buttons[(int)ButtonIndex.TagTitle]     = new ProfileButton(fontService, () => sortAction = new ProfileSorters.TagAscending());
            buttons[(int)ButtonIndex.InstantTitle] = new ProfileButton(fontService, () => sortAction = new ProfileSorters.InstantAscending());
            buttons[(int)ButtonIndex.AverageTitle] = new ProfileButton(fontService, () => sortAction = new ProfileSorters.AverageAscending());
            buttons[(int)ButtonIndex.TotalTitle]   = new ProfileButton(fontService, () => sortAction = new ProfileSorters.TotalAscending());
        }
        #endregion

        #region OnResize
        /// <summary>
        /// Respond to resize events
        /// </summary>
        /// <param name="e">Contains information on the new GameWindow size.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Width, 0, Height, 0.0, 4.0);
        }
        #endregion

        #region OnClose
        /// <summary>
        /// Intercept close event and hide instead
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            visible = false;
            visibleChanged = true;
            e.Cancel = true;
            base.OnClosing(e);
        }
        #endregion

        #region OnUpdateFrame
        /// <summary>
        /// Profile Update Loop
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            initComplete = true;
            profileData = Profile.GetProfilingData().ToList();
            if (sortAction != null)
            {
                profileData.Sort(sortAction);
            }
        }
        #endregion

        #region OnRenderFrame
        /// <summary>
        /// Profile Render Loop
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (visibleChanged)
            {
                Visible = visible;
                visibleChanged = false;
            }

            if (!visible || !initComplete)
            {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.ClearColor(Color.Black);
            fontService.fontColor = Color.White;
            int verticalIndex = 0;
            int lineHeight = 16;
            int titleHeight = 24;
            int titleFontHeight = 16;
            int linePadding = 2;
            int columnSpacing = 30;

            float width;
            float maxWidth = 0;
            float yOffset = scrollPos - titleHeight;
            float xOffset = 10;

            // Background lines to make reading easier
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(0, 0, Width, Height - titleHeight);
            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(0.2f, 0.2f, 0.2f);
            for (int i = 0; i < profileData.Count; i += 2)
            {
                float top = GetLineY(yOffset, lineHeight, linePadding, false, i - 1);
                float bottom = GetLineY(yOffset, lineHeight, linePadding, false, i);
                GL.Vertex2(0, bottom);
                GL.Vertex2(0, top);
                GL.Vertex2(Width, top);

                GL.Vertex2(Width, top);
                GL.Vertex2(Width, bottom);
                GL.Vertex2(0, bottom);
            }
            GL.End();
            maxScroll = (lineHeight + linePadding) * (profileData.Count - 1);

            // Display category
            verticalIndex = 0;
            foreach (var entry in profileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                width = fontService.DrawText(entry.Key.Category, xOffset, y, lineHeight);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            GL.Disable(EnableCap.ScissorTest);

            width = fontService.DrawText("Category", xOffset, Height - titleFontHeight, titleFontHeight);
            if (width > maxWidth)
                maxWidth = width;

            xOffset += maxWidth + columnSpacing;
            

            // Display session group
            maxWidth = 0;
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in profileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                width = fontService.DrawText(entry.Key.SessionGroup, xOffset, y, lineHeight);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            GL.Disable(EnableCap.ScissorTest);

            width = fontService.DrawText("Group", xOffset, Height - titleFontHeight, titleFontHeight);
            if (width > maxWidth)
                maxWidth = width;

            xOffset += maxWidth + columnSpacing;

            // Display session item
            maxWidth = 0;
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in profileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                width = fontService.DrawText(entry.Key.SessionItem, xOffset, y, lineHeight);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            GL.Disable(EnableCap.ScissorTest);

            width = fontService.DrawText("Item", xOffset, Height - titleFontHeight, titleFontHeight);
            if (width > maxWidth)
                maxWidth = width;

            xOffset += maxWidth + columnSpacing;
            buttons[(int)ButtonIndex.TagTitle].UpdateSize(0, Height - titleFontHeight, 0, (int)xOffset, titleFontHeight);

            // Time bars
            width = Width - xOffset - 370;
            int maxInstant = profileData.Max((kvp) => (int)kvp.Value.LastTime);
            int maxAverage = profileData.Max((kvp) => (int)kvp.Value.AverageTime);
            int maxTotal = profileData.Max((kvp) => (int)kvp.Value.TotalTime);
            float barHeight = (lineHeight - linePadding) / 3.0f;
            verticalIndex = 0;

            GL.Enable(EnableCap.ScissorTest);
            GL.Begin(PrimitiveType.Triangles);
            foreach (var entry in profileData)
            {
                // Instant
                GL.Color3(Color.Blue);
                float bottom = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                float top = bottom + barHeight;
                float right = (float)entry.Value.LastTime / maxInstant * width + xOffset;
                GL.Vertex2(xOffset, bottom);
                GL.Vertex2(xOffset, top);
                GL.Vertex2(right, top);

                GL.Vertex2(right, top);
                GL.Vertex2(right, bottom);
                GL.Vertex2(xOffset, bottom);

                // Average
                GL.Color3(Color.Green);
                top += barHeight;
                bottom += barHeight;
                right = (float)entry.Value.AverageTime / maxAverage * width + xOffset;
                GL.Vertex2(xOffset, bottom);
                GL.Vertex2(xOffset, top);
                GL.Vertex2(right, top);

                GL.Vertex2(right, top);
                GL.Vertex2(right, bottom);
                GL.Vertex2(xOffset, bottom);

                // Total
                GL.Color3(Color.Red);
                top += barHeight;
                bottom += barHeight;
                right = (float)entry.Value.TotalTime / maxTotal * width + xOffset;
                GL.Vertex2(xOffset, bottom);
                GL.Vertex2(xOffset, top);
                GL.Vertex2(right, top);

                GL.Vertex2(right, top);
                GL.Vertex2(right, bottom);
                GL.Vertex2(xOffset, bottom);
            }
            GL.End();
            GL.Disable(EnableCap.ScissorTest);
            fontService.DrawText("Blue: Instant,  Green: Avg,  Red: Total", xOffset, Height - titleFontHeight, titleFontHeight);
            xOffset = Width - 360;

            // Display timestamps
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in profileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                fontService.DrawText($"{Profile.ConvertTicksToMS(entry.Value.LastTime):F3}", xOffset, y, lineHeight);
                fontService.DrawText($"{Profile.ConvertTicksToMS(entry.Value.AverageTime):F3}", columnSpacing + 100 + xOffset, y, lineHeight);
                fontService.DrawText($"{Profile.ConvertTicksToMS(entry.Value.TotalTime):F3}", columnSpacing + columnSpacing + 200 + xOffset, y, lineHeight);
            }
            GL.Disable(EnableCap.ScissorTest);

            float yHeight = Height - titleFontHeight;

            fontService.DrawText("Instant (ms)", xOffset, yHeight, titleFontHeight);
            buttons[(int)ButtonIndex.InstantTitle].UpdateSize((int)xOffset, (int)yHeight, 0, (int)(columnSpacing + 100), titleFontHeight);

            fontService.DrawText("Average (ms)", columnSpacing + 100 + xOffset, yHeight, titleFontHeight);
            buttons[(int)ButtonIndex.AverageTitle].UpdateSize((int)(columnSpacing + 100 + xOffset), (int)yHeight, 0, (int)(columnSpacing + 100), titleFontHeight);

            fontService.DrawText("Total (ms)", columnSpacing + columnSpacing + 200 + xOffset, yHeight, titleFontHeight);
            buttons[(int)ButtonIndex.TotalTitle].UpdateSize((int)(columnSpacing + columnSpacing + 200 + xOffset), (int)yHeight, 0, Width, titleFontHeight);

            // Draw buttons
            foreach (ProfileButton button in buttons)
            {
                button.Draw();
            }

            SwapBuffers();
        }
        #endregion

        private float GetLineY(float offset, float lineHeight, float padding, bool centre, int line)
        {
            return Height + offset - lineHeight - padding - ((lineHeight + padding) * line) + ((centre) ? padding : 0);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            foreach (ProfileButton button in buttons)
            {
                if (button.ProcessClick(e.X, Height - e.Y))
                    return;
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            scrollPos += e.Delta * -30;
            if (scrollPos < minScroll)
                scrollPos = minScroll;
            if (scrollPos > maxScroll)
                scrollPos = maxScroll;
        }
    }
}