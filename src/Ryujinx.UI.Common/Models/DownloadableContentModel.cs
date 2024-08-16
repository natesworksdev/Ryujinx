namespace Ryujinx.UI.Common.Models
{
    public class DownloadableContentModel
    {
        public ulong TitleId { get; }
        public string ContainerPath { get; }
        public string FullPath { get; }
        public bool Enabled { get; }
        public bool IsBundled { get; }

        public string FileName => System.IO.Path.GetFileName(ContainerPath);
        public string TitleIdStr => TitleId.ToString("X16");
        public ulong TitleIdBase => TitleId & ~0x1FFFUL;
        
        public DownloadableContentModel(ulong titleId, string containerPath, string fullPath, bool enabled)
        {
            TitleId = titleId;
            ContainerPath = containerPath;
            FullPath = fullPath;
            Enabled = enabled;
            IsBundled = System.IO.Path.GetExtension(containerPath)?.ToLower() == ".xci";
        }
    }
}
