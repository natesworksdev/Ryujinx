using DynamicData;
using DynamicData.Binding;
using Ryujinx.Ava.UI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels;

public class UserSaveManagerViewModel : BaseModel
{
    private int _sortIndex;
    private int _orderIndex;
    private string _search;
    private ObservableCollection<SaveModel> _saves;
    private ObservableCollection<SaveModel> _views;

    public int SortIndex
    {
        get => _sortIndex;
        set
        {
            _sortIndex = value;
            OnPropertyChanged();
            Sort();
        }
    }

    public int OrderIndex
    {
        get => _orderIndex;
        set
        {
            _orderIndex = value;
            OnPropertyChanged();
            Sort();
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

    public ObservableCollection<SaveModel> Saves
    {
        get => _saves;
        set
        {
            _saves = value;
            OnPropertyChanged();
            Sort();
        }
    }
    
    public ObservableCollection<SaveModel> Views
    {
        get => _views;
    }

    public UserSaveManagerViewModel()
    {
        _saves = new ObservableCollection<SaveModel>();
        _views = new ObservableCollection<SaveModel>();
    }
    
    public void Sort()
    {
        Console.WriteLine("Sorting");
        Saves.AsObservableChangeSet()
            .Filter(Filter)
            .Sort(GetComparer())
            .Bind(out var view).AsObservableList();
            
        _views.Clear();
        _views.AddRange(view);
        OnPropertyChanged(nameof(Views));
    }
    
    private bool Filter(object arg)
    {
        if (arg is SaveModel save)
        {
            return string.IsNullOrWhiteSpace(_search) || save.Title.ToLower().Contains(_search.ToLower());
        }

        return false;
    }
    
    private IComparer<SaveModel> GetComparer()
    {
        switch (SortIndex)
        {
            case 0:
                return OrderIndex == 0
                    ? SortExpressionComparer<SaveModel>.Ascending(save => save.Title)
                    : SortExpressionComparer<SaveModel>.Descending(save => save.Title);
            case 1:
                return OrderIndex == 0
                    ? SortExpressionComparer<SaveModel>.Ascending(save => save.Size)
                    : SortExpressionComparer<SaveModel>.Descending(save => save.Size);
            default:
                return null;
        }
    }
}