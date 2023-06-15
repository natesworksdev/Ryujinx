using DynamicData;
using DynamicData.Binding;
using LibHac.Fs;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    public record class LoadingBarData
    {
        public int Max { get; set; } = 0;
        public int Curr { get; set; } = 0;
        public bool IsVisible => Max > 0 && Curr < Max;

        public void Reset()
        {
            Max = 0;
            Curr = 0;
        }
    }

    public class LoadingBarEventArgs : EventArgs
    { 
        public int Curr { get; set; }
        public int Max { get; set; }
    }

    public class ImportSaveEventArgs : EventArgs
    { 
        public SaveDataInfo SaveInfo { get; init; }
    }

    public class UserSaveManagerViewModel : BaseModel
    {
        private int _sortIndex;
        private int _orderIndex;
        private string _search;
        private bool _isGoBackEnabled = true;
        private LoadingBarData _loadingBarData = new();
        private ObservableCollection<SaveModel> _saves = new();
        private ObservableCollection<SaveModel> _views = new();
        private AccountManager _accountManager;

        public string SaveManagerHeading => LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.SaveManagerHeading, _accountManager.LastOpenedUser.Name, _accountManager.LastOpenedUser.UserId);

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

        public bool IsGoBackEnabled
        { 
            get => _isGoBackEnabled;
            set
            {
                _isGoBackEnabled = value;

                OnPropertyChanged();
            }
        }

        public LoadingBarData LoadingBarData
        {
            get => _loadingBarData;
            set
            {
                _loadingBarData = value;

                OnPropertyChanged();
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
            set
            {
                _views = value;
                OnPropertyChanged();
            }
        }

        public UserSaveManagerViewModel(AccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public void AddNewSaveEntry(SaveModel model)
        {
            _saves.Add(model);
            _views.Add(model);
            OnPropertyChanged(nameof(Views));
        }

        public void Sort()
        {
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
}