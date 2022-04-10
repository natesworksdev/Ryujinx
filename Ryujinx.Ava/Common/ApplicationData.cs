using LibHac.Common;
using LibHac.Ns;
using Ryujinx.Ava.Ui.Models;

namespace Ryujinx.Ava.Common
{
    public class ApplicationData
    {
        private bool _favorite;

        public bool Favorite
        {
            get => _favorite; set
            {
                _favorite = value;

                if (!string.IsNullOrWhiteSpace(TitleId))
                {
                    ApplicationLibrary.LoadAndSaveMetaData(TitleId, appMetadata =>
                    {
                        appMetadata.Favorite = value;
                    });
                }
            }
        }

        public byte[] Icon { get; set; }
        public string TitleName { get; set; }
        public string TitleId { get; set; }
        public string Developer { get; set; }
        public string Version { get; set; }
        public string TimePlayed { get; set; }
        public string LastPlayed { get; set; }
        public string FileExtension { get; set; }
        public string FileSize { get; set; }
        public string Path { get; set; }
        public BlitStruct<ApplicationControlProperty> ControlHolder { get; set; }

        public string ApplicationName => $"{TitleName}\n{TitleId.ToUpper()}";
    }
}