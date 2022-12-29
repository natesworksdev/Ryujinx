using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class AutoDownloadableContentLoader
    {
        public readonly List<ApplicationData> Applications;
        public AvaloniaList<DownloadableContentModel> DownloadableContents { get; private set; }
        private readonly VirtualFileSystem FileSystem;

        public AutoDownloadableContentLoader(Collection<ApplicationData> applications, VirtualFileSystem virtualFileSystem)
        {
            Applications = applications.ToList();
            FileSystem = virtualFileSystem;
        }

        public async Task AutoLoadDlcsAsync(ApplicationData application, Dictionary<string, string> dlcPathAndGameNames)
        {
            DownloadableContents = new AvaloniaList<DownloadableContentModel>();

            char[] bannedSymbols = { '.', ',', ':', ';', '>', '<', '\'', '\"', };
            string gameTitle = string.Join("", application.TitleName.Split(bannedSymbols)).ToLower().Trim();


            //Loops through the Dlcs to the given gameTitle and adds them to the downloadableContent List
            dlcPathAndGameNames.Where(titleDlc => titleDlc.Value.ToLower() == gameTitle)
                .ToList()
                .ForEach(async dlc => await AddDownloadableContent(dlc.Key, application));


            List<DownloadableContentContainer> downloadableContentContainers = new List<DownloadableContentContainer>();
            string jsonPath = LoadJsonFromTitle(application, downloadableContentContainers);
            Save(jsonPath, downloadableContentContainers);
        }


        private async Task AddDownloadableContent(string path, ApplicationData applicationData)
        {
            if (!File.Exists(path) || DownloadableContents.FirstOrDefault(x => x.ContainerPath == path) != null)
            {
                return;
            }

            using FileStream containerFile = File.OpenRead(path);

            PartitionFileSystem partitionFileSystem = new(containerFile.AsStorage());
            bool containsDownloadableContent = false;

            FileSystem.ImportTickets(partitionFileSystem);

            foreach (DirectoryEntryEx fileEntry in partitionFileSystem.EnumerateEntries("/", "*.nca"))
            {
                using var ncaFile = new UniqueRef<IFile>();

                partitionFileSystem.OpenFile(ref ncaFile.Ref(), fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                Nca nca = TryOpenNca(ncaFile.Get.AsStorage(), path);
                if (nca == null)
                {
                    continue;
                }

                if (nca.Header.ContentType == NcaContentType.PublicData)
                {
                    if ((nca.Header.TitleId & 0xFFFFFFFFFFFFE000) != ulong.Parse(applicationData.TitleId, NumberStyles.HexNumber))
                    {
                        break;
                    }

                    DownloadableContents.Add(new DownloadableContentModel(nca.Header.TitleId.ToString("X16"), path, fileEntry.FullPath, true));

                    containsDownloadableContent = true;
                }
            }

            if (!containsDownloadableContent)
            {
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogDlcNoDlcErrorMessage"]);
            }
        }


        private Nca TryOpenNca(IStorage ncaStorage, string containerPath)
        {
            try
            {
                return new Nca(FileSystem.KeySet, ncaStorage);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance["DialogDlcLoadNcaErrorMessage"], ex.Message, containerPath));
                });
            }

            return null;
        }


        private string LoadJsonFromTitle(ApplicationData applicationdata, List<DownloadableContentContainer> _downloadableContentContainerList)
        {
            ulong titleId = ulong.Parse(applicationdata.TitleId, NumberStyles.HexNumber);

            string _downloadableContentJsonPath = Path.Combine(AppDataManager.GamesDirPath, titleId.ToString("x16"), "dlc.json");

            try
            {
                _downloadableContentContainerList = JsonHelper.DeserializeFromFile<List<DownloadableContentContainer>>(_downloadableContentJsonPath);
            }
            catch
            {
                _downloadableContentContainerList = new List<DownloadableContentContainer>();
            }

            return _downloadableContentJsonPath;
        }


        public void Save(string _downloadableContentJsonPath, List<DownloadableContentContainer> _downloadableContentContainerList)
        {
            _downloadableContentContainerList.Clear();

            DownloadableContentContainer container = default;

            foreach (DownloadableContentModel downloadableContent in DownloadableContents)
            {
                if (container.ContainerPath != downloadableContent.ContainerPath)
                {
                    if (!string.IsNullOrWhiteSpace(container.ContainerPath))
                    {
                        _downloadableContentContainerList.Add(container);
                    }

                    container = new DownloadableContentContainer
                    {
                        ContainerPath = downloadableContent.ContainerPath,
                        DownloadableContentNcaList = new List<DownloadableContentNca>()
                    };
                }

                container.DownloadableContentNcaList.Add(new DownloadableContentNca
                {
                    Enabled = downloadableContent.Enabled,
                    TitleId = Convert.ToUInt64(downloadableContent.TitleId, 16),
                    FullPath = downloadableContent.FullPath
                });
            }

            if (!string.IsNullOrWhiteSpace(container.ContainerPath))
            {
                _downloadableContentContainerList.Add(container);
            }

            using (FileStream downloadableContentJsonStream = File.Create(_downloadableContentJsonPath, 4096, FileOptions.WriteThrough))
            {
                downloadableContentJsonStream.Write(Encoding.UTF8.GetBytes(JsonHelper.Serialize(_downloadableContentContainerList, true)));
            }
        }
    }
}
