using Silk.NET.OpenGL.Legacy;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
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

        private readonly GL _api;
        private ProgramLinkStatus _status = ProgramLinkStatus.Incomplete;
        private uint[] _shaderHandles;

        public int FragmentOutputMap { get; }

        public Program(GL api, ShaderSource[] shaders, int fragmentOutputMap)
        {
            _api = api;
            Handle = _api.CreateProgram();

            _api.ProgramParameter(Handle, ProgramParameterPName.BinaryRetrievableHint, 1);

            _shaderHandles = new uint[shaders.Length];
            bool hasFragmentShader = false;

            for (int index = 0; index < shaders.Length; index++)
            {
                ShaderSource shader = shaders[index];

                if (shader.Stage == ShaderStage.Fragment)
                {
                    hasFragmentShader = true;
                }

                uint shaderHandle = _api.CreateShader(shader.Stage.Convert());

                switch (shader.Language)
                {
                    case TargetLanguage.Glsl:
                        _api.ShaderSource(shaderHandle, shader.Code);
                        _api.CompileShader(shaderHandle);
                        break;
                    case TargetLanguage.Spirv:
                        _api.ShaderBinary(1, ref shaderHandle, ShaderBinaryFormat.ShaderBinaryFormatSpirV, shader.BinaryCode, shader.BinaryCode.Length);
                        _api.SpecializeShader(shaderHandle, "main", 0, (int[])null, (int[])null);
                        break;
                }

                _api.AttachShader(Handle, shaderHandle);

                _shaderHandles[index] = shaderHandle;
            }

            _api.LinkProgram(Handle);

            FragmentOutputMap = hasFragmentShader ? fragmentOutputMap : 0;
        }

        public Program(GL api, ReadOnlySpan<byte> code, bool hasFragmentShader, int fragmentOutputMap)
        {
            _api = api;
            Handle = _api.CreateProgram();

            if (code.Length >= 4)
            {
                ShaderBinaryFormat binaryFormat = (ShaderBinaryFormat)BinaryPrimitives.ReadInt32LittleEndian(code.Slice(code.Length - 4, 4));

                unsafe
                {
                    fixed (byte* ptr = code)
                    {
                        _api.ProgramBinary(Handle, (GLEnum)binaryFormat, (IntPtr)ptr, (uint)code.Length - 4);
                    }
                }
            }

            FragmentOutputMap = hasFragmentShader ? fragmentOutputMap : 0;
        }

        public void Bind()
        {
            _api.UseProgram(Handle);
        }

        public ProgramLinkStatus CheckProgramLink(bool blocking)
        {
            if (!blocking && HwCapabilities.SupportsParallelShaderCompile)
            {
                _api.GetProgram(Handle, (GetProgramParameterName)ArbParallelShaderCompile.CompletionStatusArb, out int completed);

                if (completed == 0)
                {
                    return ProgramLinkStatus.Incomplete;
                }
            }

            _api.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int status);
            DeleteShaders();

            if (status == 0)
            {
                _status = ProgramLinkStatus.Failure;

                string log = _api.GetProgramInfoLog(Handle);

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
            _api.GetProgram(Handle, ProgramPropertyARB.ProgramBinaryLength, out int size);

            Span<byte> data = stackalloc byte[size];
            GLEnum binFormat;

            fixed (byte* ptr = data)
            {
                _api.GetProgramBinary(Handle, (uint)size, out _, out binFormat, ptr);
            }

            BinaryPrimitives.WriteInt32LittleEndian(data, (int)binFormat);

            return data.ToArray();
        }

        private void DeleteShaders()
        {
            if (_shaderHandles != null)
            {
                foreach (uint shaderHandle in _shaderHandles)
                {
                    _api.DetachShader(Handle, shaderHandle);
                    _api.DeleteShader(shaderHandle);
                }

                _shaderHandles = null;
            }
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                DeleteShaders();
                _api.DeleteProgram(Handle);

                Handle = 0;
            }
        }
    }
}
