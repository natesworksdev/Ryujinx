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

            byte[] Output = new byte[OutputResponse.TotalSize];

            IntPtr Ptr = Marshal.AllocHGlobal(Output.Length);

            Marshal.StructureToPtr(OutputResponse, Ptr, true);

            Marshal.Copy(Ptr, Output, 0, Output.Length);

            Marshal.FreeHGlobal(Ptr);

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

            bool First = false;

            byte[] PoolEntryArray = new byte[16 * MemoryPoolCount];

            for (int Index = 0; Index < PoolEntry.Length; Index++)
            {
                if (!First)
                {
                    IntPtr PtrPool = Marshal.AllocHGlobal(PoolEntryArray.Length);
                    Marshal.StructureToPtr(PoolEntry[Index], PtrPool, true);
                    Marshal.Copy(PtrPool, PoolEntryArray, Index, PoolEntryArray.Length);
                    Marshal.FreeHGlobal(PtrPool);
                }
                /*else
                {
                    IntPtr PtrPool = Marshal.AllocHGlobal(PoolEntryArray.Length);
                    Marshal.StructureToPtr(PoolEntry[Index], PtrPool, true);
                    Marshal.Copy(PtrPool, PoolEntryArray, Marshal.SizeOf(PoolEntry[Index]) * Index, PoolEntryArray.Length);
                    Marshal.FreeHGlobal(PtrPool);
                }*/

                First = true;
            }

            File.WriteAllBytes("PoolTest.bin", PoolEntryArray);

            Array.Copy(PoolEntryArray, 0, Output, Marshal.SizeOf(OutputResponse), PoolEntryArray.Length);

            Context.Memory.WriteBytes(OutputPosition + 0x4, Output);

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
 