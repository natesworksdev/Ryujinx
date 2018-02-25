using Ryujinx.Core.OsHle.Ipc;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ryujinx.Core
{
    public static class Logging
    {
        private static Stopwatch ExecutionTime = new Stopwatch();
        private const string LogFileName = "Ryujinx.log";

        private static bool EnableInfo    = Config.LoggingEnableInfo;
        private static bool EnableTrace   = Config.LoggingEnableTrace;
        private static bool EnableDebug   = Config.LoggingEnableDebug;
        private static bool EnableWarn    = Config.LoggingEnableWarn;
        private static bool EnableError   = Config.LoggingEnableError;
        private static bool EnableFatal   = Config.LoggingEnableFatal;
        private static bool EnableIpc     = Config.LoggingEnableIpc;
        private static bool EnableLogFile = Config.LoggingEnableLogFile;

        static Logging()
        {
            ExecutionTime.Start();

            if (File.Exists(LogFileName)) File.Delete(LogFileName);
        }

        public static string GetExecutionTime()
        {
            return ExecutionTime.ElapsedMilliseconds.ToString().PadLeft(8, '0') + "ms";
        }

        private static string WhoCalledMe()
        {
            return new StackTrace().GetFrame(2).GetMethod().Name;
        }

        private static void LogFile(string Message)
        {
            if (EnableLogFile)
            {
                using (StreamWriter Writer = File.AppendText(LogFileName))
                {
                    Writer.WriteLine(Message);
                }
            }
        }

        public static void Info(string Message)
        {
            if (EnableInfo)
            {
                string Text = $"{GetExecutionTime()} | INFO  > {Message}";

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(Text.PadLeft(Text.Length + 1, ' '));
                Console.ResetColor();

                LogFile(Text);
            }
        }

        public static void Trace(string Message)
        {
            if (EnableTrace)
            {
                string Text = $"{GetExecutionTime()} | TRACE > {WhoCalledMe()} - {Message}";

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(Text.PadLeft(Text.Length + 1, ' '));
                Console.ResetColor();

                LogFile(Text);
            }
        }

        public static void Debug(string Message)
        {
            if (EnableDebug)
            {
                string Text = $"{GetExecutionTime()} | DEBUG > {WhoCalledMe()} - {Message}";

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(Text.PadLeft(Text.Length + 1, ' '));
                Console.ResetColor();

                LogFile(Text);
            }
        }

        public static void Warn(string Message)
        {
            if (EnableWarn)
            {
                string Text = $"{GetExecutionTime()} | WARN  > {WhoCalledMe()} - {Message}";

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Text.PadLeft(Text.Length + 1, ' '));
                Console.ResetColor();

                LogFile(Text);
            }
        }

        public static void Error(string Message)
        {
            if (EnableError)
            {
                string Text = $"{GetExecutionTime()} | ERROR > {WhoCalledMe()} - {Message}";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Text.PadLeft(Text.Length + 1, ' '));
                Console.ResetColor();

                LogFile(Text);
            }
        }

        public static void Fatal(string Message)
        {
            if (EnableFatal)
            {
                string Text = $"{GetExecutionTime()} | FATAL > {WhoCalledMe()} - {Message}";

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(Text.PadLeft(Text.Length + 1, ' '));
                Console.ResetColor();

                LogFile(Text);
            }
        }

        public static void Ipc(byte[] Data, long CmdPtr, bool Domain)
        {
            if (EnableIpc)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                string IpcMessage = "";
                using (MemoryStream MS = new MemoryStream(Data))
                {
                    BinaryReader Reader = new BinaryReader(MS);

                    int Word0 = Reader.ReadInt32();
                    int Word1 = Reader.ReadInt32();

                    int Type = (Word0 & 0xffff);

                    int PtrBuffCount = (Word0 >> 16) & 0xf;
                    int SendBuffCount = (Word0 >> 20) & 0xf;
                    int RecvBuffCount = (Word0 >> 24) & 0xf;
                    int XchgBuffCount = (Word0 >> 28) & 0xf;

                    int RawDataSize = (Word1 >> 0) & 0x3ff;
                    int RecvListFlags = (Word1 >> 10) & 0xf;
                    bool HndDescEnable = ((Word1 >> 31) & 0x1) != 0;

                    IpcMessage += Environment.NewLine + $" {GetExecutionTime()} | IpcMessage >" + Environment.NewLine +
                                  $"   Type: {Enum.GetName(typeof(IpcMessageType), Type)}" + Environment.NewLine +

                                  $"   PtrBuffCount: {PtrBuffCount.ToString()}" + Environment.NewLine +
                                  $"   SendBuffCount: {SendBuffCount.ToString()}" + Environment.NewLine +
                                  $"   RecvBuffCount: {RecvBuffCount.ToString()}" + Environment.NewLine +
                                  $"   XchgBuffCount: {XchgBuffCount.ToString()}" + Environment.NewLine +

                                  $"   RawDataSize: {RawDataSize.ToString()}" + Environment.NewLine +
                                  $"   RecvListFlags: {RecvListFlags.ToString()}" + Environment.NewLine +
                                  $"   HndDescEnable: {HndDescEnable.ToString()}" + Environment.NewLine;

                    if (HndDescEnable)
                    {
                        int Word = Reader.ReadInt32();

                        bool HasPId = (Word & 1) != 0;

                        int[] ToCopy = new int[(Word >> 1) & 0xf];
                        int[] ToMove = new int[(Word >> 5) & 0xf];

                        long PId = HasPId ? Reader.ReadInt64() : 0;

                        for (int Index = 0; Index < ToCopy.Length; Index++)
                        {
                            ToCopy[Index] = Reader.ReadInt32();
                        }

                        for (int Index = 0; Index < ToMove.Length; Index++)
                        {
                            ToMove[Index] = Reader.ReadInt32();
                        }

                        IpcMessage += Environment.NewLine + "    HndDesc:" + Environment.NewLine +
                                      $"      PId: {PId.ToString()}" + Environment.NewLine +
                                      $"      ToCopy.Length: {ToCopy.Length.ToString()}" + Environment.NewLine +
                                      $"      ToMove.Length: {ToMove.Length.ToString()}" + Environment.NewLine;
                    }

                    for (int Index = 0; Index < PtrBuffCount; Index++)
                    {
                        long IpcPtrBuffDescWord0 = Reader.ReadUInt32();
                        long IpcPtrBuffDescWord1 = Reader.ReadUInt32();

                        long Position = IpcPtrBuffDescWord1;
                        Position |= (IpcPtrBuffDescWord0 << 20) & 0x0f00000000;
                        Position |= (IpcPtrBuffDescWord0 << 30) & 0x7000000000;

                        int IpcPtrBuffDescIndex = ((int)IpcPtrBuffDescWord0 >> 0) & 0x03f;
                        IpcPtrBuffDescIndex |= ((int)IpcPtrBuffDescWord0 >> 3) & 0x1c0;

                        short Size = (short)(IpcPtrBuffDescWord0 >> 16);

                        IpcMessage += Environment.NewLine + $"    PtrBuff[{Index}]:" + Environment.NewLine +
                                      $"      Position: {Position.ToString()}" + Environment.NewLine +
                                      $"      IpcPtrBuffDescIndex: {IpcPtrBuffDescIndex.ToString()}" + Environment.NewLine +
                                      $"      Size: {Size.ToString()}" + Environment.NewLine;
                    }

                    ReadIpcBuffValues(Reader, SendBuffCount, IpcMessage, "SendBuff");
                    ReadIpcBuffValues(Reader, RecvBuffCount, IpcMessage, "RecvBuff");
                    ReadIpcBuffValues(Reader, XchgBuffCount, IpcMessage, "XchgBuff");

                    RawDataSize *= 4;

                    long RecvListPos = Reader.BaseStream.Position + RawDataSize;
                    long Pad0 = 0;

                    if ((Reader.BaseStream.Position + CmdPtr & 0xf) != 0)
                    {
                        Pad0 = 0x10 - (Reader.BaseStream.Position + CmdPtr & 0xf);
                    }

                    Reader.BaseStream.Seek(Pad0, SeekOrigin.Current);

                    int RecvListCount = RecvListFlags - 2;

                    if (RecvListCount == 0)
                    {
                        RecvListCount = 1;
                    }
                    else if (RecvListCount < 0)
                    {
                        RecvListCount = 0;
                    }

                    if (Domain && (IpcMessageType)Type == IpcMessageType.Request)
                    {
                        int DomWord0 = Reader.ReadInt32();

                        int DomCmd = (DomWord0 & 0xff);

                        RawDataSize = (DomWord0 >> 16) & 0xffff;

                        int DomObjId = Reader.ReadInt32();

                        Reader.ReadInt64(); //Padding

                        IpcMessage += Environment.NewLine + $"    Domain:" + Environment.NewLine +
                                      $"      DomCmd: {Enum.GetName(typeof(IpcDomCmd), DomCmd)}" + Environment.NewLine +
                                      $"      DomObjId: {DomObjId.ToString()}" + Environment.NewLine;
                    }

                    byte[] RawData = Reader.ReadBytes(RawDataSize);

                    IpcMessage += Environment.NewLine + $"   RawData:" + Environment.NewLine + HexDump(RawData) + Environment.NewLine;

                    Reader.BaseStream.Seek(RecvListPos, SeekOrigin.Begin);

                    for (int Index = 0; Index < RecvListCount; Index++)
                    {
                        long RecvListBuffValue = Reader.ReadInt64();
                        long RecvListBuffPosition = RecvListBuffValue & 0xffffffffffff;
                        long RecvListBuffSize = (short)(RecvListBuffValue >> 48);

                        IpcMessage += Environment.NewLine + $"    RecvList[{Index}]:" + Environment.NewLine +
                                      $"      Value: {RecvListBuffValue.ToString()}" + Environment.NewLine +
                                      $"      Position: {RecvListBuffPosition.ToString()}" + Environment.NewLine +
                                      $"      Size: {RecvListBuffSize.ToString()}" + Environment.NewLine;
                    }
                }

                Console.WriteLine(IpcMessage);
                Console.ResetColor();
            }
        }

        private static void ReadIpcBuffValues(BinaryReader Reader, int Count, string IpcMessage, string BufferName)
        {
            for (int Index = 0; Index < Count; Index++)
            {
                long Word0 = Reader.ReadUInt32();
                long Word1 = Reader.ReadUInt32();
                long Word2 = Reader.ReadUInt32();

                long Position = Word1;
                Position |= (Word2 << 4) & 0x0f00000000;
                Position |= (Word2 << 34) & 0x7000000000;

                long Size = Word0;
                Size |= (Word2 << 8) & 0xf00000000;

                int Flags = (int)Word2 & 3;

                IpcMessage += Environment.NewLine + $"    {BufferName}[{Index}]:" + Environment.NewLine +
                              $"      Position: {Position.ToString()}" + Environment.NewLine +
                              $"      Flags: {Flags.ToString()}" + Environment.NewLine +
                              $"      Size: {Size.ToString()}" + Environment.NewLine;
            }
        }

        //https://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
        public static string HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
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
