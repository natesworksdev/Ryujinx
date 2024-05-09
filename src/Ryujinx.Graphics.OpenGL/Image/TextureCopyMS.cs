using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureCopyMS
    {
        private const string ComputeShaderMSToNonMS = @"#version 450 core

layout (binding = 0, $FORMAT$) uniform uimage2DMS imgIn;
layout (binding = 1, $FORMAT$) uniform uimage2D imgOut;

layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in;

void main()
{
    uvec2 coords = gl_GlobalInvocationID.xy;
    ivec2 imageSz = imageSize(imgOut);
    if (int(coords.x) >= imageSz.x || int(coords.y) >= imageSz.y)
    {
        return;
    }
    int inSamples = imageSamples(imgIn);
    int samplesInXLog2 = 0;
    int samplesInYLog2 = 0;
    switch (inSamples)
    {
        case 2:
            samplesInXLog2 = 1;
            break;
        case 4:
            samplesInXLog2 = 1;
            samplesInYLog2 = 1;
            break;
        case 8:
            samplesInXLog2 = 2;
            samplesInYLog2 = 1;
            break;
        case 16:
            samplesInXLog2 = 2;
            samplesInYLog2 = 2;
            break;
    }
    int samplesInX = 1 << samplesInXLog2;
    int samplesInY = 1 << samplesInYLog2;
    int sampleIdx = (int(coords.x) & (samplesInX - 1)) | ((int(coords.y) & (samplesInY - 1)) << samplesInXLog2);
    uvec4 value = imageLoad(imgIn, ivec2(int(coords.x) >> samplesInXLog2, int(coords.y) >> samplesInYLog2), sampleIdx);
    imageStore(imgOut, ivec2(coords), value);
}";

        private const string ComputeShaderNonMSToMS = @"#version 450 core

layout (binding = 0, $FORMAT$) uniform uimage2D imgIn;
layout (binding = 1, $FORMAT$) uniform uimage2DMS imgOut;

layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in;

void main()
{
    uvec2 coords = gl_GlobalInvocationID.xy;
    ivec2 imageSz = imageSize(imgIn);
    if (int(coords.x) >= imageSz.x || int(coords.y) >= imageSz.y)
    {
        return;
    }
    int outSamples = imageSamples(imgOut);
    int samplesInXLog2 = 0;
    int samplesInYLog2 = 0;
    switch (outSamples)
    {
        case 2:
            samplesInXLog2 = 1;
            break;
        case 4:
            samplesInXLog2 = 1;
            samplesInYLog2 = 1;
            break;
        case 8:
            samplesInXLog2 = 2;
            samplesInYLog2 = 1;
            break;
        case 16:
            samplesInXLog2 = 2;
            samplesInYLog2 = 2;
            break;
    }
    int samplesInX = 1 << samplesInXLog2;
    int samplesInY = 1 << samplesInYLog2;
    int sampleIdx = (int(coords.x) & (samplesInX - 1)) | ((int(coords.y) & (samplesInY - 1)) << samplesInXLog2);
    uvec4 value = imageLoad(imgIn, ivec2(coords));
    imageStore(imgOut, ivec2(int(coords.x) >> samplesInXLog2, int(coords.y) >> samplesInYLog2), sampleIdx, value);
}";

        private readonly GL _api;
        private readonly OpenGLRenderer _renderer;
        private readonly uint[] _msToNonMSProgramHandles;
        private readonly uint[] _nonMSToMSProgramHandles;

        public TextureCopyMS(GL api, OpenGLRenderer renderer)
        {
            _api = api;
            _renderer = renderer;
            _msToNonMSProgramHandles = new uint[5];
            _nonMSToMSProgramHandles = new uint[5];
        }

        public void CopyMSToNonMS(ITextureInfo src, ITextureInfo dst, int srcLayer, int dstLayer, int depth)
        {
            TextureCreateInfo srcInfo = src.Info;
            TextureCreateInfo dstInfo = dst.Info;

            uint srcHandle = CreateViewIfNeeded(src);
            uint dstHandle = CreateViewIfNeeded(dst);

            uint dstWidth = (uint)dstInfo.Width;
            uint dstHeight = (uint)dstInfo.Height;

            _api.UseProgram(GetMSToNonMSShader(srcInfo.BytesPerPixel));

            for (int z = 0; z < depth; z++)
            {
                _api.BindImageTexture(0, srcHandle, 0, false, srcLayer + z, BufferAccessARB.ReadOnly, (InternalFormat)GetFormat(srcInfo.BytesPerPixel));
                _api.BindImageTexture(1, dstHandle, 0, false, dstLayer + z, BufferAccessARB.WriteOnly, (InternalFormat)GetFormat(dstInfo.BytesPerPixel));

                _api.DispatchCompute((dstWidth + 31) / 32, (dstHeight + 31) / 32, 1);
            }

            Pipeline pipeline = (Pipeline)_renderer.Pipeline;

            pipeline.RestoreProgram();
            pipeline.RestoreImages1And2();

            DestroyViewIfNeeded(src, srcHandle);
            DestroyViewIfNeeded(dst, dstHandle);
        }

        public void CopyNonMSToMS(ITextureInfo src, ITextureInfo dst, int srcLayer, int dstLayer, int depth)
        {
            TextureCreateInfo srcInfo = src.Info;
            TextureCreateInfo dstInfo = dst.Info;

            uint srcHandle = CreateViewIfNeeded(src);
            uint dstHandle = CreateViewIfNeeded(dst);

            uint srcWidth = (uint)srcInfo.Width;
            uint srcHeight = (uint)srcInfo.Height;

            _api.UseProgram(GetNonMSToMSShader(srcInfo.BytesPerPixel));

            for (int z = 0; z < depth; z++)
            {
                _api.BindImageTexture(0, srcHandle, 0, false, srcLayer + z, BufferAccessARB.ReadOnly, (InternalFormat)GetFormat(srcInfo.BytesPerPixel));
                _api.BindImageTexture(1, dstHandle, 0, false, dstLayer + z, BufferAccessARB.WriteOnly, (InternalFormat)GetFormat(dstInfo.BytesPerPixel));

                _api.DispatchCompute((srcWidth + 31) / 32, (srcHeight + 31) / 32, 1);
            }

            Pipeline pipeline = (Pipeline)_renderer.Pipeline;

            pipeline.RestoreProgram();
            pipeline.RestoreImages1And2();

            DestroyViewIfNeeded(src, srcHandle);
            DestroyViewIfNeeded(dst, dstHandle);
        }

        private static SizedInternalFormat GetFormat(int bytesPerPixel)
        {
            return bytesPerPixel switch
            {
                1 => SizedInternalFormat.R8ui,
                2 => SizedInternalFormat.R16ui,
                4 => SizedInternalFormat.R32ui,
                8 => SizedInternalFormat.RG32ui,
                16 => SizedInternalFormat.Rgba32ui,
                _ => throw new ArgumentException($"Invalid bytes per pixel {bytesPerPixel}."),
            };
        }

        private uint CreateViewIfNeeded(ITextureInfo texture)
        {
            // Binding sRGB textures as images doesn't work on NVIDIA,
            // we need to create and bind a RGBA view for it to work.
            if (texture.Info.Format == Format.R8G8B8A8Srgb)
            {
                uint handle = _api.GenTexture();

                _api.TextureView(
                    handle,
                    texture.Info.Target.Convert(),
                    texture.Storage.Handle,
                    SizedInternalFormat.Rgba8,
                    texture.FirstLevel,
                    1,
                    texture.FirstLayer,
                    (uint)texture.Info.GetLayers());

                return handle;
            }

            return texture.Handle;
        }

        private void DestroyViewIfNeeded(ITextureInfo info, uint handle)
        {
            if (info.Handle != handle)
            {
                _api.DeleteTexture(handle);
            }
        }

        private uint GetMSToNonMSShader(int bytesPerPixel)
        {
            return GetShader(ComputeShaderMSToNonMS, _msToNonMSProgramHandles, bytesPerPixel);
        }

        private uint GetNonMSToMSShader(int bytesPerPixel)
        {
            return GetShader(ComputeShaderNonMSToMS, _nonMSToMSProgramHandles, bytesPerPixel);
        }

        private uint GetShader(string code, uint[] programHandles, int bytesPerPixel)
        {
            int index = BitOperations.Log2((uint)bytesPerPixel);

            if (programHandles[index] == 0)
            {
                uint csHandle = _api.CreateShader(ShaderType.ComputeShader);

                string format = new[] { "r8ui", "r16ui", "r32ui", "rg32ui", "rgba32ui" }[index];

                _api.ShaderSource(csHandle, code.Replace("$FORMAT$", format));
                _api.CompileShader(csHandle);

                uint programHandle = _api.CreateProgram();

                _api.AttachShader(programHandle, csHandle);
                _api.LinkProgram(programHandle);
                _api.DetachShader(programHandle, csHandle);
                _api.DeleteShader(csHandle);

                _api.GetProgram(programHandle, ProgramPropertyARB.LinkStatus, out int status);

                if (status == 0)
                {
                    throw new Exception(_api.GetProgramInfoLog(programHandle));
                }

                programHandles[index] = programHandle;
            }

            return programHandles[index];
        }

        public void Dispose()
        {
            for (int i = 0; i < _msToNonMSProgramHandles.Length; i++)
            {
                if (_msToNonMSProgramHandles[i] != 0)
                {
                    _api.DeleteProgram(_msToNonMSProgramHandles[i]);
                    _msToNonMSProgramHandles[i] = 0;
                }
            }

            for (int i = 0; i < _nonMSToMSProgramHandles.Length; i++)
            {
                if (_nonMSToMSProgramHandles[i] != 0)
                {
                    _api.DeleteProgram(_nonMSToMSProgramHandles[i]);
                    _nonMSToMSProgramHandles[i] = 0;
                }
            }
        }
    }
}
