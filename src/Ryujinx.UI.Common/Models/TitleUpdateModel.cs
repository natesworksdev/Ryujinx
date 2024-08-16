namespace Ryujinx.UI.Common.Models
{
    public class TitleUpdateModel
    {
        public ulong TitleId { get; }
        public ulong Version { get; }
        public string DisplayVersion { get; }
        public string Path { get; }
        public bool IsBundled { get; }
        
        public string TitleIdStr => TitleId.ToString("X16");

        public TitleUpdateModel(ulong titleId, ulong version, string displayVersion, string path)
        {
            TitleId = titleId;
            Version = version;
            DisplayVersion = displayVersion;
            Path = path;
            IsBundled = System.IO.Path.GetExtension(path)?.ToLower() == ".xci";
        }
    }
}
