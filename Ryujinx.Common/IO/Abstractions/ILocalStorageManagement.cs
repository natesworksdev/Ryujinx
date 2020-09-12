using System.IO;

namespace Ryujinx.Common.IO.Abstractions
{
    public interface ILocalStorageManagement
    {
        Stream OpenRead(string filePath);
        bool Exists(string filePath);
    }
}
