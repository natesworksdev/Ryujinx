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

        private bool visible = true, initComplete = false, viewportUpdated = true;
        public bool visibleChanged;
        private FontService fontService;
        private List<KeyValuePair<ProfileConfig, TimingInfo>> rawPofileData, profileData;

        private float scrollPos = 0;
        private float minScroll = 0, maxScroll = 0;

        private ProfileButton[] buttons;
        private IComparer<KeyValuePair<ProfileConfig, TimingInfo>> sortAction;

        private string FilterText = "";
        private double BackspaceDownTime, UpdateTimer;
        private bool BackspaceDown = false, prevBackspaceDown = false, regexEnabled = false, ProfileUpdated = false;
        private bool showInactive = true, Paused = false;

        public ProfileWindow()
            : base(1280, 720)
        {
            Location = new Point(DisplayDevice.Default.Width - 1280, (DisplayDevice.Default.Height - 720) - 50);
            Title = "Profiler";
            sortAction = null;
            BackspaceDownTime = 0;

            // Large number to force an update on first update
            UpdateTimer = 0xFFFF;
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
            fontService.UpdateScreenHeight(Height);

            buttons = new ProfileButton[7];
            buttons[(int)ButtonIndex.TagTitle]         = new ProfileButton(fontService, () => sortAction = new ProfileSorters.TagAscending());
            buttons[(int)ButtonIndex.InstantTitle]     = new ProfileButton(fontService, () => sortAction = new ProfileSorters.InstantAscending());
            buttons[(int)ButtonIndex.AverageTitle]     = new ProfileButton(fontService, () => sortAction = new ProfileSorters.AverageAscending());
            buttons[(int)ButtonIndex.TotalTitle]       = new ProfileButton(fontService, () => sortAction = new ProfileSorters.TotalAscending());
            buttons[(int)ButtonIndex.FilterBar]        = new ProfileButton(fontService, () =>
            {
                ProfileUpdated = true;
                regexEnabled = !regexEnabled;
            });
            buttons[(int)ButtonIndex.ShowHideInactive] = new ProfileButton(fontService, () =>
            {
                ProfileUpdated = true;
                showInactive = !showInactive;
            });
            buttons[(int)ButtonIndex.Pause] = new ProfileButton(fontService, () =>
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
            viewportUpdated = true;
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
            
            // Backspace handling
            if (BackspaceDown)
            {
                if (!prevBackspaceDown)
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
            prevBackspaceDown = BackspaceDown;

            // Get timing data if enough time has passed
            UpdateTimer += e.Time;
            if (!Paused && (UpdateTimer > Profile.GetUpdateRate()))
            {
                UpdateTimer %= Profile.GetUpdateRate();
                rawPofileData = Profile.GetProfilingData().ToList();
                ProfileUpdated = true;
            }
            
            // Filtering
            if (ProfileUpdated)
            {
                if (showInactive)
                {
                    profileData = rawPofileData;
                }
                else
                {
                    profileData = rawPofileData.FindAll(kvp => kvp.Value.Instant > 0.001f);
                }

                if (sortAction != null)
                {
                    profileData.Sort(sortAction);
                }

                if (regexEnabled)
                {
                    try
                    {
                        Regex filterRegex = new Regex(FilterText, RegexOptions.IgnoreCase);
                        if (FilterText != "")
                        {
                            profileData = profileData.Where((pair => filterRegex.IsMatch(pair.Key.Search))).ToList();
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
                    profileData = profileData.Where((pair => pair.Key.Search.ToLower().Contains(FilterText.ToLower()))).ToList();
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
            if (visibleChanged)
            {
                Visible = visible;
                visibleChanged = false;
            }

            if (!visible || !initComplete)
            {
                return;
            }
            
            // Update viewport
            if (viewportUpdated)
            {
                GL.Viewport(0, 0, Width, Height);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, Width, 0, Height, 0.0, 4.0);

                fontService.UpdateScreenHeight(Height);

                viewportUpdated = false;
            }

            // Frame setup
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(Color.Black);

            fontService.fontColor = Color.White;
            int verticalIndex = 0;
            int lineHeight = 16;
            int titleHeight = 24;
            int titleFontHeight = 16;
            int linePadding = 2;
            int columnSpacing = 30;
            int filterHeight = 24;

            float width;
            float maxWidth = 0;
            float yOffset = scrollPos - titleHeight;
            float xOffset = 10;

            // Background lines to make reading easier
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(0, filterHeight, Width, Height - titleHeight - filterHeight);
            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(0.2f, 0.2f, 0.2f);
            for (int i = 0; i < profileData.Count; i += 2)
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
            if (profileData.Count != 0)
            {
                width = Width - xOffset - 370;
                long maxAverage, maxTotal;
                float barHeight = (lineHeight - linePadding) / 3.0f;
                verticalIndex = 0;

                // Get max values
                var maxInstant = maxAverage = maxTotal = 0;
                foreach (KeyValuePair<ProfileConfig, TimingInfo> kvp in profileData)
                {
                    maxInstant = Math.Max(maxInstant, kvp.Value.Instant);
                    maxAverage = Math.Max(maxAverage, kvp.Value.AverageTime);
                    maxTotal = Math.Max(maxTotal, kvp.Value.TotalTime);
                }

                GL.Enable(EnableCap.ScissorTest);
                GL.Begin(PrimitiveType.Triangles);
                foreach (var entry in profileData)
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

            fontService.DrawText("Blue: Instant,  Green: Avg,  Red: Total", xOffset, Height - titleFontHeight, titleFontHeight);
            xOffset = Width - 360;

            // Display timestamps
            verticalIndex = 0;
            GL.Enable(EnableCap.ScissorTest);
            foreach (var entry in profileData)
            {
                float y = GetLineY(yOffset, lineHeight, linePadding, true, verticalIndex++);
                fontService.DrawText($"{Profile.ConvertTicksToMS(entry.Value.Instant):F3} ({entry.Value.InstantCount})", xOffset, y, lineHeight);
                fontService.DrawText($"{Profile.ConvertTicksToMS(entry.Value.AverageTime):F3}", columnSpacing + 120 + xOffset, y, lineHeight);
                fontService.DrawText($"{Profile.ConvertTicksToMS(entry.Value.TotalTime):F3}", columnSpacing + columnSpacing + 200 + xOffset, y, lineHeight);
            }
            GL.Disable(EnableCap.ScissorTest);

            float yHeight = Height - titleFontHeight;

            fontService.DrawText("Instant (ms, count)", xOffset, yHeight, titleFontHeight);
            buttons[(int)ButtonIndex.InstantTitle].UpdateSize((int)xOffset, (int)yHeight, 0, (int)(columnSpacing + 100), titleFontHeight);

            fontService.DrawText("Average (ms)", columnSpacing + 120 + xOffset, yHeight, titleFontHeight);
            buttons[(int)ButtonIndex.AverageTitle].UpdateSize((int)(columnSpacing + 120 + xOffset), (int)yHeight, 0, (int)(columnSpacing + 100), titleFontHeight);

            fontService.DrawText("Total (ms)", columnSpacing + columnSpacing + 200 + xOffset, yHeight, titleFontHeight);
            buttons[(int)ButtonIndex.TotalTitle].UpdateSize((int)(columnSpacing + columnSpacing + 200 + xOffset), (int)yHeight, 0, Width, titleFontHeight);

            // Show/Hide Inactive
            float widthShowHideButton = buttons[(int)ButtonIndex.ShowHideInactive].UpdateSize($"{(showInactive ? "Hide" : "Show")} Inactive", 5, 5, 4, 16);

            // Play/Pause
            width = buttons[(int)ButtonIndex.Pause].UpdateSize(Paused ? "Play" : "Pause", 15 + (int)widthShowHideButton, 5, 4, 16) + widthShowHideButton;

            // Filter bar
            fontService.DrawText($"{(regexEnabled ? "Regex " : "Filter")}: {FilterText}", 25 + width, 7, 16);
            buttons[(int) ButtonIndex.FilterBar].UpdateSize((int)(25 + width), 0, 0, Width, filterHeight);

            // Draw buttons
            foreach (ProfileButton button in buttons)
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