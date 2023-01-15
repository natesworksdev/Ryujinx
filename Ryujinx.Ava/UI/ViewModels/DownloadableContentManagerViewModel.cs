using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class DownloadableContentManagerViewModel : BaseModel
    {
        private readonly List<DownloadableContentContainer> _downloadableContentContainerList;
        private readonly string                             _downloadableContentJsonPath;

        private VirtualFileSystem                           _virtualFileSystem;
        private AvaloniaList<DownloadableContentModel>      _downloadableContents = new();
        private AvaloniaList<DownloadableContentModel>      _selectedDownloadableContents = new();

        private ulong _titleId;
        private string _titleName;

        public AvaloniaList<DownloadableContentModel> DownloadableContents
        {
            get => _downloadableContents;
            set
            {
                _downloadableContents = value;
                OnPropertyChanged();
            }
        }

        public AvaloniaList<DownloadableContentModel> SelectedDownloadableContents
        {
            get => _selectedDownloadableContents;
            set
            {
                _selectedDownloadableContents = value;
                OnPropertyChanged();
            }
        }

        public DownloadableContentManagerViewModel(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName)
        {
            _virtualFileSystem = virtualFileSystem;

            _titleId   = titleId;
            _titleName = titleName;

            _downloadableContentJsonPath = Path.Combine(AppDataManager.GamesDirPath, titleId.ToString("x16"), "dlc.json");

            try
            {
                _downloadableContentContainerList = JsonHelper.DeserializeFromFile<List<DownloadableContentContainer>>(_downloadableContentJsonPath);
            }
            catch
            {
                _downloadableContentContainerList = new List<DownloadableContentContainer>();
            }

            LoadDownloadableContents();
        }

        private void LoadDownloadableContents()
        {
            foreach (DownloadableContentContainer downloadableContentContainer in _downloadableContentContainerList)
            {
                if (File.Exists(downloadableContentContainer.ContainerPath))
                {
                    using FileStream containerFile = File.OpenRead(downloadableContentContainer.ContainerPath);

                    PartitionFileSystem partitionFileSystem = new(containerFile.AsStorage());

                    _virtualFileSystem.ImportTickets(partitionFileSystem);

                    foreach (DownloadableContentNca downloadableContentNca in downloadableContentContainer.DownloadableContentNcaList)
                    {
                        using UniqueRef<IFile> ncaFile = new();

                        partitionFileSystem.OpenFile(ref ncaFile.Ref(), downloadableContentNca.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                        Nca nca = TryOpenNca(ncaFile.Get.AsStorage(), downloadableContentContainer.ContainerPath);
                        if (nca != null)
                        {
                            DownloadableContents.Add(new DownloadableContentModel(nca.Header.TitleId.ToString("X16"),
                                downloadableContentContainer.ContainerPath,
                                downloadableContentNca.FullPath,
                                downloadableContentNca.Enabled));
                        }
                    }
                }
            }

            // NOTE: Save the list again to remove leftovers.
            Save();
        }

        private Nca TryOpenNca(IStorage ncaStorage, string containerPath)
        {
            try
            {
                return new Nca(_virtualFileSystem.KeySet, ncaStorage);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance[LocaleKeys.DialogDlcLoadNcaErrorMessage], ex.Message, containerPath));
                });
            }

            return null;
        }

        public async void Add()
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title         = LocaleManager.Instance[LocaleKeys.SelectDlcDialogTitle],
                AllowMultiple = true
            };

            dialog.Filters.Add(new FileDialogFilter
            {
                Name       = "NSP",
                Extensions = { "nsp" }
            });

            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                string[] files = await dialog.ShowAsync(desktop.MainWindow);

                if (files != null)
                {
                    foreach (string file in files)
                    {
                        await AddDownloadableContent(file);
                    }
                }
            }
        }

        private async Task AddDownloadableContent(string path)
        {
            if (!File.Exists(path) || DownloadableContents.FirstOrDefault(x => x.ContainerPath == path) != null)
            {
                return;
            }

            using FileStream containerFile = File.OpenRead(path);

            PartitionFileSystem partitionFileSystem         = new(containerFile.AsStorage());
            bool                containsDownloadableContent = false;

            _virtualFileSystem.ImportTickets(partitionFileSystem);

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
                    if ((nca.Header.TitleId & 0xFFFFFFFFFFFFE000) != _titleId)
                    {
                        break;
                    }

                    DownloadableContents.Add(new DownloadableContentModel(nca.Header.TitleId.ToString("X16"), path, fileEntry.FullPath, true));

                    containsDownloadableContent = true;
                }
            }

            if (!containsDownloadableContent)
            {
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogDlcNoDlcErrorMessage]);
            }
        }

        public void Remove(DownloadableContentModel model)
        {
            DownloadableContents.Remove(model);
        }

        public void RemoveAll()
        {
            DownloadableContents.Clear();
        }

        public void EnableAll()
        {
            foreach(var item in DownloadableContents)
            {
                item.Enabled = true;
            }
        }

        public void DisableAll()
        {
            foreach (var item in DownloadableContents)
            {
                item.Enabled = false;
            }
        }

        public void Save()
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
                        ContainerPath              = downloadableContent.ContainerPath,
                        DownloadableContentNcaList = new List<DownloadableContentNca>()
                    };
                }

                container.DownloadableContentNcaList.Add(new DownloadableContentNca
                {
                    Enabled  = downloadableContent.Enabled,
                    TitleId  = Convert.ToUInt64(downloadableContent.TitleId, 16),
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