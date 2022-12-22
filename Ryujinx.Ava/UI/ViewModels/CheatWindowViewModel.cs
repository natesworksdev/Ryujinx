using Avalonia.Collections;
using Ryujinx.Ava.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.Ava.UI.ViewModels;

public class CheatWindowViewModel : BaseModel
{
    private string _heading;
    private AvaloniaList<CheatsList> _loadedCheats;
    private bool _noCheatsFound;
    
    public string EnabledCheatsPath;
    public event Action CloseAction;

    public string Heading
    {
        get => _heading;
        set
        {
            _heading = value;
            OnPropertyChanged();
        }
    }

    public AvaloniaList<CheatsList> LoadedCheats
    {
        get => _loadedCheats;
        set
        {
            _loadedCheats = value;
            OnPropertyChanged();
        }
    }

    public bool NoCheatsFound
    {
        get => _noCheatsFound;
        set
        {
            _noCheatsFound = value;
            OnPropertyChanged();
        }
    }

    public void Save()
    {
        if (NoCheatsFound)
        {
            return;
        }

        List<string> enabledCheats = new();

        foreach (var cheats in LoadedCheats)
        {
            foreach (var cheat in cheats)
            {
                if (cheat.IsEnabled)
                {
                    enabledCheats.Add(cheat.BuildIdKey);
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(EnabledCheatsPath));

        File.WriteAllLines(EnabledCheatsPath, enabledCheats);

        CloseAction?.Invoke();
    }
}