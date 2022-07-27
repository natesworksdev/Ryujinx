namespace Ryujinx.Common.Configuration
{
    public struct DownloadableContentNca
    {
        public string Path    { get; set; }
        public ulong  TitleId { get; set; }
        public bool   Enabled { get; set; }
    }
}