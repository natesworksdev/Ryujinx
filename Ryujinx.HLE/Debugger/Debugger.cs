using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Memory;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ryujinx.HLE.Debugger
{
    public class Debugger : IDisposable
    {
        internal Switch Device { get; private set; }

        public ushort GdbStubPort { get; private set; }

        private TcpListener ListenerSocket;
        private Socket ClientSocket = null;
        private NetworkStream ReadStream = null;
        private NetworkStream WriteStream = null;
        private BlockingCollection<IMessage> Messages = new BlockingCollection<IMessage>(1);
        private Thread SocketThread;
        private Thread HandlerThread;

        private long cThread;
        private long gThread;

        public Debugger(Switch device, ushort port)
        {
            Device = device;
            GdbStubPort = port;

            ARMeilleure.Optimizations.EnableDebugging = true;

            SocketThread = new Thread(SocketReaderThreadMain);
            HandlerThread = new Thread(HandlerThreadMain);
            SocketThread.Start();
            HandlerThread.Start();
        }

        private void HaltApplication() => Device.System.DebugGetApplicationProcess().DebugStopAllThreads();
        private long[] GetThreadIds() => Device.System.DebugGetApplicationProcess().DebugGetThreadUids();
        private ARMeilleure.State.ExecutionContext GetThread(long threadUid) => Device.System.DebugGetApplicationProcess().DebugGetThreadContext(threadUid);
        private ARMeilleure.State.ExecutionContext[] GetThreads() => GetThreadIds().Select(x => GetThread(x)).ToArray();
        private IVirtualMemoryManager GetMemory() => Device.System.DebugGetApplicationProcess().CpuMemory;

        const int GdbRegisterCount = 34;

        private int GdbRegisterHexSize(int gdbRegId)
        {
            switch (gdbRegId)
            {
                case >= 0 and <= 31:
                    return 16;
                case 32:
                    return 16;
                case 33:
                    return 8;
                default:
                    throw new ArgumentException();
            }
        }

        private string GdbReadRegister(ARMeilleure.State.ExecutionContext state, int gdbRegId)
        {
            switch (gdbRegId)
            {
                case >= 0 and <= 31:
                    return $"{state.GetX(gdbRegId):x16}";
                case 32:
                    return $"{state.DebugPc:x16}";
                case 33:
                    return $"{state.GetPstate():x8}";
                default:
                    throw new ArgumentException();
            }
        }

        private void GdbWriteRegister(ARMeilleure.State.ExecutionContext state, int gdbRegId, ulong value)
        {
            switch (gdbRegId)
            {
                case >= 0 and <= 31:
                    state.SetX(gdbRegId, value);
                    return;
                case 32:
                    state.DebugPc = value;
                    return;
                case 33:
                    state.SetPstate((uint)value);
                    return;
                default:
                    throw new ArgumentException();
            }
        }

        private void HandlerThreadMain()
        {
            while (true)
            {
                switch (Messages.Take())
                {
                    case AbortMessage _:
                        return;

                    case BreakInMessage _:
                        Logger.Notice.Print(LogClass.GdbStub, "Break-in requested");
                        // TODO
                        break;

                    case SendNackMessage _:
                        WriteStream.WriteByte((byte)'-');
                        break;

                    case CommandMessage { Command: var cmd }:
                        Logger.Debug?.Print(LogClass.GdbStub, $"Received Command: {cmd}");
                        WriteStream.WriteByte((byte)'+');
                        ProcessCommand(cmd);
                        break;
                }
            }
        }

        private void ProcessCommand(string cmd)
        {
            StringStream ss = new StringStream(cmd);

            switch (ss.ReadChar())
            {
                case '!':
                    if (!ss.IsEmpty())
                    {
                        goto unknownCommand;
                    }
                    // Enable extended mode
                    Reply("OK");
                    break;
                case '?':
                    if (!ss.IsEmpty())
                    {
                        goto unknownCommand;
                    }
                    CommandQuery();
                    break;
                case 'c':
                    CommandContinue(ss.IsEmpty() ? null : ss.ReadRemainingAsHex());
                    break;
                case 'D':
                    if (!ss.IsEmpty())
                    {
                        goto unknownCommand;
                    }
                    CommandDetach();
                    break;
                case 'g':
                    if (!ss.IsEmpty())
                    {
                        goto unknownCommand;
                    }
                    CommandReadGeneralRegisters();
                    break;
                case 'G':
                    CommandWriteGeneralRegisters(ss);
                    break;
                case 'H':
                    {
                        char op = ss.ReadChar();
                        ulong threadId = ss.ReadRemainingAsHex();
                        CommandSetThread(op, (long)threadId);
                        break;
                    }
                case 'k':
                    Logger.Notice.Print(LogClass.GdbStub, "Kill request received");
                    Reply("");
                    break;
                case 'm':
                    {
                        ulong addr = ss.ReadUntilAsHex(',');
                        ulong len = ss.ReadRemainingAsHex();
                        CommandReadMemory(addr, len);
                        break;
                    }
                case 'M':
                    {
                        ulong addr = ss.ReadUntilAsHex(',');
                        ulong len = ss.ReadUntilAsHex(':');
                        CommandWriteMemory(addr, len, ss);
                        break;
                    }
                case 'p':
                    {
                        ulong gdbRegId = ss.ReadRemainingAsHex();
                        CommandReadGeneralRegister((int)gdbRegId);
                        break;
                    }
                case 'P':
                    {
                        ulong gdbRegId = ss.ReadUntilAsHex('=');
                        ulong value = ss.ReadRemainingAsHex();
                        CommandWriteGeneralRegister((int)gdbRegId, value);
                        break;
                    }
                case 'q':
                    switch (ss.ReadUntil(':'))
                    {
                        case "GDBServerVersion":
                            Reply($"name:Ryujinx;version:{ReleaseInformations.GetVersion()};");
                            break;
                        case "HostInfo":
                            Reply($"triple:{ToHex("aarch64-none-elf")};endian:little;ptrsize:8;hostname:{ToHex("Ryujinx")};");
                            break;
                        case "ProcessInfo":
                            Reply("pid:1;cputype:100000c;cpusubtype:0;ostype:unknown;vendor:none;endian:little;ptrsize:8;");
                            break;
                        case "fThreadInfo":
                            Reply($"m {string.Join(",", GetThreadIds().Select(x => $"{x:x}"))}");
                            break;
                        case "sThreadInfo":
                            Reply("l");
                            break;
                        default:
                            goto unknownCommand;
                    }
                    break;
                default:
                    Logger.Notice.Print(LogClass.GdbStub, $"Unknown command: {cmd}");
                    Reply("");
                    break;
            }
        }

        void CommandQuery()
        {
            // GDB is performing initial contact. Stop everything.
            HaltApplication();
            gThread = cThread = GetThreadIds().First();
            Reply($"T05thread:{cThread:x}");
        }

        void CommandContinue(ulong? newPc)
        {
            if (newPc.HasValue)
            {
                GetThread(cThread).DebugPc = newPc.Value;
            }

            foreach (var thread in GetThreads())
            {
                thread.DebugContinue();
            }
        }

        void CommandDetach()
        {
            // TODO: Remove all breakpoints
            CommandContinue(null);
        }

        void CommandReadGeneralRegisters()
        {
            var ctx = GetThread(gThread);
            string registers = "";
            for (int i = 0; i < GdbRegisterCount; i++)
            {
                registers += GdbReadRegister(ctx, i);
            }
            Reply(registers);
        }

        void CommandWriteGeneralRegisters(StringStream ss)
        {
            var ctx = GetThread(gThread);
            for (int i = 0; i < GdbRegisterCount; i++)
            {
                GdbWriteRegister(ctx, i, ss.ReadLengthAsHex(GdbRegisterHexSize(i)));
            }
            Reply(ss.IsEmpty() ? "OK" : "E99");
        }

        void CommandSetThread(char op, long threadId)
        {
            switch (op)
            {
                case 'c':
                    cThread = threadId;
                    Reply("OK");
                    return;
                case 'g':
                    gThread = threadId;
                    Reply("OK");
                    return;
                default:
                    Reply("E99");
                    return;
            }
        }

        void CommandReadMemory(ulong addr, ulong len)
        {
            var data = new byte[len];
            GetMemory().Read(addr, data);
            Reply(ToHex(data));
        }

        void CommandWriteMemory(ulong addr, ulong len, StringStream ss)
        {
            var data = new byte[len];
            for (ulong i = 0; i < len; i++)
            {
                data[i] = (byte)ss.ReadLengthAsHex(2);
            }
            GetMemory().Write(addr, data);
        }

        void CommandReadGeneralRegister(int gdbRegId)
        {
            var ctx = GetThread(gThread);
            Reply(GdbReadRegister(ctx, gdbRegId));
        }

        void CommandWriteGeneralRegister(int gdbRegId, ulong value)
        {
            var ctx = GetThread(gThread);
            GdbWriteRegister(ctx, gdbRegId, value);
            Reply("OK");
        }

        private void Reply(string cmd)
        {
            WriteStream.Write(Encoding.ASCII.GetBytes($"${cmd}#{CalculateChecksum(cmd):x2}"));
        }

        private void SocketReaderThreadMain()
        {
        restartListen:
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Any, GdbStubPort);
                ListenerSocket = new TcpListener(endpoint);
                ListenerSocket.Start();
                Logger.Notice.Print(LogClass.GdbStub, $"Currently waiting on {endpoint} for GDB client");

                ClientSocket = ListenerSocket.AcceptSocket();
                ReadStream = new NetworkStream(ClientSocket, System.IO.FileAccess.Read);
                WriteStream = new NetworkStream(ClientSocket, System.IO.FileAccess.Write);
                Logger.Notice.Print(LogClass.GdbStub, "GDB client connected");

                while (true)
                {
                    switch (ReadStream.ReadByte())
                    {
                        case -1:
                            goto eof;
                        case '+':
                            continue;
                        case '-':
                            Logger.Notice.Print(LogClass.GdbStub, "NACK received!");
                            continue;
                        case '\x03':
                            Messages.Add(new BreakInMessage());
                            break;
                        case '$':
                            string cmd = "";
                            while (true)
                            {
                                int x = ReadStream.ReadByte();
                                if (x == -1)
                                    goto eof;
                                if (x == '#')
                                    break;
                                cmd += (char)x;
                            }

                            string checksum = $"{(char)ReadStream.ReadByte()}{(char)ReadStream.ReadByte()}";
                            // Debug.Assert(checksum == $"{CalculateChecksum(cmd):x2}");

                            Messages.Add(new CommandMessage(cmd));
                            break;
                    }
                }

            eof:
                Logger.Notice.Print(LogClass.GdbStub, "GDB client lost connection");
                goto restartListen;
            }
            catch (Exception)
            {
                Logger.Notice.Print(LogClass.GdbStub, "GDB stub socket closed");
                return;
            }
        }

        private byte CalculateChecksum(string cmd)
        {
            byte checksum = 0;
            foreach (char x in cmd)
            {
                unchecked
                {
                    checksum += (byte)x;
                }
            }
            return checksum;
        }

        private string ToHex(byte[] bytes)
        {
            return string.Join("", bytes.Select(x => $"{x:x2}"));
        }

        private string ToHex(string str)
        {
            return ToHex(Encoding.ASCII.GetBytes(str));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (HandlerThread.IsAlive)
                {
                    Messages.Add(new AbortMessage());
                }
                ListenerSocket.Stop();
                ClientSocket?.Shutdown(SocketShutdown.Both);
                ClientSocket?.Close();
                ReadStream?.Close();
                WriteStream?.Close();
                SocketThread.Join();
                HandlerThread.Join();
            }
        }
    }
}
