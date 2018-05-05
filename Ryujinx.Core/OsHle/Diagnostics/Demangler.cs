using System;

namespace Ryujinx.Core.OsHle.Diagnostics {
    static class Demangler {
        public static string ReadName(string mangled)
        {
            string result = null;
            string charCountTemp = null;
            int charCount = 0;
            foreach(var chr in mangled)
            {
                if (charCount == 0)
                {
                    if (charCountTemp == null)
                    {
                        if (chr == 'r' || chr == 'V' || chr == 'K')
                        {
                            continue;
                        }
                        if (chr == 'E')
                        {
                            break;
                        }
                    }
                    if (Char.IsDigit(chr))
                    {
                        charCountTemp += chr;
                    }
                    else
                    {
                        if (!int.TryParse(charCountTemp, out charCount))
                        {
                            return mangled;
                        }
                        result += chr;
                        charCount--;
                        charCountTemp = null;
                    }
                }
                else
                {
                    result += chr;
                    charCount--;
                    if (charCount == 0)
                    {
                        result += "::";
                    }
                }
            }
            if (result == null)
            {
                return mangled;
            }
            return result.Substring(0, result.Length - 2);
        }
    }
}