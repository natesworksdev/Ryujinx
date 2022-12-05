using Gtk;
using System;

namespace Ryujinx.Ui.Helper
{
    static class SortHelper
    {
        public static int TimePlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 5).ToString();
            string bValue = model.GetValue(b, 5).ToString();

            if (aValue.Length > 7 && aValue[^7..] == "minutes")
            {
                aValue = (float.Parse(aValue[0..^5]) * 60).ToString();
            }
            else if (aValue.Length > 5 && aValue[^5..] == "hours")
            {
                aValue = (float.Parse(aValue[0..^4]) * 3600).ToString();
            }
            else if (aValue.Length > 4 && aValue[^4..] == "days")
            {
                aValue = (float.Parse(aValue[0..^5]) * 86400).ToString();
            }
            else
            {
                aValue = aValue[0..^8];
            }

            if (bValue.Length > 7 && bValue[^7..] == "minutes")
            {
                bValue = (float.Parse(bValue[0..^5]) * 60).ToString();
            }
            else if (bValue.Length > 5 && bValue[^5..] == "hours")
            {
                bValue = (float.Parse(bValue[0..^4]) * 3600).ToString();
            }
            else if (bValue.Length > 4 && bValue[^4..] == "days")
            {
                bValue = (float.Parse(bValue[0..^5]) * 86400).ToString();
            }
            else
            {
                bValue = bValue[0..^8];
            }

            if (float.Parse(aValue) > float.Parse(bValue))
            {
                return -1;
            }
            else if (float.Parse(bValue) > float.Parse(aValue))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public static int LastPlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 6).ToString();
            string bValue = model.GetValue(b, 6).ToString();

            if (aValue == "Never")
            {
                aValue = DateTime.UnixEpoch.ToString();
            }

            if (bValue == "Never")
            {
                bValue = DateTime.UnixEpoch.ToString();
            }

            return DateTime.Compare(DateTime.Parse(bValue), DateTime.Parse(aValue));
        }

        public static int FileSizeSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 8).ToString();
            string bValue = model.GetValue(b, 8).ToString();

            if (aValue[^3..] == "GiB")
            {
                aValue = (float.Parse(aValue[0..^3]) * 1024).ToString();
            }
            else
            {
                aValue = aValue[0..^3];
            }

            if (bValue[^3..] == "GiB")
            {
                bValue = (float.Parse(bValue[0..^3]) * 1024).ToString();
            }
            else
            {
                bValue = bValue[0..^3];
            }

            if (float.Parse(aValue) > float.Parse(bValue))
            {
                return -1;
            }
            else if (float.Parse(bValue) > float.Parse(aValue))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}