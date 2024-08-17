using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Utilities;
using Ryujinx.UI.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;

namespace Ryujinx.UI.Common.Helper
{
    public static class DownloadableContentsHelper
    {
        private static readonly DownloadableContentJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());
        
        public static List<(DownloadableContentModel, bool IsEnabled)> LoadSavedDownloadableContents(VirtualFileSystem vfs, ulong applicationIdBase)
        {
            var downloadableContentJsonPath = Path.Combine(AppDataManager.GamesDirPath, applicationIdBase.ToString("X16"), "dlc.json");

            if (!File.Exists(downloadableContentJsonPath))
            {
                return [];
            }

            try
            {
                var downloadableContentContainerList = JsonHelper.DeserializeFromFile(downloadableContentJsonPath,
                    _serializerContext.ListDownloadableContentContainer);
                return LoadDownloadableContents(vfs, downloadableContentContainerList);
            }
            catch
            {
                Logger.Error?.Print(LogClass.Configuration, "Downloadable Content JSON failed to deserialize.");
                return [];
            }
        }

        private static List<(DownloadableContentModel, bool IsEnabled)> LoadDownloadableContents(VirtualFileSystem vfs, List<DownloadableContentContainer> downloadableContentContainers)
        {
            var result = new List<(DownloadableContentModel, bool IsEnabled)>();
            
            foreach (DownloadableContentContainer downloadableContentContainer in downloadableContentContainers)
            {
                if (!File.Exists(downloadableContentContainer.ContainerPath))
                {
                    continue;
                }

                using IFileSystem partitionFileSystem = PartitionFileSystemUtils.OpenApplicationFileSystem(downloadableContentContainer.ContainerPath, vfs);

                foreach (DownloadableContentNca downloadableContentNca in downloadableContentContainer.DownloadableContentNcaList)
                {
                    using UniqueRef<IFile> ncaFile = new();

                    partitionFileSystem.OpenFile(ref ncaFile.Ref, downloadableContentNca.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                    // Nca nca = TryOpenNca(vfs, ncaFile.Get.AsStorage(), downloadableContentContainer.ContainerPath);
                    Nca nca = TryOpenNca(vfs, ncaFile.Get.AsStorage());
                    if (nca == null)
                    {
                        // result.Add((content, downloadableContentNca.Enabled));
                        continue;
                    }

                    var content = new DownloadableContentModel(nca.Header.TitleId,
                        downloadableContentContainer.ContainerPath,
                        downloadableContentNca.FullPath);

                    result.Add((content, downloadableContentNca.Enabled));

                    // if (downloadableContentNca.Enabled)
                    // {
                    //     SelectedDownloadableContents.Add(content);
                    // }

                    // OnPropertyChanged(nameof(UpdateCount));
                }
            }

            return result;
        }
        
        private static Nca TryOpenNca(VirtualFileSystem vfs, IStorage ncaStorage)
        {
            try
            {
                return new Nca(vfs.KeySet, ncaStorage);
            }
            catch (Exception ex)
            {
                // Dispatcher.UIThread.InvokeAsync(async () =>
                // {
                //     await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance[LocaleKeys.DialogLoadFileErrorMessage], ex.Message, containerPath));
                // });
            }

            return null;
        }
    }
}
