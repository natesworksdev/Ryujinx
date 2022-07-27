using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration
{
    public struct DownloadableContentContainer
    {
        public string Path { get; set; }
        [JsonPropertyName("dlc_nca_list")]
        public List<DownloadableContentNca> DownloadableContentNcaList { get; set; }
    }
}