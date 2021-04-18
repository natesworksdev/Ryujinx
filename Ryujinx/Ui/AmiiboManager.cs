using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Ui.App;
using Ryujinx.Ui.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ryujinx.Ui
{
    public static class AmiiboManager
    {
        private const string DEFAULT_JSON = "{ \"amiibo\": [] }";

        private static HttpClient _httpClient;
        private static string _amiiboJsonPath;
        private static AmiiboJson _amiiboJson;
        private static List<AmiiboApi> _amiiboApis;
        private static Dictionary<string, Gdk.Pixbuf> _amiiboPreviews;
        private static bool _initialized;
        private static bool _onlineMode = true;

        public static List<AmiiboApi> AmiiboApis { get => _amiiboApis; }

        public static void Initialize()
        {
            if (!_initialized)
            {
                _httpClient = new HttpClient()
                {
                    Timeout = TimeSpan.FromMilliseconds(5000)
                };

                _amiiboPreviews = new Dictionary<string, Gdk.Pixbuf>();

                Directory.CreateDirectory(Path.Join(AppDataManager.BaseDirPath, "system", "amiibo"));

                _amiiboJsonPath = Path.Join(AppDataManager.BaseDirPath, "system", "amiibo", "Amiibo.json");

                Task.Run(async () =>
                {
                    if (File.Exists(_amiiboJsonPath))
                    {
                        try
                        {
                            string amiiboJsonString = await File.ReadAllTextAsync(_amiiboJsonPath);
                            LoadAmiiboJson(amiiboJsonString);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning?.Print(LogClass.Application, $"Failed to read Amiibo JSON data: {ex.Message}");

                            LoadAmiiboJson(DEFAULT_JSON);
                        }
                    }
                    else
                    {
                        LoadAmiiboJson(DEFAULT_JSON);
                    }
                });

                _initialized = true;
            }
        }

        private static void LoadAmiiboJson(string amiiboJsonString)
        {
            try
            {
                _amiiboJson = JsonSerializer.Deserialize<AmiiboJson>(amiiboJsonString);
                _amiiboApis = _amiiboJson.Amiibo.OrderBy(amiibo => amiibo.AmiiboSeries).ToList();
            }
            catch (Exception ex)
            {
                Logger.Warning?.Print(LogClass.Application, $"Failed to deserialize Amiibo JSON data: {ex.Message}");
                _amiiboApis = new List<AmiiboApi>();
            }
        }

        /// <summary>
        /// Checks to see if there is a new version of amiibo data available, if an update is available it will be downloaded.
        /// </summary>
        /// <returns><b>True</b>: New data was downloaded and installed successfully.<br>
        /// </br><b>False</b>: The current version of the data is already up-to-date or an error occurred.</returns>
        public static async Task<bool> UpdateAmiibos()
        {
            Logger.Info?.Print(LogClass.Application, "Checking for Amiibo Updates...");

            string amiiboJsonString;

            if (File.Exists(_amiiboJsonPath))
            {
                if (await NeedsUpdate(_amiiboJson.LastUpdated))
                {
                    amiiboJsonString = await DownloadAmiibos();

                    // Don't overwrite existing configuration.
                    if (amiiboJsonString != null)
                    {
                        LoadAmiiboJson(amiiboJsonString);

                        return true;
                    }

                    return false;
                }
                else
                {
                    Logger.Info?.Print(LogClass.Application, "Amiibos are already up to date.");

                    return false;
                }
            }
            else
            {
                try
                {
                    amiiboJsonString = await DownloadAmiibos() ?? DEFAULT_JSON;

                    LoadAmiiboJson(amiiboJsonString);

                    return true;
                }
                catch (Exception ex)
                {
                    ShowAmiiboServiceWarning(ex.Message);

                    return false;
                }
            }
        }

        public static async Task<Gdk.Pixbuf> GetAmiiboPreview(string imageUrl)
        {
            if (_amiiboPreviews.ContainsKey(imageUrl))
            {
                return _amiiboPreviews[imageUrl];
            }
            else
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(imageUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] amiiboPreviewBytes = await response.Content.ReadAsByteArrayAsync();
                        Gdk.Pixbuf result = new Gdk.Pixbuf(amiiboPreviewBytes);

                        _amiiboPreviews[imageUrl] = result;

                        return result;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error?.Print(LogClass.Application, $"Failed to read Amiibo preview data: {ex.Message}");

                    return null;
                }
            }
        }

        private static async Task<bool> NeedsUpdate(DateTime oldLastModified)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://amiibo.ryujinx.org/"));

                if (response.IsSuccessStatusCode)
                {
                    ShowOnlineModeMessage();

                    return response.Content.Headers.LastModified.Value.DateTime != oldLastModified;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowAmiiboServiceWarning(ex.Message);

                return false;
            }
        }

        private static async Task<string> DownloadAmiibos()
        {
            Logger.Info?.Print(LogClass.Application, "Downloading newer version of amiibo data...");

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("https://amiibo.ryujinx.org/");

                if (response.IsSuccessStatusCode)
                {
                    string amiiboJsonString = await response.Content.ReadAsStringAsync();

                    AmiiboJson amiiboJson = JsonSerializer.Deserialize<AmiiboJson>(amiiboJsonString);

                    amiiboJson.LastUpdated = response.Content.Headers.LastModified.Value.DateTime;

                    amiiboJsonString = JsonSerializer.Serialize<AmiiboJson>(amiiboJson);

                    using (FileStream dlcJsonStream = File.Create(_amiiboJsonPath, 4096, FileOptions.WriteThrough))
                    {
                        await dlcJsonStream.WriteAsync(Encoding.UTF8.GetBytes(amiiboJsonString));
                    }

                    Logger.Info?.Print(LogClass.Application, "Amiibo data updated successfully.");

                    ShowOnlineModeMessage();

                    return amiiboJsonString;
                }
                else
                {
                    Logger.Warning?.Print(LogClass.Application, "An error occured while fetching informations from the Amiibo API.");
                    ShowOfflineModeMessage();
                }
            }
            catch (Exception ex)
            {
                ShowAmiiboServiceWarning(ex.Message);
            }

            return null;
        }

        private static void ShowAmiiboServiceWarning(string message)
        {
            Logger.Warning?.Print(LogClass.Application, $"Unable to connect to the Amiibo API server. The service may be down or you may need to verify your internet connection is online: {message}");

            ShowOfflineModeMessage();
        }

        private static void ShowOfflineModeMessage()
        {
            if (_onlineMode)
            {
                _onlineMode = false;

                Logger.Warning?.Print(LogClass.Application, "Unable to connect to the Amiibo API server. The Amiibo service will run in offline mode until a connection is made.");
            }
        }

        private static void ShowOnlineModeMessage()
        {
            if (!_onlineMode)
            {
                _onlineMode = true;

                Logger.Info?.Print(LogClass.Application, "Successfully connected to the Amiibo API server.");
            }
        }

        public struct AmiiboJson
        {
            [JsonPropertyName("amiibo")]
            public List<AmiiboApi> Amiibo { get; set; }
            [JsonPropertyName("lastUpdated")]
            public DateTime LastUpdated { get; set; }
        }

        public struct AmiiboApi
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("head")]
            public string Head { get; set; }
            [JsonPropertyName("tail")]
            public string Tail { get; set; }
            [JsonPropertyName("image")]
            public string Image { get; set; }
            [JsonPropertyName("amiiboSeries")]
            public string AmiiboSeries { get; set; }
            [JsonPropertyName("character")]
            public string Character { get; set; }
            [JsonPropertyName("gameSeries")]
            public string GameSeries { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("release")]
            public Dictionary<string, string> Release { get; set; }

            [JsonPropertyName("gamesSwitch")]
            public List<AmiiboApiGamesSwitch> GamesSwitch { get; set; }
        }

        public class AmiiboApiGamesSwitch
        {
            [JsonPropertyName("amiiboUsage")]
            public List<AmiiboApiUsage> AmiiboUsage { get; set; }
            [JsonPropertyName("gameID")]
            public List<string> GameId { get; set; }
            [JsonPropertyName("gameName")]
            public string GameName { get; set; }
        }

        public class AmiiboApiUsage
        {
            [JsonPropertyName("Usage")]
            public string Usage { get; set; }
            [JsonPropertyName("write")]
            public bool Write { get; set; }
        }
    }
}
