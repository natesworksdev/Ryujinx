using Silk.NET.OpenGL;
using Ryujinx.Graphics.OpenGL.Image;
using System;
using Sampler = Ryujinx.Graphics.OpenGL.Image.Sampler;

namespace Ryujinx.Graphics.OpenGL
{
    class DrawTextureEmulation
    {
        private const string VertexShader = @"#version 430 core

uniform float srcX0;
uniform float srcY0;
uniform float srcX1;
uniform float srcY1;

layout (location = 0) out vec2 texcoord;

void main()
{
    bool x1 = (gl_VertexID & 1) != 0;
    bool y1 = (gl_VertexID & 2) != 0;
    gl_Position = vec4(x1 ? 1 : -1, y1 ? -1 : 1, 0, 1);
    texcoord = vec2(x1 ? srcX1 : srcX0, y1 ? srcY1 : srcY0);
}";

        private const string FragmentShader = @"#version 430 core

layout (location = 0) uniform sampler2D tex;

layout (location = 0) in vec2 texcoord;
layout (location = 0) out vec4 colour;

void main()
{
    colour = texture(tex, texcoord);
}";

        private uint _vsHandle;
        private uint _fsHandle;
        private uint _programHandle;
        private int _uniformSrcX0Location;
        private int _uniformSrcY0Location;
        private int _uniformSrcX1Location;
        private int _uniformSrcY1Location;
        private bool _initialized;

        public void Draw(
            GL api,
            TextureView texture,
            Sampler sampler,
            float x0,
            float y0,
            float x1,
            float y1,
            float s0,
            float t0,
            float s1,
            float t1)
        {
            EnsureInitialized(api);

            api.UseProgram(_programHandle);

            texture.Bind(0);
            sampler.Bind(0);

            if (x0 > x1)
            {
                (s1, s0) = (s0, s1);
            }

            if (y0 > y1)
            {
                (t1, t0) = (t0, t1);
            }

            api.Uniform1(_uniformSrcX0Location, s0);
            api.Uniform1(_uniformSrcY0Location, t0);
            api.Uniform1(_uniformSrcX1Location, s1);
            api.Uniform1(_uniformSrcY1Location, t1);

            api.ViewportIndexed(0, MathF.Min(x0, x1), MathF.Min(y0, y1), MathF.Abs(x1 - x0), MathF.Abs(y1 - y0));

            api.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        private void EnsureInitialized(GL api)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            _vsHandle = api.CreateShader(ShaderType.VertexShader);
            _fsHandle = api.CreateShader(ShaderType.FragmentShader);

            api.ShaderSource(_vsHandle, VertexShader);
            api.ShaderSource(_fsHandle, FragmentShader);

            api.CompileShader(_vsHandle);
            api.CompileShader(_fsHandle);

            _programHandle = api.CreateProgram();

            api.AttachShader(_programHandle, _vsHandle);
            api.AttachShader(_programHandle, _fsHandle);

            api.LinkProgram(_programHandle);

            api.DetachShader(_programHandle, _vsHandle);
            api.DetachShader(_programHandle, _fsHandle);

            _uniformSrcX0Location = api.GetUniformLocation(_programHandle, "srcX0");
            _uniformSrcY0Location = api.GetUniformLocation(_programHandle, "srcY0");
            _uniformSrcX1Location = api.GetUniformLocation(_programHandle, "srcX1");
            _uniformSrcY1Location = api.GetUniformLocation(_programHandle, "srcY1");
        }

        public void Dispose(GL api)
        {
            if (!_initialized)
            {
                return;
            }

            api.DeleteShader(_vsHandle);
            api.DeleteShader(_fsHandle);
            api.DeleteProgram(_programHandle);

            _initialized = false;
        }
    }
}
