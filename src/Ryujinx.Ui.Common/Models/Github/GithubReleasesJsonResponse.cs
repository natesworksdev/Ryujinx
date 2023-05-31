using System.Collections.Generic;

namespace Ryujinx.Ui.Common.Models.Github
{
    public sealed class GithubReleasesJsonResponse
    {
        public string Name { get; set; }
        public List<GithubReleaseAssetJsonResponse> Assets { get; set; }
    }
}