using Gtk;
using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using static Ryujinx.Ui.AmiiboManager;

namespace Ryujinx.Ui.Windows
{
    public partial class AmiiboWindow : Window
    {      
        public string AmiiboId { get; private set; }

        public int    DeviceId                 { get; set; }
        public string TitleId                  { get; set; }
        public string LastScannedAmiiboId      { get; set; }
        public bool   LastScannedAmiiboShowAll { get; set; }

        public ResponseType Response { get; private set; }

        public bool UseRandomUuid
        {
            get
            {
                return _randomUuidCheckBox.Active;
            }
        }

        private readonly byte[] _amiiboLogoBytes;

        private List<AmiiboApi>     _amiiboList;

        public AmiiboWindow() : base($"Ryujinx {Program.Version} - Amiibo")
        {
            Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.Resources.Logo_Ryujinx.png");

            InitializeComponent();

            _amiiboList = AmiiboManager.AmiiboApis;

            _amiiboLogoBytes    = EmbeddedResources.Read("Ryujinx/Ui/Resources/Logo_Amiibo.png");
            _amiiboImage.Pixbuf = new Gdk.Pixbuf(_amiiboLogoBytes);

            _scanButton.Sensitive         = false;
            _randomUuidCheckBox.Sensitive = false;

            _ = LoadContentAsync();
        }

        private async Task LoadContentAsync()
        {
            await Task.Run(() =>
            {
                _amiiboList = AmiiboManager.AmiiboApis;

                if (LastScannedAmiiboShowAll)
                {
                    _showAllCheckBox.Click();
                }

                ParseAmiiboData();

                _showAllCheckBox.Clicked += ShowAllCheckBox_Clicked;
            });
        }

        private void ParseAmiiboData()
        {
            List<string> comboxItemList = new List<string>();

            for (int i = 0; i < _amiiboList.Count; i++)
            {
                if (!comboxItemList.Contains(_amiiboList[i].AmiiboSeries))
                {
                    if (!_showAllCheckBox.Active)
                    {
                        foreach (var game in _amiiboList[i].GamesSwitch)
                        {
                            if (game != null)
                            {
                                if (game.GameId.Contains(TitleId))
                                {
                                    comboxItemList.Add(_amiiboList[i].AmiiboSeries);
                                    _amiiboSeriesComboBox.Append(_amiiboList[i].AmiiboSeries, _amiiboList[i].AmiiboSeries);

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        comboxItemList.Add(_amiiboList[i].AmiiboSeries);
                        _amiiboSeriesComboBox.Append(_amiiboList[i].AmiiboSeries, _amiiboList[i].AmiiboSeries);
                    }
                }
            }

            _amiiboSeriesComboBox.Changed += SeriesComboBox_Changed;
            _amiiboCharsComboBox.Changed  += CharacterComboBox_Changed;

            if (LastScannedAmiiboId != "")
            {
                SelectLastScannedAmiibo();
            }
            else
            {
                _amiiboSeriesComboBox.Active = 0;
            }
        }

        private void SelectLastScannedAmiibo()
        {
            bool isSet = _amiiboSeriesComboBox.SetActiveId(_amiiboList.FirstOrDefault(amiibo => amiibo.Head + amiibo.Tail == LastScannedAmiiboId).AmiiboSeries);
            isSet = _amiiboCharsComboBox.SetActiveId(LastScannedAmiiboId);

            if (isSet == false)
            {
                _amiiboSeriesComboBox.Active = 0;
            }
        }

        private async Task UpdateAmiiboPreview(string imageUrl)
        {
            Gdk.Pixbuf amiiboPreview = await AmiiboManager.GetAmiiboPreview(imageUrl);

            if (amiiboPreview != null)
            {
                float ratio = Math.Min((float)_amiiboImage.AllocatedWidth / amiiboPreview.Width,
                                        (float)_amiiboImage.AllocatedHeight / amiiboPreview.Height);

                int resizeHeight = (int)(amiiboPreview.Height * ratio);
                int resizeWidth = (int)(amiiboPreview.Width * ratio);

                _amiiboImage.Pixbuf = amiiboPreview.ScaleSimple(resizeWidth, resizeHeight, Gdk.InterpType.Bilinear);
            }
        }

        //
        // Events
        //
        private void SeriesComboBox_Changed(object sender, EventArgs args)
        {
            _amiiboCharsComboBox.Changed -= CharacterComboBox_Changed;

            _amiiboCharsComboBox.RemoveAll();

            List<AmiiboApi> amiiboSortedList = _amiiboList.Where(amiibo => amiibo.AmiiboSeries == _amiiboSeriesComboBox.ActiveId).OrderBy(amiibo => amiibo.Name).ToList();

            List<string> comboxItemList = new List<string>();

            for (int i = 0; i < amiiboSortedList.Count; i++)
            {
                if (!comboxItemList.Contains(amiiboSortedList[i].Head + amiiboSortedList[i].Tail))
                {
                    if (!_showAllCheckBox.Active)
                    {
                        foreach (var game in amiiboSortedList[i].GamesSwitch)
                        {
                            if (game != null)
                            {
                                if (game.GameId.Contains(TitleId))
                                {
                                    comboxItemList.Add(amiiboSortedList[i].Head + amiiboSortedList[i].Tail);
                                    _amiiboCharsComboBox.Append(amiiboSortedList[i].Head + amiiboSortedList[i].Tail, amiiboSortedList[i].Name);

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        comboxItemList.Add(amiiboSortedList[i].Head + amiiboSortedList[i].Tail);
                        _amiiboCharsComboBox.Append(amiiboSortedList[i].Head + amiiboSortedList[i].Tail, amiiboSortedList[i].Name);
                    }
                }
            }

            _amiiboCharsComboBox.Changed += CharacterComboBox_Changed;

            _amiiboCharsComboBox.Active = 0;

            _scanButton.Sensitive         = true;
            _randomUuidCheckBox.Sensitive = true;
        }

        private void CharacterComboBox_Changed(object sender, EventArgs args)
        {
            AmiiboId = _amiiboCharsComboBox.ActiveId;

            _amiiboImage.Pixbuf = new Gdk.Pixbuf(_amiiboLogoBytes);

            string imageUrl = _amiiboList.FirstOrDefault(amiibo => amiibo.Head + amiibo.Tail == _amiiboCharsComboBox.ActiveId).Image;

            string usageString = "";

            for (int i = 0; i < _amiiboList.Count; i++)
            {
                if (_amiiboList[i].Head + _amiiboList[i].Tail == _amiiboCharsComboBox.ActiveId)
                {
                    bool writable = false;

                    foreach (var item in _amiiboList[i].GamesSwitch)
                    {
                        if (item.GameId.Contains(TitleId))
                        {
                            foreach (AmiiboApiUsage usageItem in item.AmiiboUsage)
                            {
                                usageString += Environment.NewLine + $"- {usageItem.Usage.Replace("/", Environment.NewLine + "-")}";

                                writable = usageItem.Write;
                            }
                        }
                    }

                    if (usageString.Length == 0)
                    {
                        usageString = "Unknown.";
                    }

                    _gameUsageLabel.Text = $"Usage{(writable ? " (Writable)" : "")} : {usageString}";
                }
            }

            _ = UpdateAmiiboPreview(imageUrl);
        }

        private void ShowAllCheckBox_Clicked(object sender, EventArgs e)
        {
            _amiiboImage.Pixbuf = new Gdk.Pixbuf(_amiiboLogoBytes);

            _amiiboSeriesComboBox.Changed -= SeriesComboBox_Changed;
            _amiiboCharsComboBox.Changed  -= CharacterComboBox_Changed;

            _amiiboSeriesComboBox.RemoveAll();
            _amiiboCharsComboBox.RemoveAll();

            _scanButton.Sensitive         = false;
            _randomUuidCheckBox.Sensitive = false;

            new Task(() => ParseAmiiboData()).Start();
        }

        private void ScanButton_Pressed(object sender, EventArgs args)
        {
            LastScannedAmiiboShowAll = _showAllCheckBox.Active;

            Response = ResponseType.Ok;

            Close();
        }

        private void CancelButton_Pressed(object sender, EventArgs args)
        {
            AmiiboId                 = "";
            LastScannedAmiiboId      = "";
            LastScannedAmiiboShowAll = false;

            Response = ResponseType.Cancel;

            Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}