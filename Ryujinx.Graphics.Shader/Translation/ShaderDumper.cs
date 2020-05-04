using Ryujinx.Graphics.Shader.Translation;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.IO;

namespace Ryujinx.Graphics.Shader.Translation
{
    /// <summary>
    /// Shader dumper, writes binary shader code to disk.
    /// </summary>
    public class ShaderDumper
    {
        public static string ShadersDumpPath = null;

        private string _runtimeDir;
        private int    _dumpIndex;

        public int CurrentDumpIndex => _dumpIndex;

        public ShaderDumper()
        {
            _dumpIndex = 0;
        }

        public void BeginTranslation()
        {
            _dumpIndex++;
        }

        /// <summary>
        /// Dumps shader code to disk.
        /// </summary>
        /// <param name="code">Code to be dumped</param>
        /// <param name="compute">True for compute shader code, false for graphics shader code</param>
        /// <param name="combineIndex">0 if not a combined shader, 1 or 2 if combined.</param>
        public void DumpStage0(ReadOnlySpan<byte> code, bool compute, int combineIndex)
        {
            if (!IsEnabled())
            {
                return;
            }

            string fullPath = GetPath("Stage0_BinaryFull", ".bin");
            string codePath = GetPath("Stage0_BinaryCode", ".bin");

            code = Translator.ExtractCode(code, compute, out int headerSize);

            using (MemoryStream stream = new MemoryStream(code.ToArray()))
            {
                BinaryReader codeReader = new BinaryReader(stream);

                using (FileStream fullFile = File.Create(fullPath))
                using (FileStream codeFile = File.Create(codePath))
                {
                    BinaryWriter fullWriter = new BinaryWriter(fullFile);
                    BinaryWriter codeWriter = new BinaryWriter(codeFile);

                    fullWriter.Write(codeReader.ReadBytes(headerSize));

                    byte[] temp = codeReader.ReadBytes(code.Length - headerSize);

                    fullWriter.Write(temp);
                    codeWriter.Write(temp);

                    // Align to meet nvdisasm requirements.
                    while (codeFile.Length % 0x20 != 0)
                    {
                        codeWriter.Write(0);
                    }
                }
            }
        }

        public void Dump(string stageName, string dump)
        {
            if (!IsEnabled())
            {
                return;
            }

            string path = GetPath(stageName, ".txt");

            using (FileStream file = File.Create(path))
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(dump);
            }
        }

        private bool IsEnabled()
        {
            return !string.IsNullOrWhiteSpace(ShadersDumpPath);
        }

        /// <Summary>
        /// Returns the output file path for a shader stage.
        /// </summary>
        /// <returns>Directory path</returns>
        private string GetPath(string dumpType, string fileExt, string extraInfo = "")
        {
            string dir = Path.Combine(DumpDir(), dumpType);

            Directory.CreateDirectory(dir);

            string fileName = "Shader" + _dumpIndex.ToString("d4") + extraInfo + fileExt;

            return Path.Combine(dir, fileName);
        }

        /// <summary>
        /// Returns the full output directory for the current shader dump.
        /// </summary>
        /// <returns>Directory path</returns>
        private string DumpDir()
        {
            if (string.IsNullOrEmpty(_runtimeDir))
            {
                int index = 1;

                do
                {
                    _runtimeDir = Path.Combine(ShadersDumpPath, "Dumps" + index.ToString("d2"));

                    index++;
                }
                while (Directory.Exists(_runtimeDir));

                Directory.CreateDirectory(_runtimeDir);
            }

            return _runtimeDir;
        }
    }
}
