using Ryujinx.Graphics.Shader.Translation;
using System;
using System.IO;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Shader dumper, writes binary shader code to disk.
    /// </summary>
    class ShaderDumper
    {
        private const int FirstIndex = 1;

        private string _runtimeDir;
        private string _dumpPath;

        public int CurrentDumpIndex { get; private set; }

        public ShaderDumper()
        {
            CurrentDumpIndex = FirstIndex;
        }

        /// <summary>
        /// Dumps shader code to disk.
        /// </summary>
        /// <param name="code">Code to be dumped</param>
        /// <param name="compute">True for compute shader code, false for graphics shader code</param>
        /// <param name="fullPath">Output path for the shader code with header included</param>
        /// <param name="codePath">Output path for the shader code without header</param>
        public void Dump(ReadOnlySpan<byte> code, bool compute, out string fullPath, out string codePath)
        {
            _dumpPath = GraphicsConfig.ShadersDumpPath;

            if (string.IsNullOrWhiteSpace(_dumpPath))
            {
                fullPath = null;
                codePath = null;

                return;
            }

            string fileName = $"Shader{CurrentDumpIndex:d4}.bin";

            fullPath = Path.Combine(FullDir, fileName);
            codePath = Path.Combine(CodeDir, fileName);

            CurrentDumpIndex++;

            code = Translator.ExtractCode(code, compute, out int headerSize);

            using var fullWriter = new BinaryWriter(File.Create(fullPath));
            using var codeWriter = new BinaryWriter(File.Create(codePath));

            byte[] temp;
            using (var codeReader = new BinaryReader(new MemoryStream(code.ToArray())))
            {
                fullWriter.Write(codeReader.ReadBytes(headerSize));

                temp = codeReader.ReadBytes(code.Length - headerSize);
            }

            fullWriter.Write(temp);
            codeWriter.Write(temp);

            // Align to meet nvdisasm requirements.
            while (codeWriter.BaseStream.Length % 0x20 != 0)
            {
                codeWriter.Write(0);
            }
        }

        /// <summary>
        /// Returns the output directory for shader code with header.
        /// </summary>
        /// <returns>Directory path</returns>
        private string FullDir => CreateAndReturn(Path.Combine(DumpDir(), "Full"));

        /// <summary>
        /// Returns the output directory for shader code without header.
        /// </summary>
        /// <returns>Directory path</returns>
        private string CodeDir => CreateAndReturn(Path.Combine(DumpDir(), "Code"));

        /// <summary>
        /// Returns the full output directory for the current shader dump.
        /// </summary>
        /// <returns>Directory path</returns>
        private string DumpDir()
        {
            if (string.IsNullOrEmpty(_runtimeDir))
            {
                int index = FirstIndex;

                do
                {
                    _runtimeDir = Path.Combine(_dumpPath, $"Dumps{index:d2}");

                    index++;
                }
                while (Directory.Exists(_runtimeDir));

                Directory.CreateDirectory(_runtimeDir);
            }

            return _runtimeDir;
        }

        /// <summary>
        /// Creates a new specified directory if needed.
        /// </summary>
        /// <param name="dir">The directory to create</param>
        /// <returns>The same directory passed to the method</returns>
        private static string CreateAndReturn(string dir)
        {
            Directory.CreateDirectory(dir);

            return dir;
        }
    }
}