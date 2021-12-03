using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Ryujinx.Graphics.OpenGL.Helper;
using Ryujinx.Common.Osd;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.Graphics.OpenGL
{
    public class GlOsdRenderer : IDisposable, IOsdRenderer
    {
        private readonly float[] _vertices =
        {
             1f,  1f, 0.0f, 1.0f, 1.0f,
             1f, -1f, 0.0f, 1.0f, 0.0f,
            -1f, -1f, 0.0f, 0.0f, 0.0f,
            -1f, 1f, 0.0f, 0.0f, 1.0f
        };

        private readonly ushort[] _indices =
         {
            0, 1, 3,
            1, 2, 3
        };

        private int _hudFragmentShader;
        private int _hudVertexShader;
        private int _hudShaderProgram;
        private int _attribLocationTex;
        private int _attribLocationProjMtx;
        private int _attribLocationVtxPos;
        private int _attribLocationVtxUv;
        private int _attribLocationVtxColor;
        private int _elementsHandle;
        private int _vboHandle;
        private int _fontTexture;
        private ImGuiIOPtr _io;
        private int _vertexBufferObject;
        private int _elementBufferObject;
        private int _vertexArrayObject;
        private RenderTarget _renderTarget;

        public void Dispose()
        {
            if (_vboHandle != 0)
            {
                GL.DeleteBuffer(_vboHandle);
                _vboHandle = 0;
            }

            if (_elementsHandle != 0)
            {
                GL.DeleteBuffer(_elementsHandle);
                _elementsHandle = 0;
            }

            if (_hudShaderProgram != 0)
            {
                if (_hudVertexShader != 0)
                {
                    GL.DetachShader(_hudShaderProgram, _hudVertexShader);
                }

                if (_hudFragmentShader != 0)
                {
                    GL.DetachShader(_hudShaderProgram, _hudFragmentShader);
                }

            }

            if (_hudVertexShader != 0)
            {
                GL.DeleteShader(_hudVertexShader);
                _hudVertexShader = 0;
            }

            if (_hudFragmentShader != 0)
            {
                GL.DeleteShader(_hudFragmentShader);
                _hudFragmentShader = 0;
            }

            if (_hudShaderProgram != 0)
            {
                GL.DeleteProgram(_hudShaderProgram);
                _hudShaderProgram = 0;
            }
            
            if (_fontTexture != 0)
            {
                GL.DeleteTexture(_fontTexture);
                _io.Fonts.SetTexID(IntPtr.Zero);
                _fontTexture = 0;
            }

            _renderTarget.Dispose();

            if (_vertexBufferObject != 0)
            {
                GL.DeleteBuffer(_vertexBufferObject);
                _vertexBufferObject = 0;
            }

            if (_vertexArrayObject != 0)
            {
                GL.DeleteVertexArray(_vertexArrayObject);
                _vertexArrayObject = 0;
            }

            if (_elementBufferObject != 0)
            {
                GL.DeleteBuffer(_elementBufferObject);
                _elementBufferObject = 0;
            }

        }

        public void Initialize(ImGuiIOPtr io)
        {
            _io = io;
            GL.GetInteger(GetPName.Texture2D, out var lastTexture);
            var error = GL.GetError();
            GL.GetInteger(GetPName.ArrayBufferBinding, out var lastArrayBuffer);
            GL.GetInteger(GetPName.VertexArrayBinding, out var lastVertexArray);

            using (var fragmentShaderStream = LoadResource("Ryujinx.Graphics.OpenGL.Shaders.hudshader.frag"))
            {
                using (var vertexShaderStream = LoadResource("Ryujinx.Graphics.OpenGL.Shaders.hudshader.vert"))
                {
                    var buffer = new byte[fragmentShaderStream.Length];
                    fragmentShaderStream.Read(buffer);
                    string fragmentShaderSource = Encoding.ASCII.GetString(buffer);

                    buffer = new byte[vertexShaderStream.Length];
                    vertexShaderStream.Read(buffer);
                    string vertexShaderSource = Encoding.ASCII.GetString(buffer);

                    _hudFragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                    _hudVertexShader = GL.CreateShader(ShaderType.VertexShader);

                    GL.ShaderSource(_hudFragmentShader, fragmentShaderSource);
                    GL.CompileShader(_hudFragmentShader);
                    CheckShader(_hudFragmentShader);


                    GL.ShaderSource(_hudVertexShader, vertexShaderSource);
                    GL.CompileShader(_hudVertexShader);
                    CheckShader(_hudVertexShader);

                    _hudShaderProgram = GL.CreateProgram();
                    GL.AttachShader(_hudShaderProgram, _hudFragmentShader);
                    GL.AttachShader(_hudShaderProgram, _hudVertexShader);
                    GL.LinkProgram(_hudShaderProgram);
                    CheckProgram(_hudShaderProgram);

                    _attribLocationTex = GL.GetUniformLocation(_hudShaderProgram, "Texture");
                    _attribLocationProjMtx = GL.GetUniformLocation(_hudShaderProgram, "ProjMtx");
                    _attribLocationVtxPos = GL.GetAttribLocation(_hudShaderProgram, "Position");
                    _attribLocationVtxUv = GL.GetAttribLocation(_hudShaderProgram, "UV");
                    _attribLocationVtxColor = GL.GetAttribLocation(_hudShaderProgram, "Color");

                    _elementsHandle = GL.GenBuffer();
                    _vboHandle = GL.GenBuffer();

                    //Create Font
                    _io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height);

                    _fontTexture = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
                    _io.Fonts.SetTexID((IntPtr)_fontTexture);

                    UpdateRenderTarget(100, 100);

                    GL.BindTexture(TextureTarget.Texture2D, lastTexture);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, lastArrayBuffer);
                    GL.BindVertexArray(lastVertexArray);
                }
            }

            GL.LinkProgram(0);

            UpdateRenderTarget(100, 100);
        }

        public void UpdateRenderTarget(int width, int height)
        {
            _renderTarget.Dispose();

            _renderTarget = GLHelper.GenerateRenderTarget(width, height);
        }

        private void BindFramebuffer()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _renderTarget.Framebuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _renderTarget.Framebuffer);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        private void UnbindFramebuffer()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void CheckShader(int handle)
        {
            GL.GetShader(handle, ShaderParameter.CompileStatus, out var status);
            if (status != 0) return;

            GL.GetShader(handle, ShaderParameter.InfoLogLength, out var logLength);
            GL.GetShaderInfoLog(handle, logLength, out _, out var info);
            throw new Exception(info);
        }

        private void CheckProgram(int handle)
        {
            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out var status);
            if (status != 0) return;

            GL.GetProgram(handle, GetProgramParameterName.InfoLogLength, out var logLength);
            GL.GetProgramInfoLog(handle, logLength, out _, out var info);
            throw new Exception(info);
        }

        private void BindHudData(ImDrawDataPtr drawData, int fbWidth, int fbHeight, int vertexArrayObject, int texture)
        {
            BindFramebuffer();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.ScissorTest);
            GL.Disable(EnableCap.PrimitiveRestart);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Viewport(0, 0, fbWidth, fbHeight);
            var l = drawData.DisplayPos.X;
            var r = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var t = drawData.DisplayPos.Y;
            var b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            var orthoProjection = new Matrix4(
                new Vector4(2.0f / (r - l), 0.0f, 0.0f, 0.0f),
                new(0.0f, 2.0f / (t - b), 0.0f, 0.0f),
                new(0.0f, 0.0f, -1.0f, 0.0f),
                new((r + l) / (l - r), (t + b) / (b - t), 0.0f, 1.0f)
            );
            GL.UseProgram(_hudShaderProgram);
            GL.Uniform1(_attribLocationTex, 0);
            GL.UniformMatrix4(_attribLocationProjMtx, false, ref orthoProjection);

            GL.BindSampler(0, 0);

            GL.BindVertexArray(vertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementsHandle);
            GL.EnableVertexAttribArray(_attribLocationVtxPos);
            GL.EnableVertexAttribArray(_attribLocationVtxUv);
            GL.EnableVertexAttribArray(_attribLocationVtxColor);

            GL.VertexAttribPointer(_attribLocationVtxPos, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(),
                Marshal.OffsetOf<ImDrawVert>("pos"));
            GL.VertexAttribPointer(_attribLocationVtxUv, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(),
                Marshal.OffsetOf<ImDrawVert>("uv"));
            GL.VertexAttribPointer(_attribLocationVtxColor, 4, VertexAttribPointerType.UnsignedByte, true, Unsafe.SizeOf<ImDrawVert>(),
                Marshal.OffsetOf<ImDrawVert>("col"));
        }

        public void Render(ImDrawDataPtr drawData, int texture)
        {
            if (drawData.CmdListsCount == 0)
                return;


            var fbWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
            var fbHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
            if (fbWidth <= 0 || fbHeight <= 0)
                return;

            var vertexArrayObject = GL.GenVertexArray();
            BindHudData(drawData, fbWidth, fbHeight, vertexArrayObject, texture);

            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdListsRange[n];

                GL.BufferData(BufferTarget.ArrayBuffer, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(),
                    cmdList.VtxBuffer.Data, BufferUsageHint.StreamDraw);
                GL.BufferData(BufferTarget.ElementArrayBuffer, cmdList.IdxBuffer.Size * sizeof(ushort),
                    cmdList.IdxBuffer.Data, BufferUsageHint.StreamDraw);

                for (var cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
                {
                    var pcmd = cmdList.CmdBuffer[cmdI];
                    if (pcmd.UserCallback != IntPtr.Zero)
                        throw new NotImplementedException();

                    var clipRect = new Vector4
                    {
                        X = (pcmd.ClipRect.X - clipOff.X) * clipScale.X,
                        Y = (pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y,
                        Z = (pcmd.ClipRect.Z - clipOff.X) * clipScale.X,
                        W = (pcmd.ClipRect.W - clipOff.Y) * clipScale.Y
                    };

                    if (!(clipRect.X < fbWidth) || !(clipRect.Y < fbHeight) || !(clipRect.Z >= 0.0f) ||
                        !(clipRect.W >= 0.0f)) continue;

                    GL.Scissor((int)clipRect.X, (int)(fbHeight - clipRect.W), (int)(clipRect.Z - clipRect.X),
                        (int)(clipRect.W - clipRect.Y));

                    GL.BindTexture(TextureTarget.Texture2D, (uint)pcmd.TextureId);
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                }
            }

            GL.DeleteVertexArray(vertexArrayObject);

            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindSampler(0, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BlendEquationSeparate(0, 0);
            GL.BlendFuncSeparate((BlendingFactorSrc)1, 0, (BlendingFactorSrc)1, 0);

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.ScissorTest);
            GL.Enable(EnableCap.PrimitiveRestart);
            GL.Disable(EnableCap.PrimitiveRestart);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            UnbindFramebuffer();
        }

        public Stream LoadResource(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }
    }
}
