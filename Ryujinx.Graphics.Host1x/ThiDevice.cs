using Ryujinx.Common;
using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Host1x
{
    class ThiDevice : IDeviceState
    {
        private readonly ClassId _classId;
        private readonly IDeviceState _device;

        private readonly SyncptIncrManager _syncptIncrMgr;

        private struct CommandAction
        {
            public bool IsSyncptIncr { get; }
            public int Method { get; }
            public int Data { get; }

            public CommandAction(int method, int data)
            {
                IsSyncptIncr = false;
                Method = method;
                Data = data;
            }

            public CommandAction(uint syncptIncrHandle)
            {
                IsSyncptIncr = true;
                Method = 0;
                Data = (int)syncptIncrHandle;
            }
        }

        private readonly AsyncWorkQueue<CommandAction> _commandQueue;

        private readonly DeviceState<ThiRegisters> _state;

        public ThiDevice(ClassId classId, IDeviceState device, SyncptIncrManager syncptIncrMgr)
        {
            _classId = classId;
            _device = device;
            _syncptIncrMgr = syncptIncrMgr;
            _commandQueue = new AsyncWorkQueue<CommandAction>(Process, $"Ryujinx.{classId}Processor");
            _state = new DeviceState<ThiRegisters>(new Dictionary<string, (Action<int>, Func<int>)>
            {
                { nameof(ThiRegisters.IncrSyncpt), (IncrSyncpt, null) },
                { nameof(ThiRegisters.Method1), (Method1, null) }
            });
        }

        public int Read(int offset) => _state.Read(offset);
        public void Write(int offset, int data) => _state.Write(offset, data);

        private void IncrSyncpt(int data)
        {
            uint syncpointId = (uint)(data & 0xFF);
            uint cond = (uint)((data >> 8) & 0xFF); // 0 = Immediate, 1 = Done

            if (cond == 0)
            {
                _syncptIncrMgr.Increment(syncpointId);
            }
            else
            {
                _commandQueue.Add(new CommandAction(_syncptIncrMgr.IncrementWhenDone(_classId, syncpointId)));
            }
        }

        private void Method1(int data)
        {
            _commandQueue.Add(new CommandAction((int)_state.State.Method0 * 4, data));
        }

        private void Process(CommandAction cmdAction)
        {
            if (cmdAction.IsSyncptIncr)
            {
                _syncptIncrMgr.SignalDone((uint)cmdAction.Data);
            }
            else
            {
                _device.Write(cmdAction.Method, cmdAction.Data);
            }
        }
    }
}
