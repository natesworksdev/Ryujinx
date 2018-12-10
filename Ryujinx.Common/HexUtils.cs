using System;
using System.Text;

namespace Ryujinx.Common
{
    public static class HexUtils
    {
        private static readonly char[] HEX_CHARS = "0123456789ABCDEF".ToCharArray();

        private const int HEX_TABLE_COLUMN_WIDTH = 8;
        private const int HEX_TABLE_COLUMN_SPACE = 3;

        // Modified for Ryujinx
        // Original by Pascal Ganaye - CPOL License
        // https://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
        public static string HexTable(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            int bytesLength = bytes.Length;

            int firstHexColumn =
                  HEX_TABLE_COLUMN_WIDTH
                + HEX_TABLE_COLUMN_SPACE;

            int firstCharColumn = firstHexColumn
                + bytesPerLine * HEX_TABLE_COLUMN_SPACE
                + (bytesPerLine - 1) / HEX_TABLE_COLUMN_WIDTH
                + 2;

            int lineLength = firstCharColumn
                + bytesPerLine
                + Environment.NewLine.Length;

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();

            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;

            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HEX_CHARS[(i >> 28) & 0xF];
                line[1] = HEX_CHARS[(i >> 24) & 0xF];
                line[2] = HEX_CHARS[(i >> 20) & 0xF];
                line[3] = HEX_CHARS[(i >> 16) & 0xF];
                line[4] = HEX_CHARS[(i >> 12) & 0xF];
                line[5] = HEX_CHARS[(i >> 8) & 0xF];
                line[6] = HEX_CHARS[(i >> 4) & 0xF];
                line[7] = HEX_CHARS[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0)
                    {
                        hexColumn++;
                    }

                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HEX_CHARS[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HEX_CHARS[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                result.Append(line);
            }

            return result.ToString();
        }
    }
}
