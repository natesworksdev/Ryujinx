using System;
using System.Text.Json.Serialization;

namespace Ryujinx.Ui.App.Common
{
    public class ApplicationMetadata
    {
        public string Title { get; set; }
        public bool   Favorite   { get; set; }
        public double TimePlayed { get; set; }

        [JsonPropertyName("last_played_utc")]
        public DateTime? LastPlayed { get; set; } = null;

        [JsonPropertyName("last_played")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string LastPlayedOld { get; set; }

        public string HostPath { get; set; }
        public string FileType { get; set; }
        public string TitleId { get; set; }
        public string Developer { get; set; }
        public string Version { get; set; }
        public byte[] AppIcon { get; set; }
        public double FileSize { get; set; }
    }
}