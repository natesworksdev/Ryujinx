using ImGuiNET;
using System;

namespace Ryujinx.Common.Osd
{
    public interface IOsdRenderer : IDisposable
    {
        void Initialize(ImGuiIOPtr io);
        void Render(ImDrawDataPtr drawData, int texture);
        void UpdateRenderTarget(int width, int height);
    }
}