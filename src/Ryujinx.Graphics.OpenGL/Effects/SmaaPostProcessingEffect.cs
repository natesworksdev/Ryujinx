using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using Silk.NET.OpenGL.Legacy;
using System;

namespace Ryujinx.Graphics.OpenGL.Effects.Smaa
{
    internal class SmaaPostProcessingEffect : IPostProcessingEffect
    {
        public const int AreaWidth = 160;
        public const int AreaHeight = 560;
        public const int SearchWidth = 64;
        public const int SearchHeight = 16;

        private readonly OpenGLRenderer _gd;
        private TextureStorage _outputTexture;
        private TextureStorage _searchTexture;
        private TextureStorage _areaTexture;
        private uint[] _edgeShaderPrograms;
        private uint[] _blendShaderPrograms;
        private uint[] _neighbourShaderPrograms;
        private TextureStorage _edgeOutputTexture;
        private TextureStorage _blendOutputTexture;
        private readonly string[] _qualities;
        private int _inputUniform;
        private int _outputUniform;
        private int _samplerAreaUniform;
        private int _samplerSearchUniform;
        private int _samplerBlendUniform;
        private int _resolutionUniform;
        private int _quality = 1;

        public int Quality
        {
            get => _quality;
            set
            {
                _quality = Math.Clamp(value, 0, _qualities.Length - 1);
            }
        }
        public SmaaPostProcessingEffect(OpenGLRenderer gd, int quality)
        {
            _gd = gd;

            _edgeShaderPrograms = Array.Empty<uint>();
            _blendShaderPrograms = Array.Empty<uint>();
            _neighbourShaderPrograms = Array.Empty<uint>();

            _qualities = ["SMAA_PRESET_LOW", "SMAA_PRESET_MEDIUM", "SMAA_PRESET_HIGH", "SMAA_PRESET_ULTRA"];

            Quality = quality;

            Initialize();
        }

        public void Dispose()
        {
            _searchTexture?.Dispose();
            _areaTexture?.Dispose();
            _outputTexture?.Dispose();
            _edgeOutputTexture?.Dispose();
            _blendOutputTexture?.Dispose();

            DeleteShaders();
        }

        private void DeleteShaders()
        {
            for (int i = 0; i < _edgeShaderPrograms.Length; i++)
            {
                _gd.Api.DeleteProgram(_edgeShaderPrograms[i]);
                _gd.Api.DeleteProgram(_blendShaderPrograms[i]);
                _gd.Api.DeleteProgram(_neighbourShaderPrograms[i]);
            }
        }

        private unsafe void RecreateShaders(int width, int height)
        {
            string baseShader = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa.hlsl");
            var pixelSizeDefine = $"#define SMAA_RT_METRICS float4(1.0 / {width}.0, 1.0 / {height}.0, {width}, {height}) \n";

            _edgeShaderPrograms = new uint[_qualities.Length];
            _blendShaderPrograms = new uint[_qualities.Length];
            _neighbourShaderPrograms = new uint[_qualities.Length];

            for (int i = 0; i < +_edgeShaderPrograms.Length; i++)
            {
                var presets = $"#version 430 core \n#define {_qualities[i]} 1 \n{pixelSizeDefine}#define SMAA_GLSL_4 1 \nlayout (local_size_x = 16, local_size_y = 16) in;\n{baseShader}";

                var edgeShaderData = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa_edge.glsl");
                var blendShaderData = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa_blend.glsl");
                var neighbourShaderData = EmbeddedResources.ReadAllText("Ryujinx.Graphics.OpenGL/Effects/Shaders/smaa_neighbour.glsl");

                var shaders = new string[] { presets, edgeShaderData };
                var edgeProgram = ShaderHelper.CompileProgram(_gd.Api, shaders, ShaderType.ComputeShader);

                shaders[1] = blendShaderData;
                var blendProgram = ShaderHelper.CompileProgram(_gd.Api, shaders, ShaderType.ComputeShader);

                shaders[1] = neighbourShaderData;
                var neighbourProgram = ShaderHelper.CompileProgram(_gd.Api, shaders, ShaderType.ComputeShader);

                _edgeShaderPrograms[i] = edgeProgram;
                _blendShaderPrograms[i] = blendProgram;
                _neighbourShaderPrograms[i] = neighbourProgram;
            }

            _inputUniform = _gd.Api.GetUniformLocation(_edgeShaderPrograms[0], "inputTexture");
            _outputUniform = _gd.Api.GetUniformLocation(_edgeShaderPrograms[0], "imgOutput");
            _samplerAreaUniform = _gd.Api.GetUniformLocation(_blendShaderPrograms[0], "samplerArea");
            _samplerSearchUniform = _gd.Api.GetUniformLocation(_blendShaderPrograms[0], "samplerSearch");
            _samplerBlendUniform = _gd.Api.GetUniformLocation(_neighbourShaderPrograms[0], "samplerBlend");
            _resolutionUniform = _gd.Api.GetUniformLocation(_edgeShaderPrograms[0], "invResolution");
        }

        private void Initialize()
        {
            var areaInfo = new TextureCreateInfo(AreaWidth,
                AreaHeight,
                1,
                1,
                1,
                1,
                1,
                1,
                Format.R8G8Unorm,
                DepthStencilMode.Depth,
                Target.Texture2D,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha);

            var searchInfo = new TextureCreateInfo(SearchWidth,
                SearchHeight,
                1,
                1,
                1,
                1,
                1,
                1,
                Format.R8Unorm,
                DepthStencilMode.Depth,
                Target.Texture2D,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha);

            _areaTexture = new TextureStorage(_gd, areaInfo);
            _searchTexture = new TextureStorage(_gd, searchInfo);

            var areaTexture = EmbeddedResources.ReadFileToRentedMemory("Ryujinx.Graphics.OpenGL/Effects/Textures/SmaaAreaTexture.bin");
            var searchTexture = EmbeddedResources.ReadFileToRentedMemory("Ryujinx.Graphics.OpenGL/Effects/Textures/SmaaSearchTexture.bin");

            var areaView = _areaTexture.CreateDefaultView();
            var searchView = _searchTexture.CreateDefaultView();

            areaView.SetData(areaTexture);
            searchView.SetData(searchTexture);
        }

        public TextureView Run(TextureView view, int width, int height)
        {
            if (_outputTexture == null || _outputTexture.Info.Width != view.Width || _outputTexture.Info.Height != view.Height)
            {
                _outputTexture?.Dispose();
                _outputTexture = new TextureStorage(_gd, view.Info);
                _outputTexture.CreateDefaultView();
                _edgeOutputTexture = new TextureStorage(_gd, view.Info);
                _edgeOutputTexture.CreateDefaultView();
                _blendOutputTexture = new TextureStorage(_gd, view.Info);
                _blendOutputTexture.CreateDefaultView();

                DeleteShaders();

                RecreateShaders(view.Width, view.Height);
            }

            var textureView = _outputTexture.CreateView(view.Info, 0, 0) as TextureView;
            var edgeOutput = _edgeOutputTexture.DefaultView as TextureView;
            var blendOutput = _blendOutputTexture.DefaultView as TextureView;
            var areaTexture = _areaTexture.DefaultView as TextureView;
            var searchTexture = _searchTexture.DefaultView as TextureView;

            uint previousFramebuffer = (uint)_gd.Api.GetInteger(GLEnum.DrawFramebufferBinding);
            int previousUnit = _gd.Api.GetInteger(GLEnum.ActiveTexture);
            _gd.Api.ActiveTexture(TextureUnit.Texture0);
            uint previousTextureBinding0 = (uint)_gd.Api.GetInteger(GLEnum.TextureBinding2D);
            _gd.Api.ActiveTexture(TextureUnit.Texture1);
            uint previousTextureBinding1 = (uint)_gd.Api.GetInteger(GLEnum.TextureBinding2D);
            _gd.Api.ActiveTexture(TextureUnit.Texture2);
            uint previousTextureBinding2 = (uint)_gd.Api.GetInteger(GLEnum.TextureBinding2D);

            var framebuffer = new Framebuffer(_gd.Api);
            framebuffer.Bind();
            framebuffer.AttachColor(0, edgeOutput);
            _gd.Api.Clear(ClearBufferMask.ColorBufferBit);
            _gd.Api.ClearColor(0, 0, 0, 0);
            framebuffer.AttachColor(0, blendOutput);
            _gd.Api.Clear(ClearBufferMask.ColorBufferBit);
            _gd.Api.ClearColor(0, 0, 0, 0);

            _gd.Api.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

            framebuffer.Dispose();

            uint dispatchX = (uint)BitUtils.DivRoundUp(view.Width, IPostProcessingEffect.LocalGroupSize);
            uint dispatchY = (uint)BitUtils.DivRoundUp(view.Height, IPostProcessingEffect.LocalGroupSize);

            uint previousProgram = (uint)_gd.Api.GetInteger(GLEnum.CurrentProgram);
            _gd.Api.BindImageTexture(0, edgeOutput.Handle, 0, false, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
            _gd.Api.UseProgram(_edgeShaderPrograms[Quality]);
            view.Bind(0);
            _gd.Api.Uniform1(_inputUniform, 0);
            _gd.Api.Uniform1(_outputUniform, 0);
            _gd.Api.Uniform2(_resolutionUniform, view.Width, view.Height);
            _gd.Api.DispatchCompute(dispatchX, dispatchY, 1);
            _gd.Api.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _gd.Api.BindImageTexture(0, blendOutput.Handle, 0, false, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
            _gd.Api.UseProgram(_blendShaderPrograms[Quality]);
            edgeOutput.Bind(0);
            areaTexture.Bind(1);
            searchTexture.Bind(2);
            _gd.Api.Uniform1(_inputUniform, 0);
            _gd.Api.Uniform1(_outputUniform, 0);
            _gd.Api.Uniform1(_samplerAreaUniform, 1);
            _gd.Api.Uniform1(_samplerSearchUniform, 2);
            _gd.Api.Uniform2(_resolutionUniform, view.Width, view.Height);
            _gd.Api.DispatchCompute(dispatchX, dispatchY, 1);
            _gd.Api.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _gd.Api.BindImageTexture(0, textureView.Handle, 0, false, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba8);
            _gd.Api.UseProgram(_neighbourShaderPrograms[Quality]);
            view.Bind(0);
            blendOutput.Bind(1);
            _gd.Api.Uniform1(_inputUniform, 0);
            _gd.Api.Uniform1(_outputUniform, 0);
            _gd.Api.Uniform1(_samplerBlendUniform, 1);
            _gd.Api.Uniform2(_resolutionUniform, view.Width, view.Height);
            _gd.Api.DispatchCompute(dispatchX, dispatchY, 1);
            _gd.Api.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            (_gd.Pipeline as Pipeline).RestoreImages1And2();

            _gd.Api.UseProgram(previousProgram);

            _gd.Api.ActiveTexture(TextureUnit.Texture0);
            _gd.Api.BindTexture(TextureTarget.Texture2D, previousTextureBinding0);
            _gd.Api.ActiveTexture(TextureUnit.Texture1);
            _gd.Api.BindTexture(TextureTarget.Texture2D, previousTextureBinding1);
            _gd.Api.ActiveTexture(TextureUnit.Texture2);
            _gd.Api.BindTexture(TextureTarget.Texture2D, previousTextureBinding2);

            _gd.Api.ActiveTexture((TextureUnit)previousUnit);

            return textureView;
        }
    }
}
