using System.IO;

namespace Ryujinx.Common.Utilities
{
    public static class FileUtils
    {
        public static bool Delete(string path)
        {
            // TODO : We don't check if it exists first because otherwise it would not be an atomic operation.
            try
            {
                File.Delete(path);
                return true;
            }
            catch (DirectoryNotFoundException) // File.Delete can indeed throw this.
            {
                return false;
            }
        }
    }
}
