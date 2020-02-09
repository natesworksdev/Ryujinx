using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using System;
using System.Buffers.Binary;

namespace Ryujinx.Graphics.OpenGL
{
    class Program : IProgram
    {
        public int Handle { get; private set; }

        public bool IsLinked { get; private set; }

        public Program(IShader[] shaders)
        {
            Handle = GL.CreateProgram();

            GL.ProgramParameter(Handle, ProgramParameterName.ProgramBinaryRetrievableHint, 1);

            for (int index = 0; index < shaders.Length; index++)
            {
                int shaderHandle = ((Shader)shaders[index]).Handle;

                GL.AttachShader(Handle, shaderHandle);
            }

            GL.LinkProgram(Handle);

            for (int index = 0; index < shaders.Length; index++)
            {
                int shaderHandle = ((Shader)shaders[index]).Handle;

                GL.DetachShader(Handle, shaderHandle);
            }

            CheckProgramLink();

            Bind();
        }

        public Program(ReadOnlySpan<byte> code)
        {
            BinaryFormat binFormat = (BinaryFormat)BinaryPrimitives.ReadInt32LittleEndian(code.Slice(code.Length - 4, 4));

            Handle = GL.CreateProgram();

            unsafe
            {
                fixed (byte* ptr = code)
                {
                    GL.ProgramBinary(Handle, binFormat, (IntPtr)ptr, code.Length - 4);
                }
            }

            CheckProgramLink();

            Bind();
        }

        public void SetUniformBufferBindingPoint(string name, int bindingPoint)
        {
            int location = GL.GetUniformBlockIndex(Handle, name);

            if (location < 0)
            {
                return;
            }

            GL.UniformBlockBinding(Handle, location, bindingPoint);
        }

        public void SetStorageBufferBindingPoint(string name, int bindingPoint)
        {
            int location = GL.GetProgramResourceIndex(Handle, ProgramInterface.ShaderStorageBlock, name);

            if (location < 0)
            {
                return;
            }

            GL.ShaderStorageBlockBinding(Handle, location, bindingPoint);
        }

        public void SetTextureUnit(string name, int unit) => SetTextureOrImageUnit(name, unit);
        public void SetImageUnit(string name, int unit) => SetTextureOrImageUnit(name, unit);

        public void SetTextureOrImageUnit(string name, int unit)
        {
            int location = GL.GetUniformLocation(Handle, name);

            if (location < 0)
            {
                return;
            }

            GL.Uniform1(location, unit);
        }

        public void Bind()
        {
            GL.UseProgram(Handle);
        }

        private void CheckProgramLink()
        {
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);

            if (status == 0)
            {
                // Use GL.GetProgramInfoLog(Handle), it may be too long to print on the log.
                Logger.PrintDebug(LogClass.Gpu, "Shader linking failed.");
            }
            else
            {
                IsLinked = true;
            }
        }

        public byte[] GetGpuBinary()
        {
            GL.GetProgram(Handle, (GetProgramParameterName)All.ProgramBinaryLength, out int size);

            byte[] data = new byte[size + 4];

            Span<byte> dataSpan = data;

            GL.GetProgramBinary(Handle, size, out _, out BinaryFormat binFormat, data);

            BinaryPrimitives.WriteInt32LittleEndian(dataSpan.Slice(size, 4), (int)binFormat);

            return data;
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);

                Handle = 0;
            }
        }
    }
}
