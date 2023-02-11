using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.Ui.Common.Models.Github
{
    public class GithubReleasesJsonResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("assets")]
        public List<GithubReleaseAssetJsonResponse> Assets { get; set; }
    }
}