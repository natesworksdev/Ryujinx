using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.HLE.FileSystem;
using Ryujinx.UI.App.Common;
using Ryujinx.UI.Common.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application = Avalonia.Application;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class DownloadableContentManagerViewModel : BaseModel
    {
        private readonly ApplicationLibrary _applicationLibrary;
        private AvaloniaList<DownloadableContentModel> _downloadableContents = new();
        private AvaloniaList<DownloadableContentModel> _views = new();
        private AvaloniaList<DownloadableContentModel> _selectedDownloadableContents = new();

        private string _search;
        private readonly ApplicationData _applicationData;
        private readonly IStorageProvider _storageProvider;

        public AvaloniaList<DownloadableContentModel> DownloadableContents
        {
            get => _downloadableContents;
            set
            {
                _downloadableContents = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UpdateCount));
                Sort();
            }
        }

        public AvaloniaList<DownloadableContentModel> Views
        {
            get => _views;
            set
            {
                _views = value;
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

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged();
                Sort();
            }
        }

        public string UpdateCount
        {
            get => string.Format(LocaleManager.Instance[LocaleKeys.DlcWindowHeading], DownloadableContents.Count);
        }

        public DownloadableContentManagerViewModel(VirtualFileSystem virtualFileSystem, ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            _applicationLibrary = applicationLibrary;

            _applicationData = applicationData;

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _storageProvider = desktop.MainWindow.StorageProvider;
            }

            LoadDownloadableContents();
        }

        private void LoadDownloadableContents()
        {
            var dlcs = _applicationLibrary.DownloadableContents.Items
                .Where(it => it.Dlc.TitleIdBase == _applicationData.IdBase);
            foreach ((DownloadableContentModel dlc, bool isEnabled) in dlcs)
            {
                DownloadableContents.Add(dlc);

                if (isEnabled)
                {
                    SelectedDownloadableContents.Add(dlc);
                }

                OnPropertyChanged(nameof(UpdateCount));
            }

            // NOTE: Try to load downloadable contents from PFS last to preserve enabled state.
            if (AddDownloadableContent(_applicationData.Path, out var newDlc) && newDlc > 0)
            {
                ShowNewDlcAddedDialog(newDlc);
            }

            // NOTE: Save the list again to remove leftovers.
            Save();
            Sort();
        }

        public void Sort()
        {
            DownloadableContents
                // Sort bundled last
                .OrderBy(it => it.IsBundled ? 0 : 1)
                .ThenBy(it => it.TitleId)
                .AsObservableChangeSet()
                .Filter(Filter)
                .Bind(out var view).AsObservableList();

            // NOTE(jpr): this works around a bug where calling _views.Clear also clears SelectedDownloadableContents for
            // some reason. so we save the items here and add them back after
            var items = SelectedDownloadableContents.ToArray();

            _views.Clear();
            _views.AddRange(view);

            foreach (DownloadableContentModel item in items)
            {
                SelectedDownloadableContents.ReplaceOrAdd(item, item);
            }

            OnPropertyChanged(nameof(Views));
        }

        private bool Filter<T>(T arg)
        {
            if (arg is DownloadableContentModel content)
            {
                return string.IsNullOrWhiteSpace(_search) || content.FileName.ToLower().Contains(_search.ToLower()) || content.TitleIdStr.ToLower().Contains(_search.ToLower());
            }

            return false;
        }

        public async void Add()
        {
            var result = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.SelectDlcDialogTitle],
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("NSP")
                    {
                        Patterns = new[] { "*.nsp" },
                        AppleUniformTypeIdentifiers = new[] { "com.ryujinx.nsp" },
                        MimeTypes = new[] { "application/x-nx-nsp" },
                    },
                },
            });

            var totalDlcAdded = 0;
            foreach (var file in result)
            {
                if (!AddDownloadableContent(file.Path.LocalPath, out var newDlcAdded))
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogDlcNoDlcErrorMessage]);
                }

                totalDlcAdded += newDlcAdded;
            }

            if (totalDlcAdded > 0)
            {
                await ShowNewDlcAddedDialog(0);
            }
        }

        private bool AddDownloadableContent(string path, out int numDlcAdded)
        {
            numDlcAdded = 0;

            if (!File.Exists(path))
            {
                return false;
            }

            if (!_applicationLibrary.TryGetDownloadableContentFromFile(path, out var dlcs))
            {
                return false;
            }

            foreach (var dlc in dlcs.Where(dlc => dlc.TitleIdBase == _applicationData.IdBase))
            {
                if (!DownloadableContents.Contains(dlc))
                {
                    DownloadableContents.Add(dlc);
                    Dispatcher.UIThread.InvokeAsync(() => SelectedDownloadableContents.ReplaceOrAdd(dlc, dlc));

                    numDlcAdded++;
                }
            }

            if (numDlcAdded > 0)
            {
                OnPropertyChanged(nameof(UpdateCount));
                Sort();
            }

            return true;
        }

        public void Remove(DownloadableContentModel model)
        {
            SelectedDownloadableContents.Remove(model);

            if (!model.IsBundled)
            {
                DownloadableContents.Remove(model);
                OnPropertyChanged(nameof(UpdateCount));
                Sort();
            }
        }

        public void RemoveAll()
        {
            SelectedDownloadableContents.Clear();
            DownloadableContents.RemoveMany(DownloadableContents.Where(it => !it.IsBundled));

            OnPropertyChanged(nameof(UpdateCount));
            Sort();
        }

        public void EnableAll()
        {
            SelectedDownloadableContents.Clear();
            SelectedDownloadableContents.AddRange(DownloadableContents);
        }

        public void DisableAll()
        {
            SelectedDownloadableContents.Clear();
        }

        public void Enable(DownloadableContentModel model)
        {
            SelectedDownloadableContents.ReplaceOrAdd(model, model);
        }

        public void Disable(DownloadableContentModel model)
        {
            SelectedDownloadableContents.Remove(model);
        }

        public void Save()
        {
            var dlcs = DownloadableContents.Select(it => (it, SelectedDownloadableContents.Contains(it))).ToList();
            _applicationLibrary.SaveDownloadableContentsForGame(_applicationData, dlcs);
        }

        private Task ShowNewDlcAddedDialog(int numAdded)
        {
            var msg = string.Format(LocaleManager.Instance[LocaleKeys.DlcWindowDlcAddedMessage], numAdded);
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ContentDialogHelper.ShowTextDialog(LocaleManager.Instance[LocaleKeys.DialogConfirmationTitle], msg, "", "", "", LocaleManager.Instance[LocaleKeys.InputDialogOk], (int)Symbol.Checkmark);
            });
        }

    }
}
