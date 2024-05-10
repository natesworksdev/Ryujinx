using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.ARB;
using System;
using System.Buffers.Binary;

namespace Ryujinx.Graphics.OpenGL
{
    class Program : IProgram
    {
        private const int MaxShaderLogLength = 2048;

        public uint Handle { get; private set; }

        public bool IsLinked
        {
            get
            {
                if (_status == ProgramLinkStatus.Incomplete)
                {
                    CheckProgramLink(true);
                }

                return _status == ProgramLinkStatus.Success;
            }
        }

        private readonly OpenGLRenderer _gd;
        private ProgramLinkStatus _status = ProgramLinkStatus.Incomplete;
        private uint[] _shaderHandles;

        public int FragmentOutputMap { get; }

        public unsafe Program(OpenGLRenderer gd, ShaderSource[] shaders, int fragmentOutputMap)
        {
            _gd = gd;
            Handle = _gd.Api.CreateProgram();

            _gd.Api.ProgramParameter(Handle, ProgramParameterPName.BinaryRetrievableHint, 1);

            _shaderHandles = new uint[shaders.Length];
            bool hasFragmentShader = false;

            for (int index = 0; index < shaders.Length; index++)
            {
                ShaderSource shader = shaders[index];

                if (shader.Stage == ShaderStage.Fragment)
                {
                    hasFragmentShader = true;
                }

                uint shaderHandle = _gd.Api.CreateShader(shader.Stage.Convert());

                switch (shader.Language)
                {
                    case TargetLanguage.Glsl:
                        _gd.Api.ShaderSource(shaderHandle, shader.Code);
                        _gd.Api.CompileShader(shaderHandle);
                        break;
                    case TargetLanguage.Spirv:
                        fixed (byte* ptr = shader.BinaryCode.AsSpan())
                        {
                            _gd.Api.ShaderBinary(1, in shaderHandle, ShaderBinaryFormat.ShaderBinaryFormatSpirV, ptr, (uint)shader.BinaryCode.Length);
                        }
                        _gd.Api.SpecializeShader(shaderHandle, "main", 0, (uint[])null, (uint[])null);
                        break;
                }

                _gd.Api.AttachShader(Handle, shaderHandle);

                _shaderHandles[index] = shaderHandle;
            }

            _gd.Api.LinkProgram(Handle);

            FragmentOutputMap = hasFragmentShader ? fragmentOutputMap : 0;
        }

        public Program(OpenGLRenderer gd, ReadOnlySpan<byte> code, bool hasFragmentShader, int fragmentOutputMap)
        {
            _gd = gd;
            Handle = _gd.Api.CreateProgram();

            if (code.Length >= 4)
            {
                ShaderBinaryFormat binaryFormat = (ShaderBinaryFormat)BinaryPrimitives.ReadInt32LittleEndian(code.Slice(code.Length - 4, 4));

                unsafe
                {
                    fixed (byte* ptr = code)
                    {
                        _gd.Api.ProgramBinary(Handle, (GLEnum)binaryFormat, (IntPtr)ptr, (uint)code.Length - 4);
                    }
                }
            }

            FragmentOutputMap = hasFragmentShader ? fragmentOutputMap : 0;
        }

        public void Bind()
        {
            _gd.Api.UseProgram(Handle);
        }

        public ProgramLinkStatus CheckProgramLink(bool blocking)
        {
            if (!blocking && _gd.Capabilities.SupportsParallelShaderCompile)
            {
                _gd.Api.GetProgram(Handle, (GLEnum)ARB.CompletionStatusArb, out int completed);

                if (completed == 0)
                {
                    return ProgramLinkStatus.Incomplete;
                }
            }

            _gd.Api.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int status);
            DeleteShaders();

            if (status == 0)
            {
                _status = ProgramLinkStatus.Failure;

                string log = _gd.Api.GetProgramInfoLog(Handle);

                if (log.Length > MaxShaderLogLength)
                {
                    log = log[..MaxShaderLogLength] + "...";
                }

                Logger.Warning?.Print(LogClass.Gpu, $"Shader linking failed: \n{log}");
            }
            else
            {
                _status = ProgramLinkStatus.Success;
            }

            return _status;
        }

        public unsafe byte[] GetBinary()
        {
            _gd.Api.GetProgram(Handle, ProgramPropertyARB.ProgramBinaryLength, out int size);

            byte[] data = new byte[size];
            GLEnum binFormat;

            fixed (byte* ptr = data)
            {
                _gd.Api.GetProgramBinary(Handle, (uint)size, out _, out binFormat, ptr);
            }

            BinaryPrimitives.WriteInt32LittleEndian(data, (int)binFormat);

            return data;
        }

        private void DeleteShaders()
        {
            if (_shaderHandles != null)
            {
                foreach (uint shaderHandle in _shaderHandles)
                {
                    _gd.Api.DetachShader(Handle, shaderHandle);
                    _gd.Api.DeleteShader(shaderHandle);
                }

                _shaderHandles = null;
            }
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                DeleteShaders();
                _gd.Api.DeleteProgram(Handle);

                Handle = 0;
            }
        }
    }
}
