using Ryujinx.Graphics.Shader;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Persistent shader cache file management.
    /// </summary>
    static class ShaderCacheFile
    {
        private const string Extension = "shbin";

        /// <summary>
        /// Loads all cached shaders from disk.
        /// </summary>
        /// <param name="basePath">Base path of the shader cache</param>
        /// <returns>Shader cache file data array</returns>
        public static ShaderCacheFileFormat[] Load(string basePath)
        {
            string[] files = Directory.GetFiles(basePath, $"*.{Extension}", SearchOption.TopDirectoryOnly);

            List<ShaderCacheFileFormat> cached = new List<ShaderCacheFileFormat>();

            foreach (string fileName in files)
            {
                using FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                BinaryFormatter formatter = new BinaryFormatter();

                ShaderCacheFileFormat scff = (ShaderCacheFileFormat)formatter.Deserialize(fs);

                cached.Add(scff);
            }

            return cached.ToArray();
        }

        /// <summary>
        /// Saves shader code to disk.
        /// </summary>
        /// <param name="basePath">Base path of the shader cache</param>
        /// <param name="info">Array of shader program information for all stages</param>
        /// <param name="pack">Pack with spans of guest shader code</param>
        /// <param name="code">Host binary shader code</param>
        /// <param name="hash">Hash calculated from the guest shader code</param>
        /// <param name="index">Index to disambiguate the shader in case of hash collisions</param>
        public static void Save(string basePath, ShaderProgramInfo[] info, ShaderPack pack, byte[] code, int hash, int index)
        {
            ShaderCacheFileFormat scff = new ShaderCacheFileFormat(hash, pack, code, info);

            BinaryFormatter formatter = new BinaryFormatter();

            string fileName = GetShaderPath(basePath, hash, index);

            using FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            formatter.Serialize(fs, scff);
        }

        /// <summary>
        /// Gets the file path for a given shader.
        /// </summary>
        /// <param name="basePath">Base path of the shader cache</param>
        /// <param name="hash">Hash calculated from the guest shader code</param>
        /// <param name="index">Index to disambiguate the shader in case of hash collisions</param>
        /// <returns>File path</returns>
        private static string GetShaderPath(string basePath, int hash, int index)
        {
            return index != 0
                ? Path.Combine(basePath, hash.ToString("X8", CultureInfo.InvariantCulture) + "_" + index.ToString(CultureInfo.InvariantCulture) + '.' + Extension)
                : Path.Combine(basePath, hash.ToString("X8", CultureInfo.InvariantCulture) + '.' + Extension);
        }
    }
}
