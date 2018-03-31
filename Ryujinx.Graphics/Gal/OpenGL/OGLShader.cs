using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Gal.Shader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLShader
    {
        private class ShaderStage : IDisposable
        {
            public int Handle { get; private set; }

            public GalShaderType Type { get; private set; }

            public ICollection<ShaderDeclInfo> TextureUsage { get; private set; }
            public ICollection<ShaderDeclInfo> UniformUsage { get; private set; }

            public ShaderStage(
                GalShaderType               Type,
                ICollection<ShaderDeclInfo> TextureUsage,
                ICollection<ShaderDeclInfo> UniformUsage)
            {
                Handle = GL.CreateShader(OGLEnumConverter.GetShaderType(Type));

                this.Type         = Type;
                this.TextureUsage = TextureUsage;
                this.UniformUsage = UniformUsage;
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool Disposing)
            {
                if (Disposing && Handle != 0)
                {
                    GL.DeleteShader(Handle);

                    Handle = 0;
                }
            }
        }

        private struct ShaderProgram
        {
            public ShaderStage Vertex;
            public ShaderStage TessControl;
            public ShaderStage TessEvaluation;
            public ShaderStage Geometry;
            public ShaderStage Fragment;
        }

        private ShaderProgram Current;

        private Dictionary<long, ShaderStage> Stages;

        private Dictionary<ShaderProgram, int> Programs;

        public OGLShader()
        {
            Stages = new Dictionary<long, ShaderStage>();

            Programs = new Dictionary<ShaderProgram, int>();
        }

        public void Create(long Tag, byte[] Data, GalShaderType Type)
        {
            if (!Stages.ContainsKey(Tag))
            {
                GlslProgram Program = GetGlslProgram(Data, Type);

                System.Console.WriteLine(Program.Code);

                ShaderStage Stage = new ShaderStage(
                    Type,
                    Program.Textures,
                    Program.Uniforms);

                Stages.Add(Tag, Stage);

                CompileAndCheck(Stage.Handle, Program.Code);
            }
        }

        public void SetConstBuffer(long Tag, int Cbuf, byte[] Data)
        {
            if (Stages.TryGetValue(Tag, out ShaderStage Stage))
            {
                foreach (ShaderDeclInfo DeclInfo in Stage.UniformUsage.Where(x => x.Cbuf == Cbuf))
                {
                    float Value = BitConverter.ToSingle(Data, DeclInfo.Index * 4);
                }
            }
        }

        public void Bind(long Tag)
        {
            if (Stages.TryGetValue(Tag, out ShaderStage Stage))
            {
                switch (Stage.Type)
                {
                    case GalShaderType.Vertex:         Current.Vertex         = Stage; break;
                    case GalShaderType.TessControl:    Current.TessControl    = Stage; break;
                    case GalShaderType.TessEvaluation: Current.TessEvaluation = Stage; break;
                    case GalShaderType.Geometry:       Current.Geometry       = Stage; break;
                    case GalShaderType.Fragment:       Current.Fragment       = Stage; break;
                }
            }
        }

        public bool BindCurrentProgram()
        {
            if (Current.Vertex   == null ||
                Current.Fragment == null)
            {
                return false;
            }

            GL.UseProgram(GetCurrentProgramHandle());

            return true;
        }

        private int GetCurrentProgramHandle()
        {
            if (!Programs.TryGetValue(Current, out int Handle))
            {
                Handle = GL.CreateProgram();

                AttachIfNotNull(Handle, Current.Vertex);
                AttachIfNotNull(Handle, Current.TessControl);
                AttachIfNotNull(Handle, Current.TessEvaluation);
                AttachIfNotNull(Handle, Current.Geometry);
                AttachIfNotNull(Handle, Current.Fragment);

                GL.LinkProgram(Handle);

                CheckProgramLink(Handle);

                Programs.Add(Current, Handle);
            }

            return Handle;
        }

        private void AttachIfNotNull(int ProgramHandle, ShaderStage Stage)
        {
            if (Stage != null)
            {
                GL.AttachShader(ProgramHandle, Stage.Handle);
            }
        }

        private GlslProgram GetGlslProgram(byte[] Data, GalShaderType Type)
        {
            int[] Code = new int[(Data.Length - 0x50) >> 2];

            using (MemoryStream MS = new MemoryStream(Data))
            {
                MS.Seek(0x50, SeekOrigin.Begin);

                BinaryReader Reader = new BinaryReader(MS);

                for (int Index = 0; Index < Code.Length; Index++)
                {
                    Code[Index] = Reader.ReadInt32();
                }
            }

            GlslDecompiler Decompiler = new GlslDecompiler();

            return Decompiler.Decompile(Code, Type);
        }

        private static void CompileAndCheck(int Handle, string Code)
        {
            GL.ShaderSource(Handle, Code);
            GL.CompileShader(Handle);

            CheckCompilation(Handle);
        }

        private static void CheckCompilation(int Handle)
        {
            int Status = 0;

            GL.GetShader(Handle, ShaderParameter.CompileStatus, out Status);

            if (Status == 0)
            {
                throw new ShaderException(GL.GetShaderInfoLog(Handle));
            }
        }

        private static void CheckProgramLink(int Handle)
        {
            int Status = 0;

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out Status);

            if (Status == 0)
            {
                throw new ShaderException(GL.GetProgramInfoLog(Handle));
            }
        }
    }
}