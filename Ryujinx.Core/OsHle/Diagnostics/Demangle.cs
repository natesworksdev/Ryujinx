using System;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Core.OsHle.Diagnostics
{
    public static class Demangle
    {
        /*
          <builtin-type> ::= v	# void
		 ::= w	# wchar_t
		 ::= b	# bool
		 ::= c	# char
		 ::= a	# signed char
		 ::= h	# unsigned char
		 ::= s	# short
		 ::= t	# unsigned short
		 ::= i	# int
		 ::= j	# unsigned int
		 ::= l	# long
		 ::= m	# unsigned long
		 ::= x	# long long, __int64
		 ::= y	# unsigned long long, __int64
		 ::= n	# __int128
		 ::= o	# unsigned __int128
		 ::= f	# float
		 ::= d	# double
		 ::= e	# long double, __float80
		 ::= g	# __float128
		 ::= z	# ellipsis
                 ::= Dd # IEEE 754r decimal floating point (64 bits)
                 ::= De # IEEE 754r decimal floating point (128 bits)
                 ::= Df # IEEE 754r decimal floating point (32 bits)
                 ::= Dh # IEEE 754r half-precision floating point (16 bits)
                 ::= DF <number> _ # ISO/IEC TS 18661 binary floating point type _FloatN (N bits)
                 ::= Di # char32_t
                 ::= Ds # char16_t
                 ::= Da # auto
                 ::= Dc # decltype(auto)
                 ::= Dn # std::nullptr_t (i.e., decltype(nullptr))
         */
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
        public static List<string> ReadName(string mangled, out int pos, List<string> compressionData)
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
                temp = mangledType.Substring(0, 2);
                BuiltinTypes.TryGetValue(temp, out res);
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

        public static List<string> ReadParameters(string mangledParams, out int pos)
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
                    continue;
                }

                temp2 = ReadRefQualifiers(chr);
                if (temp2 != null)
                {
                    temp = temp + temp2;
                    continue;
                }

                temp2 = ReadSpecialQualifiers(chr);
                if (temp2 != null)
                {
                    Console.WriteLine(temp);
                    temp = temp + temp2;
                    continue;
                }

                // TODO: extended-qualifier?

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
                temp = null;
                i = i + pos -1;
            }
            pos = i;
            return res;
        }

        public static string ReadNameString(string mangled, out int pos)
        {
            List<string> name = ReadName(mangled, out pos, new List<string>());
            if (pos == -1 || name == null || name.Count == 0)
            {
                return mangled;
            }
            foreach (var entry in name)
            {
                Console.WriteLine(entry);
            }

            return name[name.Count - 1];
        }

        /**
          <mangled-name> ::= _Z <encoding>
            <encoding> ::= <function name> <bare-function-type>
                   ::= <data name>
                   ::= <special-name>
         */
        public static string Parse(string mangled)
        {
            Console.WriteLine("Mangled: " + mangled);
            List<string> compressionData = new List<string>();
            string res = null;
            int pos = 0;

            // We asume that we start with a function name
            // TODO: support special names
            if (mangled.StartsWith("_ZN"))
            {
                mangled = mangled.Substring(3);
                compressionData = ReadName(mangled, out pos, compressionData);
                if (pos == -1)
                    return mangled;
                res = compressionData[compressionData.Count - 1];

                compressionData.Remove(res);
                mangled = mangled.Substring(pos + 1);

                // more data? maybe not a data name so...
                if (mangled != String.Empty)
                {
                    List<string> parameters = ReadParameters(mangled, out pos);
                    // parameters parsing error, we return the original data to avoid information loss.
                    if (pos == -1)
                        return mangled;
                    res += "(";
                    res += String.Join(", ", parameters);                    
                    res += ")";
                }
                return res;
            }
            return mangled;
        }
    }
}