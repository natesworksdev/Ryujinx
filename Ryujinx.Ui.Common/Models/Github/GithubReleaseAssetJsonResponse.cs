using System.Text.Json.Serialization;

namespace Ryujinx.Ui.Common.Models.Github
{
    public class GithubReleaseAssetJsonResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}