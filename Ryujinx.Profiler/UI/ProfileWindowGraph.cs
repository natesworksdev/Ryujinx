using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Ryujinx.Profiler.UI
{
    public partial class ProfileWindow
    {
        private void DrawGraph(float xOffset, float yOffset, float width)
        {
            if (_sortedProfileData.Count != 0)
            {
                int   left, right;
                float top, bottom;

                int   verticalIndex = 0;
                float barHeight     = (LineHeight - LinePadding);
                long  timeWidth     = Profile.HistoryLength;

                GL.Enable(EnableCap.ScissorTest);
                GL.Begin(PrimitiveType.Triangles);
                foreach (var entry in _sortedProfileData)
                {
                    GL.Color3(Color.Green);
                    foreach (Timestamp timestamp in entry.Value.GetAllTimestamps())
                    {
                        left   = (int)(xOffset + width - (((float)_captureTime - timestamp.BeginTime) / timeWidth) * width);
                        right  = (int)(xOffset + width - (((float)_captureTime - timestamp.EndTime) / timeWidth) * width);
                        bottom = GetLineY(yOffset, LineHeight, LinePadding, true, verticalIndex);
                        top    = bottom + barHeight;

                        // Make sure width is at least 1px
                        right = Math.Max(left + 1, right);

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
                        left   = (int)(xOffset + width - (((float)_captureTime - entryBegin) / timeWidth) * width);
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
            }
        }
    }
}
