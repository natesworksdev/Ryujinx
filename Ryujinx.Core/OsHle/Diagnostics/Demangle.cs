using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ryujinx.Core.OsHle.Diagnostics
{
    public static class Demangle
    {
        private static readonly Dictionary<string, string> BuiltinTypes = new Dictionary<string, string>
        {
            { "v", "void" },
            { "w", "wchar_t" },
            { "b", "bool" },
            { "c", "char" },
            { "a", "signed char" },
            { "h", "unsigned char" },
            { "s", "short" },
            { "t", "unsigned short" },
            { "i", "int" },
            { "j", "unsigned int" },
            { "l", "long" },
            { "m", "unsigned long" },
            { "x", "long long, __int64" },
            { "y", "unsigned long long, __int64" },
            { "n", "__int128" },
            { "o", "unsigned __int128" },
            { "f", "float" },
            { "d", "double" },
            { "e", "long double, __float80" },
            { "g", "__float128" },
            { "z", "ellipsis" },
            { "Dd", "__iec559_double" },
            { "De", "__iec559_float128" },
            { "Df", "__iec559_float" },
            { "Dh", "__iec559_float16" },
            { "Di", "char32_t" },
            { "Ds", "char16_t" },
            { "Da", "decltype(auto)" },
            { "Dn", "std::nullptr_t" },
        };

        private static readonly Dictionary<string, string> SubstitutionExtra = new Dictionary<string, string>
        {
            {"Sa", "std::allocator"},
            {"Sb", "std::basic_string"},
            {"Ss", "std::basic_string<char, ::std::char_traits<char>, ::std::allocator<char>>"},
            {"Si", "std::basic_istream<char, ::std::char_traits<char>>"},
            {"So", "std::basic_ostream<char, ::std::char_traits<char>>"},
            {"Sd", "std::basic_iostream<char, ::std::char_traits<char>>"}
        };

        private static int FromBase36(string encoded)
        {
            string base36 = "0123456789abcdefghijklmnopqrstuvwxyz";
            char[] reversedEncoded = encoded.ToLower().ToCharArray().Reverse().ToArray();
            int result = 0;
            for (int i = 0; i < reversedEncoded.Length; i++)
            {
                char c = reversedEncoded[i];
                int value = base36.IndexOf(c);
                if (value == -1)
                    return -1;
                result += value * (int)Math.Pow(36, i);
            }
            return result;
        }

        private static string GetCompressedValue(string compression, List<string> compressionData, out int pos)
        {
            string res = null;
            bool canHaveUnqualifiedName = false;
            pos = -1;
            if (compressionData.Count == 0 || !compression.StartsWith("S"))
                return null;

            string temp = null;
            if (compression.Length > 2 && BuiltinTypes.TryGetValue(compression.Substring(0, 2), out temp))
            {
                pos = 2;
                res = temp;
                compression = compression.Substring(2);
            }
            else if (compression.StartsWith("St"))
            {
                pos = 2;
                canHaveUnqualifiedName = true;
                res = "std";
                compression = compression.Substring(2);
            }
            else if (compression.StartsWith("S_"))
            {
                pos = 2;
                res = compressionData[0];
                canHaveUnqualifiedName = true;
                compression = compression.Substring(2);
            }
            else
            {
                int id = -1;
                int underscorePos = compression.IndexOf('_');
                if (underscorePos == -1)
                    return null;
                string partialId = compression.Substring(1, underscorePos - 1);

                id = FromBase36(partialId);
                if (id == -1 || compressionData.Count <= (id + 1))
                {
                    return null;
                }
                res = compressionData[id + 1];
                pos = partialId.Length + 1;
                canHaveUnqualifiedName= true;
                compression = compression.Substring(pos);
            }
            if (res != null)
            {
                if (canHaveUnqualifiedName)
                {
                    int tempPos = -1;
                    List<string> type = ReadName(compression, compressionData, out tempPos);
                    if (tempPos != -1 && type != null)
                    {
                        pos  += tempPos;
                        res = res + "::" + type[type.Count - 1];
                    }
                }
            }
            return res;
        }

        public static List<string> ReadName(string mangled, List<string> compressionData, out int pos)
        {
            List<string> res = new List<string>();
            string charCountTemp = null;
            int charCount = 0;
            int i;

            pos = -1;
            for (i = 0; i < mangled.Length; i++)
            {
                char chr = mangled[i];
                if (charCountTemp == null)
                {
                    if (ReadCVQualifiers(chr) != null)
                    {
                        continue;
                    }
                    if (chr == 'S')
                    {
                        string data = GetCompressedValue(mangled.Substring(i), compressionData, out pos);
                        if (pos == -1)
                        {
                            return null;
                        }
                        if (res.Count == 0)
                            res.Add(data);
                        else
                            res.Add(res[res.Count - 1] + "::" + data);
                        i += pos;
                        if (mangled[i] == 'E')
                        {
                            break;
                        }
                        continue;
                    }
                    else if (chr == 'E')
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
                        return null;
                    }
                    string demangledPart = mangled.Substring(i, charCount);
                    if (res.Count == 0)
                        res.Add(demangledPart);
                    else
                        res.Add(res[res.Count - 1] + "::" + demangledPart);
                    i = i + charCount - 1;
                    charCount = 0;
                    charCountTemp = null;
                }
            }
            if (res.Count == 0)
            {
                return null;
            }
            pos = i;
            return res;
        }

        public static string ReadBuiltinType(string mangledType, out int pos)
        {
            string res = null;
            string temp;
            pos = -1;
            temp = mangledType[0].ToString();
            if (!BuiltinTypes.TryGetValue(temp, out res))
            {
                if (mangledType.Length >= 2)
                {
                    temp = mangledType.Substring(0, 2);
                    BuiltinTypes.TryGetValue(temp, out res);
                }
            }
            if (res != null)
                pos = temp.Length;
            return res;
        }

        private static string ReadCVQualifiers(char qualifier)
        {
            if (qualifier == 'r')
                return "restricted";
            else if (qualifier == 'V')
                return "volatile";
            else if (qualifier == 'K')
                return "const";
            else if (qualifier == 'R')
                return "&";
            else if (qualifier == 'O')
                return "&&";
            return null;
        }

        private static string ReadRefQualifiers(char qualifier)
        {
            if (qualifier == 'R')
                return "&";
            else if (qualifier == 'O')
                return "&&";
            return null;
        }

        private static string ReadSpecialQualifiers(char qualifier)
        {
            if (qualifier == 'P')
                return "*";
            else if (qualifier == 'C')
                return "complex";
            else if (qualifier == 'G')
                return "imaginary";
            return null;
        }

        public static List<string> ReadParameters(string mangledParams, List<string> compressionData, out int pos)
        {
            List<string> res = new List<string>();
            int i = 0;
            pos = -1;

            string temp = null;
            string temp2 = null;
            for (i = 0; i < mangledParams.Length; i++)
            {
                char chr = mangledParams[i];
                string part = mangledParams.Substring(i);

                // Try to read qualifiers
                temp2 = ReadCVQualifiers(chr);
                if (temp2 != null)
                {
                    temp = temp2 + " " + temp;
                    compressionData.Add(temp);

                    // need more data
                    continue;
                }

                temp2 = ReadRefQualifiers(chr);
                if (temp2 != null)
                {
                    temp = temp +  temp2;
                    compressionData.Add(temp);

                    // need more data
                    continue;
                }

                temp2 = ReadSpecialQualifiers(chr);
                if (temp2 != null)
                {
                    temp = temp + temp2;

                    // need more data
                    continue;
                }

                // TODO: extended-qualifier?

                if (part.StartsWith("S"))
                {
                    temp2 = GetCompressedValue(part, compressionData, out pos);
                    if (pos != -1 && temp2 != null)
                    {
                        i += pos;
                        temp = temp2 + temp;
                        res.Add(temp);
                        compressionData.Add(temp);
                        temp = null;
                        continue;
                    }
                    pos = -1;
                    return null;
                }
                else if (part.StartsWith("N"))
                {
                    part = part.Substring(1);
                    List<string> name = ReadName(part, compressionData, out pos);
                    if (pos != -1 && name != null)
                    {
                        i += pos + 1;
                        temp = name[name.Count - 1] + " " + temp;
                        res.Add(temp);
                        compressionData.Add(temp);
                        temp = null;
                        continue;
                    }
                }

                // Try builting
                temp2 = ReadBuiltinType(part, out pos);
                if (pos == -1)
                {
                    return null;
                }
                if (temp != null)
                    temp = temp2 + " " + temp;
                else
                    temp = temp2;
                res.Add(temp);
                compressionData.Add(temp);
                temp = null;
                i = i + pos -1;
            }
            pos = i;
            return res;
        }

        public static string Parse(string originalMangled)
        {
            string mangled = originalMangled;
            List<string> compressionData = new List<string>();
            string res = null;
            int pos = 0;

            // We asume that we start with a function name
            // TODO: support special names
            if (mangled.StartsWith("_ZN"))
            {
                mangled = mangled.Substring(3);
                compressionData = ReadName(mangled, compressionData, out pos);
                if (pos == -1)
                    return originalMangled;
                res = compressionData[compressionData.Count - 1];

                compressionData.Remove(res);
                mangled = mangled.Substring(pos + 1);

                // more data? maybe not a data name so...
                if (mangled != String.Empty)
                {
                    List<string> parameters = ReadParameters(mangled, compressionData, out pos);
                    // parameters parsing error, we return the original data to avoid information loss.
                    if (pos == -1)
                        return originalMangled;
                    res += "(";
                    res += String.Join(", ", parameters);
                    res += ")";
                }
                return res;
            }
            return originalMangled;
        }
    }
}