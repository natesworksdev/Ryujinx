using Ryujinx.Graphics.Shader;
using System;

namespace Ryujinx.Graphics.GAL
{
    public interface IRenderer : IDisposable
    {
        IPipeline Pipeline { get; }

        IWindow Window { get; }

        IShader CompileShader(ShaderProgram shader);

        IBuffer CreateBuffer(int size);

        IProgram CreateProgram(IShader[] shaders);

        ISampler CreateSampler(SamplerCreateInfo info);
        ITexture CreateTexture(TextureCreateInfo info);

        Capabilities GetCapabilities();

        ICounterEvent ReportCounter(CounterType type, EventHandler<ulong> resultHandler);

        void Initialize();

        void ResetCounter(CounterType type);
    }
}
