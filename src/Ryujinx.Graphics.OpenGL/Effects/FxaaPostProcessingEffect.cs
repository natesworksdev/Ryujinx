using Silk.NET.OpenGL;
using Ryujinx.Common;
using Ryujinx.Graphics.OpenGL.Image;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal class FxaaPostProcessingEffect : IPostProcessingEffect
    {
        private readonly OpenGLRenderer _gd;
        private int _resolutionUniform;
        private int _inputUniform;
        private int _outputUniform;
        private uint _shaderProgram;
        private TextureStorage _textureStorage;

        public FxaaPostProcessingEffect(OpenGLRenderer gd)
        {
            Initialize();

            _gd = gd;
        }

        public void Dispose()
        {
            if (_shaderProgram != 0)
            {
                _gd.Api.DeleteProgram(_shaderProgram);
                _textureStorage?.Dispose();
            }
        }

        private void Initialize()
        {
            _shaderProgram = ShaderHelper.CompileProgram(EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/fxaa.glsl"), ShaderType.ComputeShader);

            _resolutionUniform = _gd.Api.GetUniformLocation(_shaderProgram, "invResolution");
            _inputUniform = _gd.Api.GetUniformLocation(_shaderProgram, "inputTexture");
            _outputUniform = _gd.Api.GetUniformLocation(_shaderProgram, "imgOutput");
        }

        public TextureView Run(TextureView view, int width, int height)
        {
            if (_textureStorage == null || _textureStorage.Info.Width != view.Width || _textureStorage.Info.Height != view.Height)
            {
                _textureStorage?.Dispose();
                _textureStorage = new TextureStorage(_gd, view.Info);
                _textureStorage.CreateDefaultView();
            }

            var textureView = _textureStorage.CreateView(view.Info, 0, 0) as TextureView;

            int previousProgram = _gd.Api.GetInteger(GetPName.CurrentProgram);
            int previousUnit = _gd.Api.GetInteger(GetPName.ActiveTexture);
            _gd.Api.ActiveTexture(TextureUnit.Texture0);
            int previousTextureBinding = _gd.Api.GetInteger(GetPName.TextureBinding2D);

            _gd.Api.BindImageTexture(0, textureView.Handle, 0, false, 0, BufferAccessARB.ReadWrite, SizedInternalFormat.Rgba8);
            _gd.Api.UseProgram(_shaderProgram);

            var dispatchX = BitUtils.DivRoundUp(view.Width, IPostProcessingEffect.LocalGroupSize);
            var dispatchY = BitUtils.DivRoundUp(view.Height, IPostProcessingEffect.LocalGroupSize);

            view.Bind(0);
            _gd.Api.Uniform1(_inputUniform, 0);
            _gd.Api.Uniform1(_outputUniform, 0);
            _gd.Api.Uniform2(_resolutionUniform, (float)view.Width, (float)view.Height);
            _gd.Api.DispatchCompute(dispatchX, dispatchY, 1);
            _gd.Api.UseProgram(previousProgram);
            _gd.Api.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            (_gd.Pipeline as Pipeline).RestoreImages1And2();

            _gd.Api.ActiveTexture(TextureUnit.Texture0);
            _gd.Api.BindTexture(TextureTarget.Texture2D, previousTextureBinding);

            _gd.Api.ActiveTexture((TextureUnit)previousUnit);

            return textureView;
        }
    }
}
