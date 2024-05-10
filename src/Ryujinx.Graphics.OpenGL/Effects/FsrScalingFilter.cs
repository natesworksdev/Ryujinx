using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using Silk.NET.OpenGL.Legacy;
using System;
using static Ryujinx.Graphics.OpenGL.Effects.ShaderHelper;

namespace Ryujinx.Graphics.OpenGL.Effects
{
    internal class FsrScalingFilter : IScalingFilter
    {
        private readonly OpenGLRenderer _gd;
        private int _inputUniform;
        private int _outputUniform;
        private int _sharpeningUniform;
        private int _srcX0Uniform;
        private int _srcX1Uniform;
        private int _srcY0Uniform;
        private uint _scalingShaderProgram;
        private uint _sharpeningShaderProgram;
        private float _scale = 1;
        private int _srcY1Uniform;
        private int _dstX0Uniform;
        private int _dstX1Uniform;
        private int _dstY0Uniform;
        private int _dstY1Uniform;
        private int _scaleXUniform;
        private int _scaleYUniform;
        private TextureStorage _intermediaryTexture;

        public float Level
        {
            get => _scale;
            set
            {
                _scale = MathF.Max(0.01f, value);
            }
        }

        public FsrScalingFilter(OpenGLRenderer gd)
        {
            Initialize();

            _gd = gd;
        }

        public void Dispose()
        {
            if (_scalingShaderProgram != 0)
            {
                _gd.Api.DeleteProgram(_scalingShaderProgram);
                _gd.Api.DeleteProgram(_sharpeningShaderProgram);
            }

            _intermediaryTexture?.Dispose();
        }

        private void Initialize()
        {
            var scalingShader = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/fsr_scaling.glsl");
            var sharpeningShader = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/fsr_sharpening.glsl");
            var fsrA = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/ffx_a.h");
            var fsr1 = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/ffx_fsr1.h");

            scalingShader = scalingShader.Replace("#include \"ffx_a.h\"", fsrA);
            scalingShader = scalingShader.Replace("#include \"ffx_fsr1.h\"", fsr1);
            sharpeningShader = sharpeningShader.Replace("#include \"ffx_a.h\"", fsrA);
            sharpeningShader = sharpeningShader.Replace("#include \"ffx_fsr1.h\"", fsr1);

            _scalingShaderProgram = CompileProgram(_gd.Api, scalingShader, ShaderType.ComputeShader);
            _sharpeningShaderProgram = CompileProgram(_gd.Api, sharpeningShader, ShaderType.ComputeShader);

            _inputUniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "Source");
            _outputUniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "imgOutput");
            _sharpeningUniform = _gd.Api.GetUniformLocation(_sharpeningShaderProgram, "sharpening");

            _srcX0Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "srcX0");
            _srcX1Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "srcX1");
            _srcY0Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "srcY0");
            _srcY1Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "srcY1");
            _dstX0Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "dstX0");
            _dstX1Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "dstX1");
            _dstY0Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "dstY0");
            _dstY1Uniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "dstY1");
            _scaleXUniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "scaleX");
            _scaleYUniform = _gd.Api.GetUniformLocation(_scalingShaderProgram, "scaleY");
        }

        public void Run(
            TextureView view,
            TextureView destinationTexture,
            int width,
            int height,
            Extents2D source,
            Extents2D destination)
        {
            if (_intermediaryTexture == null || _intermediaryTexture.Info.Width != width || _intermediaryTexture.Info.Height != height)
            {
                _intermediaryTexture?.Dispose();
                var originalInfo = view.Info;
                var info = new TextureCreateInfo(width,
                    height,
                    originalInfo.Depth,
                    originalInfo.Levels,
                    originalInfo.Samples,
                    originalInfo.BlockWidth,
                    originalInfo.BlockHeight,
                    originalInfo.BytesPerPixel,
                    originalInfo.Format,
                    originalInfo.DepthStencilMode,
                    originalInfo.Target,
                    originalInfo.SwizzleR,
                    originalInfo.SwizzleG,
                    originalInfo.SwizzleB,
                    originalInfo.SwizzleA);

                _intermediaryTexture = new TextureStorage(_gd, info);
                _intermediaryTexture.CreateDefaultView();
            }

            var textureView = _intermediaryTexture.CreateView(_intermediaryTexture.Info, 0, 0) as TextureView;

            uint previousProgram = (uint)_gd.Api.GetInteger(GetPName.CurrentProgram);
            int previousUnit = _gd.Api.GetInteger(GetPName.ActiveTexture);
            _gd.Api.ActiveTexture(TextureUnit.Texture0);
            uint previousTextureBinding = (uint)_gd.Api.GetInteger(GetPName.TextureBinding2D);

            _gd.Api.BindImageTexture(0, textureView.Handle, 0, false, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);

            int threadGroupWorkRegionDim = 16;
            uint dispatchX = (uint)((width + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim);
            uint dispatchY = (uint)((height + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim);

            // Scaling pass
            float srcWidth = Math.Abs(source.X2 - source.X1);
            float srcHeight = Math.Abs(source.Y2 - source.Y1);
            float scaleX = srcWidth / view.Width;
            float scaleY = srcHeight / view.Height;
            _gd.Api.UseProgram(_scalingShaderProgram);
            view.Bind(0);
            _gd.Api.Uniform1(_inputUniform, 0);
            _gd.Api.Uniform1(_outputUniform, 0);
            _gd.Api.Uniform1(_srcX0Uniform, (float)source.X1);
            _gd.Api.Uniform1(_srcX1Uniform, (float)source.X2);
            _gd.Api.Uniform1(_srcY0Uniform, (float)source.Y1);
            _gd.Api.Uniform1(_srcY1Uniform, (float)source.Y2);
            _gd.Api.Uniform1(_dstX0Uniform, (float)destination.X1);
            _gd.Api.Uniform1(_dstX1Uniform, (float)destination.X2);
            _gd.Api.Uniform1(_dstY0Uniform, (float)destination.Y1);
            _gd.Api.Uniform1(_dstY1Uniform, (float)destination.Y2);
            _gd.Api.Uniform1(_scaleXUniform, scaleX);
            _gd.Api.Uniform1(_scaleYUniform, scaleY);
            _gd.Api.DispatchCompute(dispatchX, dispatchY, 1);

            _gd.Api.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            // Sharpening Pass
            _gd.Api.UseProgram(_sharpeningShaderProgram);
            _gd.Api.BindImageTexture(0, destinationTexture.Handle, 0, false, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
            textureView.Bind(0);
            _gd.Api.Uniform1(_inputUniform, 0);
            _gd.Api.Uniform1(_outputUniform, 0);
            _gd.Api.Uniform1(_sharpeningUniform, 1.5f - (Level * 0.01f * 1.5f));
            _gd.Api.DispatchCompute(dispatchX, dispatchY, 1);

            _gd.Api.UseProgram(previousProgram);
            _gd.Api.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            (_gd.Pipeline as Pipeline).RestoreImages1And2();

            _gd.Api.ActiveTexture(TextureUnit.Texture0);
            _gd.Api.BindTexture(TextureTarget.Texture2D, previousTextureBinding);

            _gd.Api.ActiveTexture((TextureUnit)previousUnit);
        }
    }
}
