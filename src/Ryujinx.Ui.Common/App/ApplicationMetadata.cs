using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Ryujinx.Ui.App.Common
{
    public class ApplicationMetadata
    {
        public string Title { get; set; }
        public bool   Favorite   { get; set; }
        public double TimePlayed { get; set; }
        public DateTime? LastPlayed { get; set; } = null;
    }
}