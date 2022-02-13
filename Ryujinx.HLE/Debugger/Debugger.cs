using ARMeilleure.State;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Memory;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ExecutionContext = ARMeilleure.State.ExecutionContext;

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

        private ulong? cThread;
        private ulong? gThread;

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
        private ulong[] GetThreadIds() => Device.System.DebugGetApplicationProcess().DebugGetThreadUids();

        private ExecutionContext GetThread(ulong threadUid) =>
            Device.System.DebugGetApplicationProcess().DebugGetThreadContext(threadUid);
        private ExecutionContext[] GetThreads() => GetThreadIds().Select(GetThread).ToArray();
        private IVirtualMemoryManager GetMemory() => Device.System.DebugGetApplicationProcess().CpuMemory;
        private void InvalidateCacheRegion(ulong address, ulong size) =>
            Device.System.DebugGetApplicationProcess().InvalidateCacheRegion(address, size);

        const int GdbRegisterCount = 68;

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
                case >= 34 and <= 65:
                    return 32;
                case 66:
                    return 8;
                case 67:
                    return 8;
                default:
                    throw new ArgumentException();
            }
        }

        private string GdbReadRegister(ExecutionContext state, int gdbRegId)
        {
            switch (gdbRegId)
            {
                case >= 0 and <= 31:
                    return ToHex(BitConverter.GetBytes(state.GetX(gdbRegId)));
                case 32:
                    return ToHex(BitConverter.GetBytes(state.DebugPc));
                case 33:
                    return ToHex(BitConverter.GetBytes(state.GetPstate()));
                case >= 34 and <= 65:
                    return ToHex(state.GetV(gdbRegId - 34).ToArray());
                case 66:
                    return ToHex(BitConverter.GetBytes((uint)state.Fpsr));
                case 67:
                    return ToHex(BitConverter.GetBytes((uint)state.Fpcr));
                default:
                    return null;
            }
        }

        private bool GdbWriteRegister(ExecutionContext state, int gdbRegId, ulong value)
        {
            switch (gdbRegId)
            {
                case >= 0 and <= 31:
                    state.SetX(gdbRegId, value);
                    return true;
                case 32:
                    state.DebugPc = value;
                    return true;
                case 33:
                    state.SetPstate((uint)value);
                    return true;
                default:
                    return false;
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
                        CommandQuery();
                        break;

                    case SendNackMessage _:
                        WriteStream.WriteByte((byte)'-');
                        break;

                    case CommandMessage {Command: var cmd}:
                        Logger.Notice.Print(LogClass.GdbStub, $"Received Command: {cmd}");
                        WriteStream.WriteByte((byte)'+');
                        ProcessCommand(cmd);
                        break;

                    case ThreadBreakMessage msg:
                        HaltApplication();
                        Reply($"T05thread:{msg.ThreadUid:x};");
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
                    ReplyOK();
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
                        ulong? threadId = ss.ReadRemainingAsThreadUid();
                        CommandSetThread(op, threadId);
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
                    if (ss.ConsumeRemaining("GDBServerVersion"))
                    {
                        Reply($"name:Ryujinx;version:{ReleaseInformations.GetVersion()};");
                        break;
                    }

                    if (ss.ConsumeRemaining("HostInfo"))
                    {
                        Reply(
                            $"triple:{ToHex("aarch64-unknown-linux-android")};endian:little;ptrsize:8;hostname:{ToHex("Ryujinx")};");
                        break;
                    }

                    if (ss.ConsumeRemaining("ProcessInfo"))
                    {
                        Reply(
                            $"pid:1;cputype:100000c;cpusubtype:0;triple:{ToHex("aarch64-unknown-linux-android")};ostype:unknown;vendor:none;endian:little;ptrsize:8;");
                        break;
                    }

                    if (ss.ConsumePrefix("Supported:") || ss.ConsumeRemaining("Supported"))
                    {
                        Reply("PacketSize=10000;qXfer:features:read+");
                        break;
                    }

                    if (ss.ConsumeRemaining("fThreadInfo"))
                    {
                        Reply($"m{string.Join(",", GetThreadIds().Select(x => $"{x:x}"))}");
                        break;
                    }

                    if (ss.ConsumeRemaining("sThreadInfo"))
                    {
                        Reply("l");
                        break;
                    }

                    if (ss.ConsumePrefix("Xfer:features:read:"))
                    {
                        string feature = ss.ReadUntil(':');
                        ulong addr = ss.ReadUntilAsHex(',');
                        ulong len = ss.ReadRemainingAsHex();

                        string data;
                        if (RegisterInformation.Features.TryGetValue(feature, out data))
                        {
                            if (addr >= (ulong)data.Length)
                            {
                                Reply("l");
                                break;
                            }

                            if (len >= (ulong)data.Length - addr)
                            {
                                Reply("l" + data.Substring((int)addr));
                                break;
                            }
                            else
                            {
                                Reply("m" + data.Substring((int)addr, (int)len));
                                break;
                            }
                        }
                        else
                        {
                            Reply("E00"); // Invalid annex
                            break;
                        }
                    }

                    goto unknownCommand;
                case 'Q':
                    goto unknownCommand;
                case 's':
                    CommandStep(ss.IsEmpty() ? null : ss.ReadRemainingAsHex());
                    break;
                case 'T':
                    {
                        ulong? threadId = ss.ReadRemainingAsThreadUid();
                        CommandIsAlive(threadId);
                        break;
                    }
                default:
                    unknownCommand:
                    // Logger.Notice.Print(LogClass.GdbStub, $"Unknown command: {cmd}");
                    Reply("");
                    break;
            }
        }

        void CommandQuery()
        {
            // GDB is performing initial contact. Stop everything.
            HaltApplication();
            gThread = cThread = GetThreadIds().First();
            Reply($"T05thread:{cThread:x};");
            //Reply("S05");
        }

        void CommandContinue(ulong? newPc)
        {
            if (newPc.HasValue)
            {
                if (cThread == null)
                {
                    ReplyError();
                    return;
                }

                GetThread(cThread.Value).DebugPc = newPc.Value;
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
            if (gThread == null)
            {
                ReplyError();
                return;
            }

            var ctx = GetThread(gThread.Value);
            string registers = "";
            for (int i = 0; i < GdbRegisterCount; i++)
            {
                registers += GdbReadRegister(ctx, i);
            }

            Reply(registers);
        }

        void CommandWriteGeneralRegisters(StringStream ss)
        {
            if (gThread == null)
            {
                ReplyError();
                return;
            }

            var ctx = GetThread(gThread.Value);
            for (int i = 0; i < GdbRegisterCount; i++)
            {
                GdbWriteRegister(ctx, i, ss.ReadLengthAsLEHex(GdbRegisterHexSize(i)));
            }

            if (ss.IsEmpty())
            {
                ReplyOK();
            }
            else
            {
                ReplyError();
            }
        }

        void CommandSetThread(char op, ulong? threadId)
        {
            if (threadId == 0)
            {
                threadId = GetThreads().First().ThreadUid;
            }

            switch (op)
            {
                case 'c':
                    cThread = threadId;
                    ReplyOK();
                    return;
                case 'g':
                    gThread = threadId;
                    ReplyOK();
                    return;
                default:
                    ReplyError();
                    return;
            }
        }

        void CommandReadMemory(ulong addr, ulong len)
        {
            try
            {
                var data = new byte[len];
                GetMemory().Read(addr, data);
                Reply(ToHex(data));
            }
            catch (InvalidMemoryRegionException)
            {
                ReplyError();
            }
        }

        void CommandWriteMemory(ulong addr, ulong len, StringStream ss)
        {
            try
            {
                var data = new byte[len];
                for (ulong i = 0; i < len; i++)
                {
                    data[i] = (byte)ss.ReadLengthAsHex(2);
                }

                GetMemory().Write(addr, data);
                InvalidateCacheRegion(addr, len);
                ReplyOK();
            }
            catch (InvalidMemoryRegionException)
            {
                ReplyError();
            }
        }

        void CommandReadGeneralRegister(int gdbRegId)
        {
            if (gThread == null)
            {
                ReplyError();
                return;
            }

            var ctx = GetThread(gThread.Value);
            string result = GdbReadRegister(ctx, gdbRegId);
            if (result != null)
            {
                Reply(result);
            }
            else
            {
                ReplyError();
            }
        }

        void CommandWriteGeneralRegister(int gdbRegId, ulong value)
        {
            if (gThread == null)
            {
                ReplyError();
                return;
            }

            var ctx = GetThread(gThread.Value);
            if (GdbWriteRegister(ctx, gdbRegId, value))
            {
                ReplyOK();
            }
            else
            {
                ReplyError();
            }
        }

        private void CommandStep(ulong? newPc)
        {
            if (cThread == null)
            {
                ReplyError();
                return;
            }

            var ctx = GetThread(cThread.Value);

            if (newPc.HasValue)
            {
                ctx.DebugPc = newPc.Value;
            }

            ctx.DebugStep();
            Reply($"T00thread:{ctx.ThreadUid:x};");
        }

        private void CommandIsAlive(ulong? threadId)
        {
            if (GetThreads().Any(x => x.ThreadUid == threadId))
            {
                ReplyOK();
            }
            else
            {
                Reply("E00");
            }
        }

        private void Reply(string cmd)
        {
            Logger.Notice.Print(LogClass.GdbStub, $"Reply: {cmd}");
            WriteStream.Write(Encoding.ASCII.GetBytes($"${cmd}#{CalculateChecksum(cmd):x2}"));
        }

        private void ReplyOK()
        {
            Reply("OK");
        }

        private void ReplyError()
        {
            Reply("E01");
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
                ClientSocket.NoDelay = true;
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

        public void ThreadBreak(object sender, InstExceptionEventArgs e)
        {
            ExecutionContext ctx = (ExecutionContext)sender;

            ctx.DebugStop();

            Logger.Notice.Print(LogClass.GdbStub, $"Break hit on thread {ctx.ThreadUid} at pc {e.Address:x016}");

            Messages.Add(new ThreadBreakMessage(e, ctx.ThreadUid));
        }
    }
}