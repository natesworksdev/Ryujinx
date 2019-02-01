using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common;

namespace Ryujinx.Profiler.UI
{
    public partial class ProfileWindow
    {
        private TimingFlag[] _timingFlags;

        private const float GraphMoveSpeed = 40000;
        private const float GraphZoomSpeed = 50;

        private float _graphZoom      = 1;
        private float  _graphPosition = 0;

        private void DrawGraph(float xOffset, float yOffset, float width)
        {
            if (_sortedProfileData.Count != 0)
            {
                int   left, right;
                float top, bottom;

                int   verticalIndex      = 0;
                float barHeight          = (LineHeight - LinePadding);
                long  history            = Profile.HistoryLength;
                long  timeWidthTicks     = (long)(history / (double)_graphZoom);
                long  graphPositionTicks = (long)(_graphPosition * PerformanceCounter.TicksPerMillisecond);

                // Reset start point if out of bounds
                if (timeWidthTicks + graphPositionTicks > history)
                {
                    graphPositionTicks = history - timeWidthTicks;
                    _graphPosition = (float)graphPositionTicks / PerformanceCounter.TicksPerMillisecond;
                }

                // Draw timing flags
                GL.Enable(EnableCap.ScissorTest);
                GL.Color3(Color.Gray);
                GL.Begin(PrimitiveType.Lines);
                foreach (TimingFlag timingFlag in _timingFlags)
                {
                    int x = (int)(xOffset + width - ((float)(_captureTime - (timingFlag.Timestamp + graphPositionTicks)) / timeWidthTicks) * width);
                    GL.Vertex2(x, 0);
                    GL.Vertex2(x, Height);
                }
                GL.End();

                // Draw bars
                GL.Begin(PrimitiveType.Triangles);
                foreach (var entry in _sortedProfileData)
                {
                    int furthest = 0;

                    GL.Color3(Color.Green);
                    foreach (Timestamp timestamp in entry.Value.GetAllTimestamps())
                    {
                        right = (int)(xOffset + width - ((float)(_captureTime - (timestamp.EndTime + graphPositionTicks)) / timeWidthTicks) * width);

                        // Skip drawing multiple timestamps on same pixel
                        if (right <= furthest)
                            continue;

                        left   = (int)(xOffset + width - ((float)(_captureTime - (timestamp.BeginTime + graphPositionTicks)) / timeWidthTicks) * width);
                        bottom = GetLineY(yOffset, LineHeight, LinePadding, true, verticalIndex);
                        top    = bottom + barHeight;

                        // Make sure width is at least 1px
                        right = Math.Max(left + 1, right);

                        furthest = right;

                        // Skip rendering out of bounds bars
                        if (top < 0 || bottom > Height)
                            continue;

                        GL.Vertex2(left,  bottom);
                        GL.Vertex2(left,  top);
                        GL.Vertex2(right, top);

                        GL.Vertex2(right, top);
                        GL.Vertex2(right, bottom);
                        GL.Vertex2(left,  bottom);
                    }

                    GL.Color3(Color.Red);
                    // Currently capturing timestamp
                    long entryBegin = entry.Value.BeginTime;
                    if (entryBegin != -1)
                    {
                        left   = (int)(xOffset + width + _graphPosition - (((float)_captureTime - entryBegin) / timeWidthTicks) * width);
                        bottom = GetLineY(yOffset, LineHeight, LinePadding, true, verticalIndex);
                        top    = bottom + barHeight;
                        right  = (int)(xOffset + width);

                        // Make sure width is at least 1px
                        left = Math.Min(left - 1, right);

                        // Skip rendering out of bounds bars
                        if (top < 0 || bottom > Height)
                            continue;

                        GL.Vertex2(left,  bottom);
                        GL.Vertex2(left,  top);
                        GL.Vertex2(right, top);

                        GL.Vertex2(right, top);
                        GL.Vertex2(right, bottom);
                        GL.Vertex2(left,  bottom);
                    }

                    verticalIndex++;
                }

                GL.End();
                GL.Disable(EnableCap.ScissorTest);

                string label = $"-{MathF.Round(_graphPosition, 2)} ms";

                // Dummy draw for measure
                float labelWidth = _fontService.DrawText(label, 0, 0, LineHeight, false);
                _fontService.DrawText(label, xOffset + width - labelWidth - LinePadding, FilterHeight + LinePadding, LineHeight);
                
                _fontService.DrawText($"-{MathF.Round((float)((timeWidthTicks / PerformanceCounter.TicksPerMillisecond) + _graphPosition), 2)} ms", xOffset + LinePadding, FilterHeight + LinePadding, LineHeight);
            }
        }
    }
}
