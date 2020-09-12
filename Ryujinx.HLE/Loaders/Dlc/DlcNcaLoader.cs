using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.IO.Abstractions;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.Loaders.Dlc
{
    public sealed class DlcNcaLoader
    {
        private readonly string _containerPath;
        private readonly ILocalStorageManagement _localStorageManagement;
        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly string _titleId;

        public DlcNcaLoader(string titleId, string containarPath, ILocalStorageManagement localStorageManagement, VirtualFileSystem virtualFileSystem)
        {
            _titleId = titleId;
            _containerPath = containarPath;
            _localStorageManagement = localStorageManagement;
            _virtualFileSystem = virtualFileSystem;
        }

        public IEnumerable<DlcNca> Load()
        {
            var dlcNcaList = new List<DlcNca>();

            using var containerFile = _localStorageManagement.OpenRead(_containerPath);

            using var pfs = new PartitionFileSystem(containerFile.AsStorage());
            _virtualFileSystem.ImportTickets(pfs);

            foreach (var fileEntry in pfs.EnumerateEntries("/", "*.nca"))
            {
                pfs.OpenFile(out IFile ncaFile, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                if (!TryCreateNca(ncaFile.AsStorage(), _containerPath, out var nca))
                {
                    continue;
                }                    

                if (nca.Header.ContentType == NcaContentType.PublicData)
                {
                    if ((nca.Header.TitleId & 0xFFFFFFFFFFFFE000).ToString("x16") != _titleId)
                    {
                        break;
                    }                        

                    dlcNcaList.Add(new DlcNca(fileEntry.FullPath, nca.Header.TitleId, true));
                }
            }

            return dlcNcaList;
        }

        private bool TryCreateNca(IStorage ncaStorage, string containerPath, out Nca nca)
        {
            try
            {
                nca = new Nca(_virtualFileSystem.KeySet, ncaStorage);

                return true;
            }
            catch (InvalidDataException exception)
            {
                Logger.Error?.Print(LogClass.Application, $"{exception.Message}. Errored File: {containerPath}");
            }            

            nca = null;

            return false;
        }
    }
}
