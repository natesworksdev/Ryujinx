using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Osd
{
    public class OsdContext : IDisposable
    {
        private Stopwatch _timer;
        private float _lastUpdate;
        private ImGuiIOPtr _io;
        private IOsdRenderer _renderer;
        private bool _initialized;
        private bool _updateRenderTarget;
        private Vector2 _size;

        public event EventHandler OnUi;

        public OsdContext()
        {
            _timer = new Stopwatch();
            _timer.Start();
        }

        public void InitializeRenderer(IOsdRenderer renderer)
        {
            ImGui.CreateContext();
            ImGui.StyleColorsDark();

            _io = ImGui.GetIO();
            _renderer = renderer;
            _renderer.Initialize(_io);
            _initialized = true;
        }

        public void Dispose()
        {
            _renderer?.Dispose();
            ImGui.DestroyContext();
        }

        public void UpdateSize(Vector2 size)
        {
            if (_initialized)
            {
                _size = size;
                _io.DisplaySize = size;
                _io.DisplayFramebufferScale = Vector2.One;
                _updateRenderTarget = true;
            }
        }

        public void RenderUi(int stagingTexture)
        {
            if (!_initialized)
            {
                return;
            }

            var currentTime = _timer.ElapsedMilliseconds / 1000f;
            _io.DeltaTime = _lastUpdate > 0.0 ? currentTime - _lastUpdate : 1.0f / 60.0f;
            _lastUpdate = _timer.ElapsedMilliseconds / 1000f;

            if (_updateRenderTarget)
            {
                _renderer.UpdateRenderTarget((int)_size.X, (int)_size.Y);
                _updateRenderTarget = false;
            }

            ImGui.NewFrame();

            OnUi?.Invoke(this, null);

            ImGui.Render();
            _renderer.Render(ImGui.GetDrawData(), stagingTexture);
        }
    }
}
