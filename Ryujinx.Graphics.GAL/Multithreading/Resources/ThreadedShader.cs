using Ryujinx.Graphics.GAL.Multithreading.Commands.Shader;
using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.GAL.Multithreading.Resources
{
    class ThreadedShader : IShader
    {
        private ThreadedRenderer _renderer;
        private ShaderStage _stage;
        private ShaderBindings _bindings;
        private string _code;
        private byte[] _binaryCode;

        public IShader Base;

        public ThreadedShader(ThreadedRenderer renderer, ShaderStage stage, ShaderBindings bindings, string code)
        {
            _renderer = renderer;

            _stage = stage;
            _bindings = bindings;
            _code = code;
        }

        public ThreadedShader(ThreadedRenderer renderer, ShaderStage stage, ShaderBindings bindings, byte[] code)
        {
            _renderer = renderer;

            _stage = stage;
            _bindings = bindings;
            _binaryCode = code;
        }

        internal void EnsureCreated()
        {
            if ((_code != null || _binaryCode != null) && Base == null)
            {
                Base = _binaryCode != null
                    ? _renderer.BaseRenderer.CompileShader(_stage, _bindings, _binaryCode)
                    : _renderer.BaseRenderer.CompileShader(_stage, _bindings, _code);
                _code = null;
                _binaryCode = null;
            }
        }

        public void Dispose()
        {
            _renderer.New<ShaderDisposeCommand>().Set(new TableRef<ThreadedShader>(_renderer, this));
            _renderer.QueueCommand();
        }
    }
}
