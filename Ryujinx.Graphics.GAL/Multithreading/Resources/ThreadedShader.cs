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
        private bool _created;
        private string _code;
        private byte[] _byteCode;

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
            _byteCode = code;
        }

        internal void EnsureCreated()
        {
            if (!_created && Base == null)
            {
                if (_code != null)
                {
                    Base = _renderer.BaseRenderer.CompileShader(_stage, _bindings, _code);
                    _code = null;
                }
                else
                {
                    Base = _renderer.BaseRenderer.CompileShader(_stage, _bindings, _byteCode);
                    _byteCode = null;
                }

                _created = true;
            }
        }

        public void Dispose()
        {
            _renderer.New<ShaderDisposeCommand>().Set(new TableRef<ThreadedShader>(_renderer, this));
            _renderer.QueueCommand();
        }
    }
}
