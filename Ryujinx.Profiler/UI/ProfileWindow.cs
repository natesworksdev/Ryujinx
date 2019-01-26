using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
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
            FilterBar = 4,
            ShowHideInactive = 5,
            Pause = 6,
        }

        private bool VisibleLocal = true, InitComplete = false, ViewportUpdated = true;
        public bool VisibleChangedLocal;
        private FontService FontServ;
        private List<KeyValuePair<ProfileConfig, TimingInfo>> RawPofileData, ProfileData;

        private float ScrollPos = 0;
        private float MinScroll = 0, MaxScroll = 0;

        private ProfileButton[] Buttons;
        private IComparer<KeyValuePair<ProfileConfig, TimingInfo>> SortAction;

        private string FilterText = "";
        private double BackspaceDownTime, UpdateTimer;
        private bool BackspaceDown = false, PrevBackspaceDown = false, RegexEnabled = false, ProfileUpdated = false;
        private bool ShowInactive = true, Paused = false;

        public ProfileWindow()
            : base(1280, 720)
        {
            Location = new Point(DisplayDevice.Default.Width - 1280, (DisplayDevice.Default.Height - 720) - 50);
            Title = "Profiler";
            SortAction = null;
            BackspaceDownTime = 0;

            // Large number to force an update on first update
            UpdateTimer = 0xFFFF;
        }

        #region Public Methods
        public void ToggleVisible()
        {
            VisibleLocal = !VisibleLocal;
            VisibleChangedLocal = true;
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
            FontServ = new FontService();
            FontServ.InitalizeTextures();
            FontServ.UpdateScreenHeight(Height);

            Buttons = new ProfileButton[7];
            Buttons[(int)ButtonIndex.TagTitle]         = new ProfileButton(FontServ, () => SortAction = new ProfileSorters.TagAscending());
            Buttons[(int)ButtonIndex.InstantTitle]     = new ProfileButton(FontServ, () => SortAction = new ProfileSorters.InstantAscending());
            Buttons[(int)ButtonIndex.AverageTitle]     = new ProfileButton(FontServ, () => SortAction = new ProfileSorters.AverageAscending());
            Buttons[(int)ButtonIndex.TotalTitle]       = new ProfileButton(FontServ, () => SortAction = new ProfileSorters.TotalAscending());
            Buttons[(int)ButtonIndex.FilterBar]        = new ProfileButton(FontServ, () =>
            {
                ProfileUpdated = true;
                RegexEnabled = !RegexEnabled;
            });
            Buttons[(int)ButtonIndex.ShowHideInactive] = new ProfileButton(FontServ, () =>
            {
                ProfileUpdated = true;
                ShowInactive = !ShowInactive;
            });
            Buttons[(int)ButtonIndex.Pause] = new ProfileButton(FontServ, () =>
            {
                ProfileUpdated = true;
                Paused = !Paused;
            });
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
            ViewportUpdated = true;
        }
        #endregion

        #region OnClose
        /// <summary>
        /// Intercept close event and hide instead
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            VisibleLocal = false;
            VisibleChangedLocal = true;
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
            InitComplete = true;
            
            // Backspace handling
            if (BackspaceDown)
            {
                if (!PrevBackspaceDown)
                {
                    BackspaceDownTime = 0;
                    FilterBackspace();
                }
                else
                {
                    BackspaceDownTime += e.Time;
                    if (BackspaceDownTime > 0.3)
                    {
                        BackspaceDownTime -= 0.05;
                        FilterBackspace();
                    }
                }
            }
            PrevBackspaceDown = BackspaceDown;

            // Get timing data if enough time has passed
            UpdateTimer += e.Time;
            if (!Paused && (UpdateTimer > Profile.GetUpdateRate()))
            {
                UpdateTimer %= Profile.GetUpdateRate();
                RawPofileData = Profile.GetProfilingData().ToList();
                ProfileUpdated = true;
            }
            
            // Filtering
            if (ProfileUpdated)
            {
                if (ShowInactive)
                {
                    ProfileData = RawPofileData;
                }
                else
                {
                    ProfileData = RawPofileData.FindAll(kvp => kvp.Value.Instant > 0.001f);
                }

                if (SortAction != null)
                {
                    ProfileData.Sort(SortAction);
                }

                if (RegexEnabled)
                {
                    try
                    {
                        Regex filterRegex = new Regex(FilterText, RegexOptions.IgnoreCase);
                        if (FilterText != "")
                        {
                            ProfileData = ProfileData.Where((pair => filterRegex.IsMatch(pair.Key.Search))).ToList();
                        }
                    }
                    catch (ArgumentException argException)
                    {
                        // Skip filtering for invalid regex
                    }
                }
                else
                {
                    // Regular filtering
                    ProfileData = ProfileData.Where((pair => pair.Key.Search.ToLower().Contains(FilterText.ToLower()))).ToList();
                }

                ProfileUpdated = false;
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
            if (VisibleChangedLocal)
            {
                Visible = VisibleLocal;
                VisibleChangedLocal = false;
            }

            if (!VisibleLocal || !InitComplete)
            {
                return;
            }
            
            // Update viewport
            if (ViewportUpdated)
            {
                GL.Viewport(0, 0, Width, Height);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, Width, 0, Height, 0.0, 4.0);

                FontServ.UpdateScreenHeight(Height);

                ViewportUpdated = false;
            }

            // Frame setup
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(Color.Black);

            FontServ.fontColor = Color.White;
            int verticalIndex = 0;
            int lineHeight = 16;
            int titleHeight = 24;
            int titleFontHeight = 16;
            int linePadding = 2;
            int columnSpacing = 30;
            int filterHeight = 24;

            float width;
            float maxWidth = 0;
            float yOffset = ScrollPos - titleHeight;
            float xOffset = 10;

            // Background lines to make reading easier
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(0, filterHeight, Width, Height - titleHeight - filterHeight);
            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(0.2f, 0.2f, 0.2f);
            for (int i = 0; i < ProfileData.Count; i += 2)
            {
                float top = GetLineY(yOffset, lineHeight, linePadding, false, i - 1);
                float bottom = GetLineY(yOffset, lineHeight, linePadding, false, i);

                // Skip rendering out of bounds bars
                if (top < 0 || bottom > Height)
                    continue;

                GL.Vertex2(0, bottom);
                GL.Vertex2(0, top);
                GL.Vertex2(Width, top);

                GL.Vertex2(Width, top);
                GL.Vertex2(Width, bottom);
                GL.Vertex2(0, bottom);
            }
            GL.End();
            MaxScroll = (lineHeight + linePadding) * (ProfileData.Count - 1);

            // Display category
            verticalIndex = 0;
            foreach (var entry in ProfileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                width = FontServ.DrawText(entry.Key.Category, xOffset, y, lineHeight);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            GL.Disable(EnableCap.ScissorTest);

            width = FontServ.DrawText("Category", xOffset, Height - titleFontHeight, titleFontHeight);
            if (width > maxWidth)
                maxWidth = width;

            xOffset += maxWidth + columnSpacing;
            

            // Display session group
            maxWidth = 0;
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in ProfileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                width = FontServ.DrawText(entry.Key.SessionGroup, xOffset, y, lineHeight);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            GL.Disable(EnableCap.ScissorTest);

            width = FontServ.DrawText("Group", xOffset, Height - titleFontHeight, titleFontHeight);
            if (width > maxWidth)
                maxWidth = width;

            xOffset += maxWidth + columnSpacing;

            // Display session item
            maxWidth = 0;
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in ProfileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                width = FontServ.DrawText(entry.Key.SessionItem, xOffset, y, lineHeight);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            GL.Disable(EnableCap.ScissorTest);

            width = FontServ.DrawText("Item", xOffset, Height - titleFontHeight, titleFontHeight);
            if (width > maxWidth)
                maxWidth = width;

            xOffset += maxWidth + columnSpacing;
            Buttons[(int)ButtonIndex.TagTitle].UpdateSize(0, Height - titleFontHeight, 0, (int)xOffset, titleFontHeight);

            // Time bars
            if (ProfileData.Count != 0)
            {
                width = Width - xOffset - 370;
                long maxAverage, maxTotal;
                float barHeight = (lineHeight - linePadding) / 3.0f;
                verticalIndex = 0;

                // Get max values
                var maxInstant = maxAverage = maxTotal = 0;
                foreach (KeyValuePair<ProfileConfig, TimingInfo> kvp in ProfileData)
                {
                    maxInstant = Math.Max(maxInstant, kvp.Value.Instant);
                    maxAverage = Math.Max(maxAverage, kvp.Value.AverageTime);
                    maxTotal = Math.Max(maxTotal, kvp.Value.TotalTime);
                }

                GL.Enable(EnableCap.ScissorTest);
                GL.Begin(PrimitiveType.Triangles);
                foreach (var entry in ProfileData)
                {
                    // Instant
                    GL.Color3(Color.Blue);
                    float bottom = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                    float top = bottom + barHeight;
                    float right = (float) entry.Value.Instant / maxInstant * width + xOffset;

                    // Skip rendering out of bounds bars
                    if (top < 0 || bottom > Height)
                        continue;

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
                    right = (float) entry.Value.AverageTime / maxAverage * width + xOffset;
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
                    right = (float) entry.Value.TotalTime / maxTotal * width + xOffset;
                    GL.Vertex2(xOffset, bottom);
                    GL.Vertex2(xOffset, top);
                    GL.Vertex2(right, top);

                    GL.Vertex2(right, top);
                    GL.Vertex2(right, bottom);
                    GL.Vertex2(xOffset, bottom);
                }

                GL.End();
                GL.Disable(EnableCap.ScissorTest);
            }

            FontServ.DrawText("Blue: Instant,  Green: Avg,  Red: Total", xOffset, Height - titleFontHeight, titleFontHeight);
            xOffset = Width - 360;

            // Display timestamps
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in ProfileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                FontServ.DrawText($"{Profile.ConvertTicksToMS(entry.Value.Instant):F3} ({entry.Value.InstantCount})", xOffset, y, lineHeight);
                FontServ.DrawText($"{Profile.ConvertTicksToMS(entry.Value.AverageTime):F3}", columnSpacing + 120 + xOffset, y, lineHeight);
                FontServ.DrawText($"{Profile.ConvertTicksToMS(entry.Value.TotalTime):F3}", columnSpacing + columnSpacing + 200 + xOffset, y, lineHeight);
            }
            GL.Disable(EnableCap.ScissorTest);

            float yHeight = Height - titleFontHeight;

            FontServ.DrawText("Instant (ms, count)", xOffset, yHeight, titleFontHeight);
            Buttons[(int)ButtonIndex.InstantTitle].UpdateSize((int)xOffset, (int)yHeight, 0, (int)(columnSpacing + 100), titleFontHeight);

            FontServ.DrawText("Average (ms)", columnSpacing + 120 + xOffset, yHeight, titleFontHeight);
            Buttons[(int)ButtonIndex.AverageTitle].UpdateSize((int)(columnSpacing + 120 + xOffset), (int)yHeight, 0, (int)(columnSpacing + 100), titleFontHeight);

            FontServ.DrawText("Total (ms)", columnSpacing + columnSpacing + 200 + xOffset, yHeight, titleFontHeight);
            Buttons[(int)ButtonIndex.TotalTitle].UpdateSize((int)(columnSpacing + columnSpacing + 200 + xOffset), (int)yHeight, 0, Width, titleFontHeight);

            // Show/Hide Inactive
            float widthShowHideButton = Buttons[(int)ButtonIndex.ShowHideInactive].UpdateSize($"{(ShowInactive ? "Hide" : "Show")} Inactive", 5, 5, 4, 16);

            // Play/Pause
            width = Buttons[(int)ButtonIndex.Pause].UpdateSize(Paused ? "Play" : "Pause", 15 + (int)widthShowHideButton, 5, 4, 16) + widthShowHideButton;

            // Filter bar
            FontServ.DrawText($"{(RegexEnabled ? "Regex " : "Filter")}: {FilterText}", 25 + width, 7, 16);
            Buttons[(int) ButtonIndex.FilterBar].UpdateSize((int)(25 + width), 0, 0, Width, filterHeight);

            // Draw buttons
            foreach (ProfileButton button in Buttons)
            {
                button.Draw();
            }

            // Dividing lines
            GL.Color3(Color.White);
            GL.Begin(PrimitiveType.Lines);
            // Top divider
            GL.Vertex2(0, Height -titleHeight);
            GL.Vertex2(Width, Height - titleHeight);

            // Bottom divider
            GL.Vertex2(0, filterHeight);
            GL.Vertex2(Width, filterHeight);

            // Bottom vertical divider
            GL.Vertex2(widthShowHideButton + 10, 0);
            GL.Vertex2(widthShowHideButton + 10, filterHeight);

            GL.Vertex2(width + 20, 0);
            GL.Vertex2(width + 20, filterHeight);
            GL.End();
            SwapBuffers();
        }
        #endregion

        private void FilterBackspace()
        {
            if (FilterText.Length <= 1)
            {
                FilterText = "";
            }
            else
            {
                FilterText = FilterText.Remove(FilterText.Length - 1, 1);
            }
        }

        private float GetLineY(float offset, float lineHeight, float padding, bool centre, int line)
        {
            return Height + offset - lineHeight - padding - ((lineHeight + padding) * line) + ((centre) ? padding : 0);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            FilterText += e.KeyChar;
            ProfileUpdated = true;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.BackSpace)
            {
                ProfileUpdated = BackspaceDown = true;
                return;
            }
            base.OnKeyUp(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.BackSpace)
            {
                BackspaceDown = false;
                return;
            }
            base.OnKeyUp(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            foreach (ProfileButton button in Buttons)
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
            ScrollPos += e.Delta * -30;
            if (ScrollPos < MinScroll)
                ScrollPos = MinScroll;
            if (ScrollPos > MaxScroll)
                ScrollPos = MaxScroll;
        }
    }
}