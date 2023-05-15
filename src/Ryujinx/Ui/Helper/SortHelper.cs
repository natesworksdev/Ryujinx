using Gtk;
using Ryujinx.Common.Utilities;
using System;

namespace Ryujinx.Ui.Helper
{
    static class SortHelper
    {
        public static int TimePlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            TimeSpan aTimeSpan = ValueFormatUtils.ParseTimeSpan(model.GetValue(a, 5).ToString());
            TimeSpan bTimeSpan = ValueFormatUtils.ParseTimeSpan(model.GetValue(b, 5).ToString());

            return TimeSpan.Compare(aTimeSpan, bTimeSpan);
        }

        public static int LastPlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            DateTime aDateTime = ValueFormatUtils.ParseDateTime(model.GetValue(a, 6).ToString());
            DateTime bDateTime = ValueFormatUtils.ParseDateTime(model.GetValue(b, 6).ToString());

            return DateTime.Compare(aDateTime, bDateTime);
        }

        public static int FileSizeSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            long aSize = ValueFormatUtils.ParseFileSize(model.GetValue(a, 8).ToString());
            long bSize = ValueFormatUtils.ParseFileSize(model.GetValue(b, 8).ToString());

            if (aSize == bSize)
            {
                return 0;
            }

            return (aSize < bSize) ? -1 : 1;
        }
    }
}
