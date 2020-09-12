using Ryujinx.Common.IO.Abstractions;
using System.IO;

namespace Ryujinx.Common.IO
{
    public sealed class LocalStorageManagement : ILocalStorageManagement
    {
        public bool Exists(string filePath) => File.Exists(filePath);

        public Stream OpenRead(string filePath) => File.OpenRead(filePath);
    }
}
