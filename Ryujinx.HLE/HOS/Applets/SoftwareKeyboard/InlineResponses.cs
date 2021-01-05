using System.IO;
using System.Text;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class InlineResponses
    {
        private static uint WriteString(string text, BinaryWriter writer, int maxSize, Encoding encoding)
        {
            var bytes = encoding.GetBytes(text);
            writer.Write(bytes);
            writer.Seek(maxSize - bytes.Length, SeekOrigin.Current);
            writer.Write((uint)text.Length); // String size
            return (uint)bytes.Length;
        }

        private static void WriteStringWithCursor(string text, BinaryWriter writer, int maxSize, Encoding encoding)
        {
            uint cursor = WriteString(text, writer, maxSize, encoding);
            writer.Write(cursor); // Cursor position
        }

        public static byte[] FinishedInitialize(uint state = 2)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x1]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x0); // Reply code
                writer.Write((byte)1); // Data
                return stream.ToArray();
            }
        }

        public static byte[] Default(uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x0]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x1); // Reply code
                return stream.ToArray();
            }
        }

        public static byte[] ChangedString(string text, uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x3FC]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x2); // Reply code
                writer.Write((byte)1); // Data
                return stream.ToArray();
            }
        }

        public static byte[] MovedCursor(string text, uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x3F4]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x3); // Reply code
                WriteStringWithCursor(text, writer, 0x3EC, Encoding.Unicode);
                return stream.ToArray();
            }
        }

        public static byte[] MovedTab(uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x3F4]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x4); // Reply code
                writer.Write((byte)1); // Data
                return stream.ToArray();
            }
        }

        public static byte[] DecidedEnter(string text, uint state = 4)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x3F0]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x5); // Reply code
                WriteString(text, writer, 0x3EC, Encoding.Unicode);
                return stream.ToArray();
            }
        }

        public static byte[] DecidedCancel(uint state = 4)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x0]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x6); // Reply code
                return stream.ToArray();
            }
        }

        public static byte[] ChangedStringUtf8(string text, uint state = 3)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x7E4]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x7); // Reply code
                writer.Write((byte)1); // Data
                return stream.ToArray();
            }
        }

        public static byte[] MovedCursorUtf8(string text, uint state = 3)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x7DC]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x8); // Reply code
                WriteStringWithCursor(text, writer, 0x7D4, Encoding.UTF8);
                return stream.ToArray();
            }
        }

        public static byte[] DecidedEnterUtf8(string text, uint state = 4)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x7D8]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x9); // Reply code
                WriteString(text, writer, 0x7D4, Encoding.UTF8);
                return stream.ToArray();
            }
        }

        public static byte[] UnsetCustomizeDic(uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x0]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0xA); // Reply code
                return stream.ToArray();
            }
        }

        public static byte[] ReleasedUserWordInfo(uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x0]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0xB); // Reply code
                return stream.ToArray();
            }
        }

        public static byte[] UnsetCustomizedDictionaries(uint state = 1)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x0]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0xC); // Reply code
                return stream.ToArray();
            }
        }

        public static byte[] ChangedStringV2(string text, uint state = 3)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x3FD]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State: Data available
                writer.Write((uint)0xD); // Reply code
                WriteStringWithCursor(text, writer, 0x3EC, Encoding.Unicode);
                writer.Write((int)0); // ?
                writer.Write((int)0); // ?
                writer.Write((byte)0); // Flag == 0
                return stream.ToArray();
            }
        }

        public static byte[] MovedCursorV2(string text, uint state = 3)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x3F5]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State: Data available
                writer.Write((uint)0xE); // Reply code
                WriteStringWithCursor(text, writer, 0x3EC, Encoding.Unicode);
                writer.Write((byte)0); // Flag == 0
                return stream.ToArray();
            }
        }

        public static byte[] ChangedStringUtf8V2(string text, uint state = 3)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x7E5]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0xF); // Reply code
                WriteStringWithCursor(text, writer, 0x7D4, Encoding.UTF8);
                writer.Write((int)0); // ?
                writer.Write((int)0); // ?
                writer.Write((byte)0); // Flag == 0
                return stream.ToArray();
            }
        }

        public static byte[] MovedCursorUtf8V2(string text, uint state = 3)
        {
            using (MemoryStream stream = new MemoryStream(new byte[2*sizeof(uint) + 0x7DD]))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((uint)state); // State
                writer.Write((uint)0x10); // Reply code
                WriteStringWithCursor(text, writer, 0x7D4, Encoding.UTF8);
                writer.Write((byte)0); // Flag == 0
                return stream.ToArray();
            }
        }
    }
}
