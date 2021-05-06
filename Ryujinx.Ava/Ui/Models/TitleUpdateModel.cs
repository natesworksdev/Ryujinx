using LibHac.Ns;

namespace Ryujinx.Ava.Ui.Models
{
    public class TitleUpdateModel
    {
        public bool IsEnabled { get; set; }
        public bool IsNoUpdate { get; }
        public ApplicationControlProperty Control { get; }
        public string Path { get; }
        public string Label => IsNoUpdate ? "No Update" : $"Version {Control.DisplayVersion.ToString()} - {Path}";
        
        public TitleUpdateModel(ApplicationControlProperty control, string path, bool isNoUpdate = false)
        {
            Control    = control;
            Path       = path;
            IsNoUpdate = isNoUpdate;
        }
    }
}