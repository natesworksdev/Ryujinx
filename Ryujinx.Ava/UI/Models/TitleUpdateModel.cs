using LibHac.Ns;
using Ryujinx.Ava.Common.Locale;

namespace Ryujinx.Ava.UI.Models
{
    public class TitleUpdateModel
    {
        public ApplicationControlProperty Control { get; }
        public string Path { get; }

        public string Label
        {
            get
            {
                LocaleManager.Instance.UpdateDynamicValue(LocaleKeys.TitleUpdateVersionLabel, Control.DisplayVersionString.ToString());

                return LocaleManager.Instance[LocaleKeys.TitleUpdateVersionLabel];
            }
        }

        public TitleUpdateModel(ApplicationControlProperty control, string path)
        {
            Control = control;
            Path = path;
        }
    }
}