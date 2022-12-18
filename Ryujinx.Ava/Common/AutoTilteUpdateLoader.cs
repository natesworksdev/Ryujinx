using Ryujinx.Ui.App.Common;
using Ryujinx.Ui.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Ui.Controls
{
    internal class AutoTitleUpdateLoader
    {
        public List<ApplicationData> Applications { get; private set; }

        public AutoTitleUpdateLoader(List<ApplicationData> applications)
        {
            Applications = applications;
        }

        public async Task AutoLoadUpdatesAsync(ApplicationData application, Func<string, ulong, Task> addTitleUpdates)
        {
            //bannedSmbols clears out special symbols ex. not allowed in windows folder name
            //replaceChars replaces signs needed but in different encoding. example Shin Megami Tensei V 
            char[] bannedSymbols = { '.', ',', ':', ';', '>', '<', '\'', '\"', };
            Dictionary<char, char> replaceChars = new Dictionary<char, char>() 
            {
                {'Ⅴ','V' }
            }; 

            string gameTitle = string.Join("", application.TitleName.Split(bannedSymbols))
                .Replace(replaceChars.First().Key,
                         replaceChars.First().Value)
                .ToUpper()
                .Trim();

            //Loops through the Updates to the given gameTitle and adds them to the downloadableContent List
            foreach (KeyValuePair<string, string> updateWithGameTitle in GetTitleUpdatesWithGameName().Where(titleUpdate => titleUpdate.Value.ToUpper() == gameTitle).ToList())
            {
                await addTitleUpdates(updateWithGameTitle.Key, ulong.Parse(application.TitleId, NumberStyles.HexNumber));
            }
        }

        public Dictionary<string, string> GetTitleUpdatesWithGameName()
        {
            //Searches for the files in the Global Updates folder and puts their path and titlename (from folder) in a dictionary.
            return Directory.GetFiles(ConfigurationState.Instance.System.GlobalTitleUpdatePath)
                .ToDictionary(x => x, y => Path.GetFileName(y).Split(new[] { "[" }, StringSplitOptions.RemoveEmptyEntries)
                .First()
                .Trim());
        }
    }
}