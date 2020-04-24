using System.IO;

namespace Ryujinx.Common.Utilities
{
    public static class DirectoryUtils
    {
        public static bool Delete(string path, bool recursive = false)
        {
            // TODO : We don't check if it exists first because otherwise it would not be an atomic operation.
            // However, Directory.Delete should be atomic even with 'recursive' set, so instead we just catch
            // the exception if it did not exist.
            try
            {
                Directory.Delete(path, recursive: recursive);
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }
    }
}
