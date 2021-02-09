using Gtk;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Ui.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ryujinx.Ui.Windows
{
    public partial class AmiiboWindow : Window
    {
        private struct LastUpdatedJson
        {
            public DateTime LastUpdated { get; set; }
        }

        private struct AmiiboJson
        {
            public List<Amiibo> Amiibo      { get; set; }
            public DateTime     LastUpdated { get; set; }
        }

        private struct Amiibo
        {
            public string Name         { get; set; }
            public string Head         { get; set; }
            public string Tail         { get; set; }
            public string Image        { get; set; }
            public string AmiiboSeries { get; set; }
            public string Character    { get; set; }
            public string GameSeries   { get; set; }
            public string Type         { get; set; }

            public Dictionary<string, string> Release { get; set; }
        }

        private const string DEFAULT_JSON = "{ \"amiibo\": [] }";

        public string AmiiboId { get; private set; }
        public int    DeviceId { get; set; }

        public bool UseRandomUuid
        {
            get
            {
                return _randomUuidCheckBox.Active;
            }
        }

        private readonly HttpClient            _httpClient;
        private readonly string                _amiiboJsonPath;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        private readonly byte[] _amiiboLogoBytes;

        private List<Amiibo> _amiiboList;

        public AmiiboWindow() : base($"Ryujinx {Program.Version} - Amiibo")
        {
            Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.Resources.Logo_Ryujinx.png");

            InitializeComponent();

            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMilliseconds(5000)
            };

            Directory.CreateDirectory(System.IO.Path.Join(AppDataManager.BaseDirPath, "system", "amiibo"));

            _amiiboJsonPath = System.IO.Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", "Amiibo.json");
            _amiiboList     = new List<Amiibo>();

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                DictionaryKeyPolicy  = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _amiiboLogoBytes    = EmbeddedResources.Read("Ryujinx/Ui/Resources/Logo_Amiibo.png");
            _amiiboImage.Pixbuf = new Gdk.Pixbuf(_amiiboLogoBytes);

            _scanButton.Sensitive         = false;
            _randomUuidCheckBox.Sensitive = false;

            _ = LoadContentAsync();
        }

        private async Task<bool> CheckConnectivity()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("https://www.google.com/");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                GtkDialog.CreateInfoDialog($"Amiibo API", "You must have an active internet connection in order to download Amiibo informations.");

                return false;
            }
        }

        private async Task LoadContentAsync()
        {
            string amiiboJsonString = DEFAULT_JSON;

            if (File.Exists(_amiiboJsonPath))
            {
                amiiboJsonString = File.ReadAllText(_amiiboJsonPath);

                if (await NeedsUpdate(JsonSerializer.Deserialize<AmiiboJson>(amiiboJsonString, _jsonSerializerOptions).LastUpdated))
                {
                    amiiboJsonString = await DownloadAmiiboJson();
                }
            }
            else
            {
                try
                {
                    amiiboJsonString = await DownloadAmiiboJson();
                }
                catch
                {
                    GtkDialog.CreateInfoDialog($"Amiibo API", "You must have an active internet connection in order to download Amiibo informations.");

                    Close();
                }
            }

            _amiiboList = JsonSerializer.Deserialize<AmiiboJson>(amiiboJsonString, _jsonSerializerOptions).Amiibo;
            _amiiboList = _amiiboList.OrderBy(amiibo => amiibo.AmiiboSeries).ToList();

            foreach (string series in _amiiboList.Select(amiibo => amiibo.AmiiboSeries).Distinct())
            {
                _amiiboSeriesComboBox.Append(series, series);
            }

            _amiiboSeriesComboBox.Changed += SeriesComboBox_Changed;
            _amiiboCharsComboBox.Changed  += CharacterComboBox_Changed;

            _amiiboSeriesComboBox.Active = 0;
        }

        private async Task<bool> NeedsUpdate(DateTime oldLastUpdated)
        {
            if (!await CheckConnectivity())
            {
                return false;
            }

            HttpResponseMessage response = await _httpClient.GetAsync("https://amiibo.ryujinx.org/lastupdated");

            if (response.IsSuccessStatusCode)
            {
                string   lastUpdatedJson = await response.Content.ReadAsStringAsync();
                DateTime lastUpdated     = JsonSerializer.Deserialize<LastUpdatedJson>(lastUpdatedJson, _jsonSerializerOptions).LastUpdated;

                return lastUpdated != oldLastUpdated;
            }

            return false;
        }

        private async Task<string> DownloadAmiiboJson()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("https://amiibo.ryujinx.org/");

            if (response.IsSuccessStatusCode)
            {
                string amiiboJsonString = await response.Content.ReadAsStringAsync();

                using (FileStream dlcJsonStream = File.Create(_amiiboJsonPath, 4096, FileOptions.WriteThrough))
                {
                    dlcJsonStream.Write(Encoding.UTF8.GetBytes(amiiboJsonString));
                }

                return amiiboJsonString;
            }
            else
            {
                GtkDialog.CreateInfoDialog($"Amiibo API", "An error occured while fetching informations from the API.");

                Close();
            }

            return DEFAULT_JSON;
        }

        private async Task UpdateAmiiboPreview(string imageUrl)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                byte[]     amiiboPreviewBytes = await response.Content.ReadAsByteArrayAsync();
                Gdk.Pixbuf amiiboPreview      = new Gdk.Pixbuf(amiiboPreviewBytes);

                float ratio = Math.Min((float)_amiiboImage.AllocatedWidth  / amiiboPreview.Width,
                                       (float)_amiiboImage.AllocatedHeight / amiiboPreview.Height);

                int resizeHeight = (int)(amiiboPreview.Height * ratio);
                int resizeWidth  = (int)(amiiboPreview.Width  * ratio);

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

            List<Amiibo> amiiboSortedList = _amiiboList.Where(amiibo => amiibo.AmiiboSeries == _amiiboSeriesComboBox.ActiveId).OrderBy(amiibo => amiibo.Name).ToList();

            foreach (Amiibo amiibo in amiiboSortedList)
            {
                _amiiboCharsComboBox.Append(amiibo.Head + amiibo.Tail, amiibo.Name);
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

            _ = UpdateAmiiboPreview(imageUrl);
        }

        private void ScanButton_Pressed(object sender, EventArgs args)
        {
            Close();
        }

        private void CancelButton_Pressed(object sender, EventArgs args)
        {
            AmiiboId = "";

            Close();
        }

        protected override void Dispose(bool disposing)
        {
            _httpClient.Dispose();

            base.Dispose(disposing);
        }
    }
}