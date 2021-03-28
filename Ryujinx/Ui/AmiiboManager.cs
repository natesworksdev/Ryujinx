using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
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

        private static HttpClient                     _httpClient;
        private static string                         _amiiboJsonPath;
        private static AmiiboJson                     _amiiboJson;
        private static List<AmiiboApi>                _amiiboApis;
        private static Dictionary<string, Gdk.Pixbuf> _amiiboPreviews;
        private static bool                           _initialized;

        public static List<AmiiboApi>                 AmiiboApis { get => _amiiboApis; }

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

                if (File.Exists(_amiiboJsonPath))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            string amiiboJsonString = File.ReadAllText(_amiiboJsonPath);
                            await LoadAmiiboJson(amiiboJsonString);

                            // Update if there is Amiibo data newer than the current version.
                            await UpdateAmiibos();
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning?.Print(LogClass.Application, $"Failed to read Amiibo JSON data: {ex.Message}");

                            _amiiboApis = new List<AmiiboApi>();
                        }
                    });
                }
                else
                {
                    Task.Run(async () =>
                    {
                        await UpdateAmiibos();
                    });
                }

                _initialized = true;
            }
        }

        private static async Task LoadAmiiboJson(string amiiboJsonString)
        {
            await Task.Run(() =>
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
            });
        }

        /// <summary>
        /// Checks to see that there is a new version of amiibo data available for download.
        /// </summary>
        /// <returns><b>True</b> New data was downloaded and installed successfully.<br>
        /// </br><b>False</b> The current version of the data is already up-to-date or an error occurred.</returns>
        public static async Task<bool> UpdateAmiibos()
        {
            Logger.Info?.Print(LogClass.Application, "Checking for Amiibo Updates..");

            string amiiboJsonString;

            if (File.Exists(_amiiboJsonPath))
            {
                if (await NeedsUpdate(_amiiboJson.LastUpdated))
                {
                    amiiboJsonString = await DownloadAmiiboJson();

                    // Don't overwrite existing configuration. != will be faster than !.Equals since DEFAULT_JSON would be assigned by reference.
                    if (amiiboJsonString != DEFAULT_JSON)
                    {
                        _ = LoadAmiiboJson(amiiboJsonString);

                        return true;
                    }

                    return false;
                }
                else
                {
                    Logger.Info?.Print(LogClass.Application, "Your Amiibos are already up to date!");

                    return false;
                }
            }
            else
            {
                try
                {
                    amiiboJsonString = await DownloadAmiiboJson();
                    
                    _ = LoadAmiiboJson(amiiboJsonString);

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
            if(_amiiboPreviews.ContainsKey(imageUrl))
            {
                return _amiiboPreviews[imageUrl];
            }
            else
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
        }

        private static async Task<bool> NeedsUpdate(DateTime oldLastModified)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://amiibo.ryujinx.org/"));

                if (response.IsSuccessStatusCode)
                {
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

        private static async Task<string> DownloadAmiiboJson()
        {
            Logger.Info?.Print(LogClass.Application, "Downloading newer version of amiibo data..");

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("https://amiibo.ryujinx.org/");

                if (response.IsSuccessStatusCode)
                {
                    string amiiboJsonString = await response.Content.ReadAsStringAsync();

                    AmiiboJson amiiboJson = JsonSerializer.Deserialize<AmiiboJson>(amiiboJsonString);

                    DateTime lastUpdated = amiiboJson.LastUpdated;
                    amiiboJson.LastUpdated = new DateTime(lastUpdated.Year, lastUpdated.Month, lastUpdated.Day, lastUpdated.Hour, lastUpdated.Minute, lastUpdated.Second);
                    amiiboJsonString = JsonSerializer.Serialize<AmiiboJson>(amiiboJson);

                    using (FileStream dlcJsonStream = File.Create(_amiiboJsonPath, 4096, FileOptions.WriteThrough))
                    {
                        await dlcJsonStream.WriteAsync(Encoding.UTF8.GetBytes(amiiboJsonString));
                    }

                    Logger.Info?.Print(LogClass.Application, "Amiibo data updated successfully!");

                    return amiiboJsonString;
                }
                else
                {
                    Logger.Warning?.Print(LogClass.Application, "An error occured while fetching informations from the Amiibo API.");
                }
            }
            catch (Exception ex)
            {
                ShowAmiiboServiceWarning(ex.Message);
            }

            return DEFAULT_JSON;
        }

        private static void ShowAmiiboServiceWarning(string message)
        {
            Logger.Warning?.Print(LogClass.Application, $"Unable to connect to Amiibo API server. The service may be down or you may need to verify your internet connection is online: {message}");
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
