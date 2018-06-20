using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Gal.Shader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLShader
    {
        private enum ShadingLanguage
        {
            Unknown,
            GLSL,
            SPIRV
        }

        private abstract class ShaderStage : IDisposable
        {
            public int Handle { get; protected set; }

            public GalShaderType Type { get; private set; }

            public IEnumerable<ShaderDeclInfo> TextureUsage { get; private set; }
            public IEnumerable<ShaderDeclInfo> UniformUsage { get; private set; }

            public ShaderStage(
                GalShaderType               Type,
                IEnumerable<ShaderDeclInfo> TextureUsage,
                IEnumerable<ShaderDeclInfo> UniformUsage)
            {
                this.Type = Type;
                this.TextureUsage = TextureUsage;
                this.UniformUsage = UniformUsage;
            }

            public abstract void Compile();

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

        private class GlslStage : ShaderStage
        {
            public string Code { get; private set; }

            public GlslStage(
                GalShaderType               Type,
                string                      Code,
                IEnumerable<ShaderDeclInfo> TextureUsage,
                IEnumerable<ShaderDeclInfo> UniformUsage)
                : base(Type, TextureUsage, UniformUsage)
            {
                this.Code = Code;
            }

            public override void Compile()
            {
                if (Handle == 0)
                {
                    Handle = GL.CreateShader(OGLEnumConverter.GetShaderType(Type));

                    GL.ShaderSource(Handle, Code);
                    GL.CompileShader(Handle);

                    CheckCompilation(Handle);
                }
            }
        }

        private class SpirvStage : ShaderStage
        {
            public byte[] Bytecode { get; private set; }

            public IDictionary<string, int> Locations { get; private set; }

            public SpirvStage(
                GalShaderType               Type,
                byte[]                      Bytecode,
                IEnumerable<ShaderDeclInfo> TextureUsage,
                IEnumerable<ShaderDeclInfo> UniformUsage,
                IDictionary<string, int>    Locations)
                : base(Type, TextureUsage, UniformUsage)
            {
                this.Bytecode  = Bytecode;
                this.Locations = Locations;
            }

            public override void Compile()
            {
                if (Handle == 0)
                {
                    Handle = GL.CreateShader(OGLEnumConverter.GetShaderType(Type));

                    BinaryFormat SpirvFormat = (BinaryFormat)0x9551; 

                    GL.ShaderBinary(1, new int[]{Handle}, SpirvFormat, Bytecode, Bytecode.Length);

                    GL.SpecializeShader(Handle, "main", 0, new int[]{}, new int[]{});

                    CheckCompilation(Handle);
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

        private const int BuffersPerStage = Shader.UniformBinding.BuffersPerStage;

        private const int BufferSize = 16 * 1024; //ARB_uniform_buffer, 16 KiB

        private ShaderProgram Current;

        private ConcurrentDictionary<long, ShaderStage> Stages;

        private Dictionary<ShaderProgram, int> Programs;

        private ShadingLanguage Language = ShadingLanguage.Unknown;

        private OGLStreamBuffer[][] Buffers;

        public int CurrentProgramHandle { get; private set; }

        public OGLShader()
        {
            Stages = new ConcurrentDictionary<long, ShaderStage>();

            Programs = new Dictionary<ShaderProgram, int>();

            Buffers = new OGLStreamBuffer[5][]; //one per stage

            for (int Stage = 0; Stage < 5; Stage++)
            {
                Buffers[Stage] = new OGLStreamBuffer[BuffersPerStage];
            }
        }

        public void Prepare(bool TrySPIRV)
        {
            Console.WriteLine(GL.GetInteger(GetPName.MaxUniformBufferBindings));
            
            for (int Stage = 0; Stage < 5; Stage++)
            {
                for (int Cbuf = 0; Cbuf < BuffersPerStage; Cbuf++)
                {
                    OGLStreamBuffer Buffer = OGLStreamBuffer.Create(BufferTarget.UniformBuffer, BufferSize);

                    Buffer.Allocate();

                    Buffers[Stage][Cbuf] = Buffer;
                }
            }

            if (TrySPIRV)
            {
                if (HasSPIRV())
                {
                    Console.WriteLine("SPIR-V Shading Language");
                    Language = ShadingLanguage.SPIRV;
                }
                else
                {
                    Console.WriteLine("GLSL fallback (SPIR-V not available)");
                    Language = ShadingLanguage.GLSL;
                }
            }
            else
            {
                Language = ShadingLanguage.GLSL;
            }
        }

        public void Create(IGalMemory Memory, long Tag, GalShaderType Type)
        {
            Stages.GetOrAdd(Tag, (Key) => ShaderStageFactory(Memory, Tag, Type));
        }

        private ShaderStage ShaderStageFactory(IGalMemory Memory, long Position, GalShaderType Type)
        {
            switch (Language)
            {
                case ShadingLanguage.SPIRV:
                {
                    SpirvProgram Program = GetSpirvProgram(Memory, Position, Type);

                    return new SpirvStage(
                        Type,
                        Program.Bytecode,
                        Program.Textures,
                        Program.Uniforms,
                        Program.Locations);
                }

                case ShadingLanguage.GLSL:
                {
                    GlslProgram Program = GetGlslProgram(Memory, Position, Type);

                    return new GlslStage(
                        Type,
                        Program.Code,
                        Program.Textures,
                        Program.Uniforms);
                }
                
                default:
                    throw new InvalidOperationException();
            }
        }

        private GlslProgram GetGlslProgram(IGalMemory Memory, long Position, GalShaderType Type)
        {
            GlslDecompiler Decompiler = new GlslDecompiler();

            return Decompiler.Decompile(Memory, Position + 0x50, Type);
        }

        private SpirvProgram GetSpirvProgram(IGalMemory Memory, long Position, GalShaderType Type)
        {
            SpirvDecompiler Decompiler = new SpirvDecompiler();

            return Decompiler.Decompile(Memory, Position + 0x50, Type);
        }

        public IEnumerable<ShaderDeclInfo> GetTextureUsage(long Tag)
        {
            if (Stages.TryGetValue(Tag, out ShaderStage Stage))
            {
                return Stage.TextureUsage;
            }

            return Enumerable.Empty<ShaderDeclInfo>();
        }

        private int GetUniformLocation(string Name, ShaderStage Stage)
        {
            switch (Language)
            {
                case ShadingLanguage.SPIRV:
                {
                    SpirvStage Spirv = (SpirvStage)Stage;

                    if (Spirv.Locations.TryGetValue(Name, out int Location))
                    {
                        return Location;
                    }
                    break;
                }
                
                case ShadingLanguage.GLSL:
                {
                    return GL.GetUniformLocation(CurrentProgramHandle, Name);
                }
            }

            throw new InvalidOperationException();
        }

        private bool TrySpirvStageLocation(string Name, ShaderStage Stage, out int Location)
        {
            if (Stage == null)
            {
                Location = -1;
                return false;
            }

            SpirvStage Spirv = (SpirvStage)Stage;

            return Spirv.Locations.TryGetValue(Name, out Location);
        }

        private int GetSpirvLocation(string Name)
        {
            int Location;

            if (TrySpirvStageLocation(Name, Current.Vertex, out Location)
                || TrySpirvStageLocation(Name, Current.TessControl, out Location)
                || TrySpirvStageLocation(Name, Current.TessEvaluation, out Location)
                || TrySpirvStageLocation(Name, Current.Geometry, out Location)
                || TrySpirvStageLocation(Name, Current.Fragment, out Location))
            {
                return Location;
            }

            throw new InvalidOperationException();
        }

        private int GetUniformLocation(string Name)
        {
            switch (Language)
            {
                case ShadingLanguage.SPIRV:
                    return GetSpirvLocation(Name);

                case ShadingLanguage.GLSL:
                    return GL.GetUniformLocation(CurrentProgramHandle, Name);
            }

            throw new InvalidOperationException();
        }

        public void SetConstBuffer(long Tag, int Cbuf, byte[] Data)
        {
            BindProgram();

            if (Stages.TryGetValue(Tag, out ShaderStage Stage))
            {
                foreach (ShaderDeclInfo DeclInfo in Stage.UniformUsage.Where(x => x.Cbuf == Cbuf))
                {
                    if (Cbuf >= BuffersPerStage)
                    {
                        string Message = $"Game tried to write constant buffer #{Cbuf} but only 0-#{BuffersPerStage-1} are supported";
                        throw new NotSupportedException(Message);
                    }
                    
                    OGLStreamBuffer Buffer = Buffers[(int)Stage.Type][Cbuf];

                    int Size = Math.Min(Data.Length, BufferSize);

                    byte[] Destiny = Buffer.Map(Size);

                    Array.Copy(Data, Destiny, Size);

                    Buffer.Unmap(Size);
                }
            }
        }

        public void SetUniform1(string UniformName, int Value)
        {
            BindProgram();

            int Location = GetUniformLocation(UniformName);

            GL.Uniform1(Location, Value);
        }

        public void SetUniform2F(string UniformName, float X, float Y)
        {
            BindProgram();

            int Location = GetUniformLocation(UniformName);

            GL.Uniform2(Location, X, Y);
        }

        public void Bind(long Tag)
        {
            if (Stages.TryGetValue(Tag, out ShaderStage Stage))
            {
                Bind(Stage);
            }
        }

        private void Bind(ShaderStage Stage)
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

        public void BindProgram()
        {
            if (Current.Vertex   == null ||
                Current.Fragment == null)
            {
                return;
            }

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

                if (Language == ShadingLanguage.GLSL)
                {
                    BindUniformBlocksIfNotNull(Handle, Current.Vertex);
                    BindUniformBlocksIfNotNull(Handle, Current.TessControl);
                    BindUniformBlocksIfNotNull(Handle, Current.TessEvaluation);
                    BindUniformBlocksIfNotNull(Handle, Current.Geometry);
                    BindUniformBlocksIfNotNull(Handle, Current.Fragment);
                }

                Programs.Add(Current, Handle);
            }

            GL.UseProgram(Handle);

            //TODO: This could be done once, right?
            for (int Stage = 0; Stage < 5; Stage++)
            {
                for (int Cbuf = 0; Cbuf < BuffersPerStage; Cbuf++)
                {
                    OGLStreamBuffer Buffer = Buffers[Stage][Cbuf];

                    int Binding = Shader.UniformBinding.Get((GalShaderType)Stage, Cbuf);

                    GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Binding, Buffer.Handle);
                }
            }

            CurrentProgramHandle = Handle;
        }

        private void AttachIfNotNull(int ProgramHandle, ShaderStage Stage)
        {
            if (Stage != null)
            {
                Stage.Compile();

                GL.AttachShader(ProgramHandle, Stage.Handle);
            }
        }

        private void BindUniformBlocksIfNotNull(int ProgramHandle, ShaderStage Stage)
        {
            if (Stage != null)
            {
                foreach (ShaderDeclInfo DeclInfo in Stage.UniformUsage)
                {
                    int BlockIndex = GL.GetUniformBlockIndex(ProgramHandle, DeclInfo.Name);

                    if (BlockIndex < 0)
                    {
                        throw new InvalidOperationException();
                    }

                    int Binding = Shader.UniformBinding.Get(Stage.Type, DeclInfo.Cbuf);

                    GL.UniformBlockBinding(ProgramHandle, BlockIndex, Binding);
                }
            }
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

        private static bool HasSPIRV()
        {
            int ExtensionCount = GL.GetInteger(GetPName.NumExtensions);

            for (int i = 0; i < ExtensionCount; i++)
            {
                if (GL.GetString(StringNameIndexed.Extensions, i) == "GL_ARB_gl_spirv")
                {
                    return true;
                }
            }

            return false;
        }
    }
}