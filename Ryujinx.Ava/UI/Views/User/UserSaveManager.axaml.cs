using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Shim;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UserId = LibHac.Fs.UserId;

namespace Ryujinx.Ava.UI.Views.User
{
    public partial class UserSaveManager : UserControl
    {
        private AccountManager _accountManager;
        private HorizonClient _horizonClient;
        private VirtualFileSystem _virtualFileSystem;
        private int _sortIndex;
        private int _orderIndex;
        private ObservableCollection<SaveModel> _view = new();
        private string _search;

        private NavigationDialogHost _parent;

        public ObservableCollection<SaveModel> Saves { get; set; } = new();

        public ObservableCollection<SaveModel> View
        {
            get => _view;
            set => _view = value;
        }

        public int SortIndex
        {
            get => _sortIndex;
            set
            {
                _sortIndex = value;
                Sort();
            }
        }

        public int OrderIndex
        {
            get => _orderIndex;
            set
            {
                _orderIndex = value;
                Sort();
            }
        }

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                Sort();
            }
        }

        public UserSaveManager()
        {
            InitializeComponent();
            AddHandler(Frame.NavigatedToEvent, (s, e) =>
            {
                NavigatedTo(e);
            }, RoutingStrategies.Direct);
        }

        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (Program.PreviewerDetached)
            {
                switch (arg.NavigationMode)
                {
                    case NavigationMode.New:
                        var args =
                            ((NavigationDialogHost parent, AccountManager accountManager, HorizonClient client, VirtualFileSystem
                                virtualFileSystem))arg.Parameter;
                        _horizonClient = args.client;
                        _virtualFileSystem = args.virtualFileSystem;

                        _parent = args.parent;
                        break;
                }

                DataContext = this;
                Task.Run(LoadSaves);
            }
        }

        public void LoadSaves()
        {
            Saves.Clear();
            var saveDataFilter = SaveDataFilter.Make(programId: default, saveType: SaveDataType.Account,
                new UserId((ulong)_accountManager.LastOpenedUser.UserId.High, (ulong)_accountManager.LastOpenedUser.UserId.Low), saveDataId: default, index: default);

            using var saveDataIterator = new UniqueRef<SaveDataIterator>();

            _horizonClient.Fs.OpenSaveDataIterator(ref saveDataIterator.Ref(), SaveDataSpaceId.User, in saveDataFilter).ThrowIfFailure();

            Span<SaveDataInfo> saveDataInfo = stackalloc SaveDataInfo[10];

            while (true)
            {
                saveDataIterator.Get.ReadSaveDataInfo(out long readCount, saveDataInfo).ThrowIfFailure();

                if (readCount == 0)
                {
                    break;
                }

                for (int i = 0; i < readCount; i++)
                {
                    var save = saveDataInfo[i];
                    if (save.ProgramId.Value != 0)
                    {
                        var saveModel = new SaveModel(save, _horizonClient, _virtualFileSystem);
                        Saves.Add(saveModel);
                        saveModel.DeleteAction = () => { Saves.Remove(saveModel); };
                    }
                    
                    Sort();
                }
            }
        }
        
        private void GoBack(object sender, RoutedEventArgs e)
        {
            _parent?.GoBack();
        }

        private void Sort()
        {
            Saves.AsObservableChangeSet()
                .Filter(Filter)
                .Sort(GetComparer())
                .Bind(out var view).AsObservableList();
            
            _view.Clear();
            _view.AddRange(view);
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

        private bool Filter(object arg)
        {
            if (arg is SaveModel save)
            {
                return string.IsNullOrWhiteSpace(_search) || save.Title.ToLower().Contains(_search.ToLower());
            }

            return false;
        }
    }
}