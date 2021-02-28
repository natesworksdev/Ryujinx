using System.Collections.Generic;

namespace Ryujinx.Common.Configuration.ConfigurationStateSection
{
    /// <summary>
    /// UI configuration section
    /// </summary>
    public class UiSection
    {
        public class Columns
        {
            public ReactiveObject<bool> FavColumn { get; private set; }
            public ReactiveObject<bool> IconColumn { get; private set; }
            public ReactiveObject<bool> AppColumn { get; private set; }
            public ReactiveObject<bool> DevColumn { get; private set; }
            public ReactiveObject<bool> VersionColumn { get; private set; }
            public ReactiveObject<bool> TimePlayedColumn { get; private set; }
            public ReactiveObject<bool> LastPlayedColumn { get; private set; }
            public ReactiveObject<bool> FileExtColumn { get; private set; }
            public ReactiveObject<bool> FileSizeColumn { get; private set; }
            public ReactiveObject<bool> PathColumn { get; private set; }

            public Columns()
            {
                FavColumn = new ReactiveObject<bool>();
                IconColumn = new ReactiveObject<bool>();
                AppColumn = new ReactiveObject<bool>();
                DevColumn = new ReactiveObject<bool>();
                VersionColumn = new ReactiveObject<bool>();
                TimePlayedColumn = new ReactiveObject<bool>();
                LastPlayedColumn = new ReactiveObject<bool>();
                FileExtColumn = new ReactiveObject<bool>();
                FileSizeColumn = new ReactiveObject<bool>();
                PathColumn = new ReactiveObject<bool>();
            }
        }

        public class ColumnSortSettings
        {
            public ReactiveObject<int> SortColumnId { get; private set; }
            public ReactiveObject<bool> SortAscending { get; private set; }

            public ColumnSortSettings()
            {
                SortColumnId = new ReactiveObject<int>();
                SortAscending = new ReactiveObject<bool>();
            }
        }

        /// <summary>
        /// Used to toggle columns in the GUI
        /// </summary>
        public Columns GuiColumns { get; private set; }

        /// <summary>
        /// Used to configure column sort settings in the GUI
        /// </summary>
        public ColumnSortSettings ColumnSort { get; private set; }

        /// <summary>
        /// A list of directories containing games to be used to load games into the games list
        /// </summary>
        public ReactiveObject<List<string>> GameDirs { get; private set; }

        /// <summary>
        /// Enable or disable custom themes in the GUI
        /// </summary>
        public ReactiveObject<bool> EnableCustomTheme { get; private set; }

        /// <summary>
        /// Path to custom GUI theme
        /// </summary>
        public ReactiveObject<string> CustomThemePath { get; private set; }

        /// <summary>
        /// Start games in fullscreen mode
        /// </summary>
        public ReactiveObject<bool> StartFullscreen { get; private set; }

        public UiSection()
        {
            GuiColumns = new Columns();
            ColumnSort = new ColumnSortSettings();
            GameDirs = new ReactiveObject<List<string>>();
            EnableCustomTheme = new ReactiveObject<bool>();
            CustomThemePath = new ReactiveObject<string>();
            StartFullscreen = new ReactiveObject<bool>();
        }
    }
}
