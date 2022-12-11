using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Ui.Controls;
using Ryujinx.Ava.Ui.Models;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;
using SpanHelpers = LibHac.Common.SpanHelpers;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class AutoTitleUpdateLoader
    {
        private TitleUpdateMetadata _titleUpdateWindowData;
        public List<ApplicationData> Applications { get; private set; }
        private readonly VirtualFileSystem FileSystem;
        private AvaloniaList<TitleUpdateModel> titleUpdates { get; set; }

        public AutoTitleUpdateLoader(Collection<ApplicationData> applications, VirtualFileSystem virtualFileSystem)
        {
            Applications = applications.ToList();
            FileSystem = virtualFileSystem;
        }


        public async Task AutoLoadUpdatesAsync(ApplicationData application, Dictionary<string, string> updatePathandGameNames)
        {
            titleUpdates = new AvaloniaList<TitleUpdateModel>();

            char[] bannedSymbols = { '.', ',', ':', ';', '>', '<', '\'', '\"', };
            string gameTitle = string.Join("", application.TitleName.Split(bannedSymbols)).ToLower().Trim();


            //Loops through the Updates to the given gameTitle and adds them to the downloadableContent List
            updatePathandGameNames.Where(titleUpdate => titleUpdate.Value.ToLower() == gameTitle)
                .ToList()
                .ForEach(async update => await AddUpdate(update.Key, application));


            List<DownloadableContentContainer> downloadableContentContainers = new List<DownloadableContentContainer>();
            string jsonPath = LoadJsonFromTitle(application, downloadableContentContainers);
            Save(jsonPath);
        }

        private string LoadJsonFromTitle(ApplicationData applicationdata, List<DownloadableContentContainer> _downloadableContentContainerList)
        {
            ulong titleId = ulong.Parse(applicationdata.TitleId, NumberStyles.HexNumber);

            string _titleUpdateJsonPath = Path.Combine(AppDataManager.GamesDirPath, titleId.ToString("x16"), "updates.json");

            try
            {
                _titleUpdateWindowData = JsonHelper.DeserializeFromFile<TitleUpdateMetadata>(_titleUpdateJsonPath);
            }
            catch
            {
                _titleUpdateWindowData = new TitleUpdateMetadata
                {
                    Selected = "",
                    Paths = new List<string>()
                };
            }

            return _titleUpdateJsonPath;
        }

        private async Task AddUpdate(string path, ApplicationData applicationData)
        {
            if (File.Exists(path) && !titleUpdates.Any(x => x.Path == path))
            {
                using FileStream file = new(path, FileMode.Open, FileAccess.Read);

                try
                {
                    (Nca patchNca, Nca controlNca) = ApplicationLoader.GetGameUpdateDataFromPartition(FileSystem, new PartitionFileSystem(file.AsStorage()), ulong.Parse(applicationData.TitleId, NumberStyles.HexNumber).ToString("x16"), 0);

                    if (controlNca != null && patchNca != null)
                    {
                        ApplicationControlProperty controlData = new();

                        using UniqueRef<IFile> nacpFile = new();

                        controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None).OpenFile(ref nacpFile.Ref(), "/control.nacp".ToU8Span(), OpenMode.Read).ThrowIfFailure();
                        nacpFile.Get.Read(out _, 0, SpanHelpers.AsByteSpan(ref controlData), ReadOption.None).ThrowIfFailure();

                        titleUpdates.Add(new TitleUpdateModel(controlData, path));

                        foreach (var update in titleUpdates)
                        {
                            update.IsEnabled = false;
                        }

                        titleUpdates.Last().IsEnabled = true;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance["DialogUpdateAddUpdateErrorMessage"]);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance["DialogDlcLoadNcaErrorMessage"], ex.Message, path));
                    });
                }
            }
        }

        public void Save(string titleUpdateJsonPath)
        {
            _titleUpdateWindowData.Paths.Clear();

            _titleUpdateWindowData.Selected = "";

            foreach (TitleUpdateModel update in titleUpdates)
            {
                _titleUpdateWindowData.Paths.Add(update.Path);

                if (update.IsEnabled)
                {
                    _titleUpdateWindowData.Selected = update.Path;
                }
            }

            using (FileStream titleUpdateJsonStream = File.Create(titleUpdateJsonPath, 4096, FileOptions.WriteThrough))
            {
                titleUpdateJsonStream.Write(Encoding.UTF8.GetBytes(JsonHelper.Serialize(_titleUpdateWindowData, true)));
            }
        }
    }
}
