using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class AutoDownloadableContentLoader
    {
        public readonly List<ApplicationData> Applications;

        public AutoDownloadableContentLoader(List<ApplicationData> applications)
        {
            Applications = applications;
        }

        private Dictionary<string, string> GetDlcsWithGameName()
        {
            //Searches for the files in the Global Dlcs folder, from settings, and puts their path and titlename (from filename) in a dictionary.
            return Directory.GetFiles(ConfigurationState.Instance.System.GlobalDlcPath)
                .ToDictionary(x => x, y => Path.GetFileName(y).Split(new[] { "[" }, StringSplitOptions.RemoveEmptyEntries)
                .First()
                .Trim());
        }

        public async Task AutoLoadDlcsAsync(ApplicationData application, Func<string, ulong, Task> addDlcs)
        {
            char[] bannedSymbols = { '.', ',', ':', ';', '>', '<', '\'', '\"', };
            Dictionary<char,char> replaceChars = new Dictionary<char, char>()
            {
                {'Ⅴ','V' }
            }; //This Dictionary replaces signs needed but in different encoding. example Shin Megami Tensei V 

            string gameTitle = string.Join("", application.TitleName.Split(bannedSymbols))
                .Replace(replaceChars.First().Key,
                         replaceChars.First().Value)
                .ToUpper()
                .Trim();

            foreach (KeyValuePair<string, string> dlcWithGameTitle in GetDlcsWithGameName().Where(titleDlc => titleDlc.Value.ToUpper() == gameTitle).ToList())
            {
                await addDlcs(dlcWithGameTitle.Key, ulong.Parse(application.TitleId, NumberStyles.HexNumber));
            }
        }
    }
}