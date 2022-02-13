using System.Diagnostics;
using System.Globalization;

namespace Ryujinx.HLE.Debugger
{
    class StringStream
    {
        private readonly string Data;
        private int Position;

        public StringStream(string s)
        {
            Data = s;
        }

        public char ReadChar()
        {
            return Data[Position++];
        }

        public string ReadUntil(char needle)
        {
            int needlePos = Data.IndexOf(needle, Position);

            if (needlePos == -1)
            {
                needlePos = Data.Length;
            }

            string result = Data.Substring(Position, needlePos - Position);
            Position = needlePos + 1;
            return result;
        }

        public string ReadLength(int len)
        {
            string result = Data.Substring(Position, len);
            Position += len;
            return result;
        }

        public string ReadRemaining()
        {
            string result = Data.Substring(Position);
            Position = Data.Length;
            return result;
        }

        public ulong ReadRemainingAsHex()
        {
            return ulong.Parse(ReadRemaining(), NumberStyles.HexNumber);
        }

        public ulong ReadUntilAsHex(char needle)
        {
            return ulong.Parse(ReadUntil(needle), NumberStyles.HexNumber);
        }

        public ulong ReadLengthAsHex(int len)
        {
            return ulong.Parse(ReadLength(len), NumberStyles.HexNumber);
        }

        public ulong ReadLengthAsLEHex(int len)
        {
            Debug.Assert(len % 2 == 0);

            ulong result = 0;
            int pos = 0;
            while (pos < len)
            {
                result += ReadLengthAsHex(2) << (4 * pos);
                pos += 2;
            }
            return result;
        }

        public ulong? ReadRemainingAsThreadUid()
        {
            string s = ReadRemaining();
            return s == "-1" ? null : ulong.Parse(s, NumberStyles.HexNumber);
        }

        public bool ConsumePrefix(string prefix)
        {
            if (Data.Substring(Position).StartsWith(prefix))
            {
                Position += prefix.Length;
                return true;
            }
            return false;
        }

        public bool ConsumeRemaining(string match)
        {
            if (Data.Substring(Position) == match)
            {
                Position += match.Length;
                return true;
            }
            return false;
        }

        public bool IsEmpty()
        {
            return Position >= Data.Length;
        }
    }
}