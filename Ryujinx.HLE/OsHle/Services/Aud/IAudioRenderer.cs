using ChocolArm64.Memory;
using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Handles;
using Ryujinx.HLE.OsHle.Ipc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    class IAudioRenderer : IpcService, IDisposable
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private KEvent UpdateEvent;

        private AudioRendererParameters WorkerParams;

        public IAudioRenderer(AudioRendererParameters WorkerParams)
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 4, RequestUpdateAudioRenderer },
                { 5, StartAudioRenderer         },
                { 6, StopAudioRenderer          },
                { 7, QuerySystemEvent           }
            };

            UpdateEvent       = new KEvent();
            this.WorkerParams = WorkerParams;
        }

        public long RequestUpdateAudioRenderer(ServiceCtx Context)
        {
            //(buffer<unknown, 5, 0>) -> (buffer<unknown, 6, 0>, buffer<unknown, 6, 0>)

            long OutputPosition = Context.Request.GetBufferType0x22().Position;
            long InputPosition  = Context.Request.GetBufferType0x21().Position;

            AudioRendererConfig InputRequest = AMemoryHelper.Read<AudioRendererConfig>(Context.Memory, InputPosition);

            int MemoryPoolCount = WorkerParams.EffectCount + (WorkerParams.VoiceCount * 4);

            int MemoryPoolOffset = Marshal.SizeOf(InputRequest) + InputRequest.BehaviourSize;

            MemoryPoolInfo[] PoolInfo = new MemoryPoolInfo[MemoryPoolCount];

            for (int Index = 0; Index < MemoryPoolCount; Index++)
            {
                PoolInfo[Index] = AMemoryHelper.Read<MemoryPoolInfo>(Context.Memory, InputPosition + MemoryPoolOffset + Index * 0x20);
            }

            GCHandle Handle = GCHandle.Alloc(WorkerParams, GCHandleType.Pinned);

            AudioRendererResponse OutputResponse = (AudioRendererResponse)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(AudioRendererResponse));

            Handle.Free();

            OutputResponse.Revision = WorkerParams.Magic;
            OutputResponse.ErrorInfoSize = 0xb0;
            OutputResponse.MemoryPoolsSize = (WorkerParams.EffectCount + (WorkerParams.VoiceCount * 4)) * 0x10;
            OutputResponse.VoicesSize = WorkerParams.VoiceCount * 0x10;
            OutputResponse.EffectsSize = WorkerParams.EffectCount * 0x10;
            OutputResponse.SinksSize = WorkerParams.SinkCount * 0x20;
            OutputResponse.PerformanceManagerSize = 0x10;
            OutputResponse.TotalSize = Marshal.SizeOf(OutputResponse) + OutputResponse.ErrorInfoSize + OutputResponse.MemoryPoolsSize +
                OutputResponse.VoicesSize + OutputResponse.EffectsSize + OutputResponse.SinksSize + OutputResponse.PerformanceManagerSize;

            Context.Ns.Log.PrintInfo(LogClass.ServiceAudio, $"TotalSize: {OutputResponse.TotalSize}");
            Context.Ns.Log.PrintInfo(LogClass.ServiceAudio, $"MemoryPoolsSize: {OutputResponse.MemoryPoolsSize}");
            Context.Ns.Log.PrintInfo(LogClass.ServiceAudio, $"VoicesSize: {OutputResponse.VoicesSize}");
            Context.Ns.Log.PrintInfo(LogClass.ServiceAudio, $"EffectsSize: {OutputResponse.EffectsSize}");
            Context.Ns.Log.PrintInfo(LogClass.ServiceAudio, $"SinksSize: {OutputResponse.SinksSize}");
            Context.Ns.Log.PrintInfo(LogClass.ServiceAudio, $"MemoryPoolCount: {MemoryPoolCount}");

            MemoryPoolEntry[] PoolEntry = new MemoryPoolEntry[MemoryPoolCount];

            for (int Index = 0; Index < PoolEntry.Length; Index++)
            {
                if (PoolInfo[Index].PoolState == (int)MemoryPoolStates.MPS_RequestAttach)
                    PoolEntry[Index].State = (int)MemoryPoolStates.MPS_RequestAttach;
                else if (PoolInfo[Index].PoolState == (int)MemoryPoolStates.MPS_RequestDetatch)
                    PoolEntry[Index].State = (int)MemoryPoolStates.MPS_Detatched;
                else
                    PoolEntry[Index].State = PoolInfo[Index].PoolState;
            }

            //0x40 bytes header
            Context.Memory.WriteInt32(OutputPosition + 0x4, OutputResponse.ErrorInfoSize); //Behavior Out State Size? (note: this is the last section)
            Context.Memory.WriteInt32(OutputPosition + 0x8, OutputResponse.MemoryPoolsSize); //Memory Pool Out State Size?
            Context.Memory.WriteInt32(OutputPosition + 0xc, OutputResponse.VoicesSize); //Voice Out State Size?
            Context.Memory.WriteInt32(OutputPosition + 0x14, OutputResponse.EffectsSize); //Effect Out State Size?
            Context.Memory.WriteInt32(OutputPosition + 0x1c, OutputResponse.SinksSize); //Sink Out State Size?
            Context.Memory.WriteInt32(OutputPosition + 0x20, OutputResponse.PerformanceManagerSize); //Performance Out State Size?
            Context.Memory.WriteInt32(OutputPosition + 0x3c, OutputResponse.TotalSize); //Total Size (including 0x40 bytes header)

            for (int Offset = 0x40; Offset < 0x40 + (OutputResponse.TotalSize - 800); Offset += 0x10)
            {
                Context.Memory.WriteInt32(OutputPosition + Offset, 5);
            }

            //TODO: We shouldn't be signaling this here.
            UpdateEvent.WaitEvent.Set();

            Context.Ns.Log.PrintStub(LogClass.ServiceAudio, "Stubbed.");

            return 0;
        }

        public long StartAudioRenderer(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAudio, "Stubbed.");

            return 0;
        }

        public long StopAudioRenderer(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAudio, "Stubbed.");

            return 0;
        }

        public long QuerySystemEvent(ServiceCtx Context)
        {
            int Handle = Context.Process.HandleTable.OpenHandle(UpdateEvent);

            Context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Handle);

            return 0;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                UpdateEvent.Dispose();
            }
        }
    }
}
 