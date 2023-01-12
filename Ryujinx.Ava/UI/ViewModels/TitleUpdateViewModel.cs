using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ns;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpanHelpers = LibHac.Common.SpanHelpers;
using Path = System.IO.Path;

namespace Ryujinx.Ava.UI.ViewModels;

public class TitleUpdateViewModel : BaseModel
{
    public TitleUpdateMetadata _titleUpdateWindowData;
    public readonly string     _titleUpdateJsonPath;
    private VirtualFileSystem   _virtualFileSystem { get; }
    private ulong               _titleId   { get; }
    private string              _titleName { get; }

    private AvaloniaList<TitleUpdateModel> _titleUpdates = new();
    private object _selectedUpdate;

    public AvaloniaList<TitleUpdateModel> TitleUpdates
    {
        get => _titleUpdates;
        set
        {
            _titleUpdates = value;
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

    public TitleUpdateViewModel(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName)
    {
        _virtualFileSystem = virtualFileSystem;

        _titleId   = titleId;
        _titleName = titleName;

        _titleUpdateJsonPath = Path.Combine(AppDataManager.GamesDirPath, titleId.ToString("x16"), "updates.json");

        try
        {
            _titleUpdateWindowData = JsonHelper.DeserializeFromFile<TitleUpdateMetadata>(_titleUpdateJsonPath);
        }
        catch
        {
            _titleUpdateWindowData = new TitleUpdateMetadata
            {
                Selected = "",
                Paths    = new List<string>()
            };
        }

        LoadUpdates();
    }

    private void LoadUpdates()
    {
        TitleUpdates.Add(new TitleUpdateModel(default, string.Empty, true));

        foreach (string path in _titleUpdateWindowData.Paths)
        {
            AddUpdate(path);
        }

        if (_titleUpdateWindowData.Selected == "")
        {
            SelectedUpdate = TitleUpdates[0];
        }
        else
        {
            TitleUpdateModel selected = TitleUpdates.FirstOrDefault(x => x.Path == _titleUpdateWindowData.Selected);

            if (selected != null)
            {
                SelectedUpdate = selected;
            }
        }

        SortUpdates();
    }

    private void SortUpdates()
    {
        var list = TitleUpdates.ToList();

        list.Sort((first, second) =>
        {
            if (string.IsNullOrEmpty(first.Control.DisplayVersionString.ToString()))
            {
                return -1;
            }
            else if (string.IsNullOrEmpty(second.Control.DisplayVersionString.ToString()))
            {
                return 1;
            }

            return Version.Parse(first.Control.DisplayVersionString.ToString()).CompareTo(Version.Parse(second.Control.DisplayVersionString.ToString())) * -1;
        });

        TitleUpdates.Clear();
        TitleUpdates.AddRange(list);
    }

    private void AddUpdate(string path)
    {
        if (File.Exists(path) && ! TitleUpdates.Any(x => x.Path == path))
        {
            using FileStream file = new(path, FileMode.Open, FileAccess.Read);

            try
            {
                (Nca patchNca, Nca controlNca) = ApplicationLoader.GetGameUpdateDataFromPartition(_virtualFileSystem, new PartitionFileSystem(file.AsStorage()), _titleId.ToString("x16"), 0);

                if (controlNca != null && patchNca != null)
                {
                    ApplicationControlProperty controlData = new();

                    using UniqueRef<IFile> nacpFile = new();

                    controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None).OpenFile(ref nacpFile.Ref(), "/control.nacp".ToU8Span(), OpenMode.Read).ThrowIfFailure();
                    nacpFile.Get.Read(out _, 0, SpanHelpers.AsByteSpan(ref controlData), ReadOption.None).ThrowIfFailure();

                    TitleUpdates.Add(new TitleUpdateModel(controlData, path));

                    SelectedUpdate = TitleUpdates.Last();
                }
                else
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogUpdateAddUpdateErrorMessage]);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await ContentDialogHelper.CreateErrorDialog(string.Format(LocaleManager.Instance[LocaleKeys.DialogDlcLoadNcaErrorMessage], ex.Message, path));
                });
            }
        }
    }

    private void RemoveUpdates(bool removeSelectedOnly = false)
    {
        if (removeSelectedOnly)
        {
            TitleUpdates.RemoveAll(TitleUpdates.Where(x => x == SelectedUpdate && !x.IsNoUpdate).ToList());
        }
        else
        {
            TitleUpdates.RemoveAll(TitleUpdates.Where(x => !x.IsNoUpdate).ToList());
        }

        SelectedUpdate = TitleUpdates.FirstOrDefault(x => x.IsNoUpdate);

        SortUpdates();
    }

    public void RemoveSelected()
    {
        RemoveUpdates(true);
    }

    public void RemoveAll()
    {
        RemoveUpdates();
    }

    public async void Add()
    {
        OpenFileDialog dialog = new()
        {
            Title         = LocaleManager.Instance[LocaleKeys.SelectUpdateDialogTitle],
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
                    AddUpdate(file);
                }
            }
        }

        SortUpdates();
    }
}