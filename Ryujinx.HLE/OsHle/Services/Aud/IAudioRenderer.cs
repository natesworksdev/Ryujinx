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
            long OutputPosition = Context.Request.GetBufferType0x22().Position;
            long InputPosition  = Context.Request.GetBufferType0x21().Position;

            AudioRendererConfig InputRequest = AMemoryHelper.Read<AudioRendererConfig>(Context.Memory, InputPosition);

            int MemoryPoolOffset = Marshal.SizeOf(InputRequest) + InputRequest.BehaviourSize;

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

            Context.Memory.WriteInt32(OutputPosition + 0x4,  OutputResponse.ErrorInfoSize);
            Context.Memory.WriteInt32(OutputPosition + 0x8,  OutputResponse.MemoryPoolsSize);
            Context.Memory.WriteInt32(OutputPosition + 0xc,  OutputResponse.VoicesSize);
            Context.Memory.WriteInt32(OutputPosition + 0x14, OutputResponse.EffectsSize);
            Context.Memory.WriteInt32(OutputPosition + 0x1c, OutputResponse.SinksSize);
            Context.Memory.WriteInt32(OutputPosition + 0x20, OutputResponse.PerformanceManagerSize);
            Context.Memory.WriteInt32(OutputPosition + 0x3c, OutputResponse.TotalSize - 4);

            for (int Offset = 0x40; Offset < 0x40 + OutputResponse.MemoryPoolsSize; Offset += 0x10, MemoryPoolOffset += 0x20)
            {
                int PoolState = Context.Memory.ReadInt32(InputPosition + MemoryPoolOffset + 0xC);

                if (PoolState == 4)
                {
                    Context.Memory.WriteInt32(OutputPosition + Offset, 5);
                }
                else if (PoolState == 2)
                {
                    Context.Memory.WriteInt32(OutputPosition + Offset, 3);
                }
                else
                {
                    Context.Memory.WriteInt32(OutputPosition + Offset, PoolState);
                }
            }

            //TODO: We shouldn't be signaling this here.
            UpdateEvent.WaitEvent.Set();

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
 