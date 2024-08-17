using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData.Kernel;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.UI.App.Common;
using Ryujinx.UI.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application = Avalonia.Application;
using Path = System.IO.Path;

namespace Ryujinx.Ava.UI.ViewModels
{
    public record TitleUpdateViewNoUpdateSentinal();

    public class TitleUpdateViewModel : BaseModel
    {

        public TitleUpdateMetadata TitleUpdateWindowData;
        public readonly string TitleUpdateJsonPath;
        private VirtualFileSystem VirtualFileSystem { get; }
        private ApplicationLibrary ApplicationLibrary { get; }
        private ApplicationData ApplicationData { get; }

        private AvaloniaList<TitleUpdateModel> _titleUpdates = new();
        private AvaloniaList<object> _views = new();
        private object _selectedUpdate = new TitleUpdateViewNoUpdateSentinal();

        private static readonly TitleUpdateMetadataJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public AvaloniaList<TitleUpdateModel> TitleUpdates
        {
            get => _titleUpdates;
            set
            {
                _titleUpdates = value;
                OnPropertyChanged();
            }
        }

        public AvaloniaList<object> Views
        {
            get => _views;
            set
            {
                _views = value;
                OnPropertyChanged();
            }
        }

        public object SelectedUpdate
        {
            get => _selectedUpdate;
            set
            {
                _selectedUpdate = value;
                OnPropertyChanged();
            }
        }

        public IStorageProvider StorageProvider;

        public TitleUpdateViewModel(VirtualFileSystem virtualFileSystem, ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            VirtualFileSystem = virtualFileSystem;
            ApplicationLibrary = applicationLibrary;

            ApplicationData = applicationData;

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                StorageProvider = desktop.MainWindow.StorageProvider;
            }

            TitleUpdateJsonPath = Path.Combine(AppDataManager.GamesDirPath, ApplicationData.IdBaseString, "updates.json");

            try
            {
                TitleUpdateWindowData = JsonHelper.DeserializeFromFile(TitleUpdateJsonPath, _serializerContext.TitleUpdateMetadata);
            }
            catch
            {
                Logger.Warning?.Print(LogClass.Application, $"Failed to deserialize title update data for {ApplicationData.IdBaseString} at {TitleUpdateJsonPath}");

                TitleUpdateWindowData = new TitleUpdateMetadata
                {
                    Selected = "",
                    Paths = new List<string>(),
                };

                Save();
            }

            LoadUpdates();
        }

        private void LoadUpdates()
        {
            // Try to load updates from PFS first
            AddUpdate(ApplicationData.Path, true);

            foreach (string path in TitleUpdateWindowData.Paths)
            {
                AddUpdate(path);
            }

            var selected = TitleUpdates.FirstOrOptional(x => x.Path == TitleUpdateWindowData.Selected);
            SelectedUpdate = selected.HasValue ? selected.Value : new TitleUpdateViewNoUpdateSentinal();

            // NOTE: Save the list again to remove leftovers.
            Save();
            SortUpdates();
        }

        public void SortUpdates()
        {
            var sortedUpdates = TitleUpdates.OrderByDescending(update => update.Version);

            Views.Clear();
            Views.Add(new TitleUpdateViewNoUpdateSentinal());
            Views.AddRange(sortedUpdates);

            if (SelectedUpdate is TitleUpdateViewNoUpdateSentinal)
            {
                SelectedUpdate = Views[0];
            }
            else if (!TitleUpdates.Contains((TitleUpdateModel)SelectedUpdate))
            {
                SelectedUpdate = Views.Count > 1 ? Views[1] : Views[0];
            }
        }

        private void AddUpdate(string path, bool ignoreNotFound = false, bool selected = false)
        {
            if (!File.Exists(path) || TitleUpdates.Any(x => x.Path == path))
            {
                return;
            }

            try
            {
                if (!ApplicationLibrary.TryGetTitleUpdatesFromFile(path, out var titleUpdates))
                {
                    if (!ignoreNotFound)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                            ContentDialogHelper.CreateErrorDialog(
                                LocaleManager.Instance[LocaleKeys.DialogUpdateAddUpdateErrorMessage]));
                    }

                    return;
                }

                foreach (var titleUpdate in titleUpdates)
                {
                    if (titleUpdate.TitleIdBase != ApplicationData.Id)
                    {
                        continue;
                    }

                    TitleUpdates.Add(titleUpdate);

                    if (selected)
                    {
                        Dispatcher.UIThread.InvokeAsync(() => SelectedUpdate = titleUpdate);
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(() => ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogLoadFileErrorMessage, ex.Message, path)));
            }
        }

        public void RemoveUpdate(TitleUpdateModel update)
        {
            TitleUpdates.Remove(update);

            SortUpdates();
        }

        public async Task Add()
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.AllSupportedFormats])
                    {
                        Patterns = new[] { "*.nsp" },
                        AppleUniformTypeIdentifiers = new[] { "com.ryujinx.nsp" },
                        MimeTypes = new[] { "application/x-nx-nsp" },
                    },
                },
            });

            foreach (var file in result)
            {
                AddUpdate(file.Path.LocalPath, selected: true);
            }

            SortUpdates();
        }

        public void Save()
        {
            TitleUpdateWindowData.Paths.Clear();
            TitleUpdateWindowData.Selected = "";

            foreach (TitleUpdateModel update in TitleUpdates)
            {
                TitleUpdateWindowData.Paths.Add(update.Path);

                if (update == SelectedUpdate as TitleUpdateModel)
                {
                    TitleUpdateWindowData.Selected = update.Path;
                }
            }

            JsonHelper.SerializeToFile(TitleUpdateJsonPath, TitleUpdateWindowData, _serializerContext.TitleUpdateMetadata);
        }
    }
}
