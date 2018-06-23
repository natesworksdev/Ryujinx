using ChocolArm64.Memory;
using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Handles;
using Ryujinx.HLE.OsHle.Ipc;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    class IAudioRenderer : IpcService, IDisposable
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private KEvent UpdateEvent;

        private AudioRendererParameters Params;

        public IAudioRenderer(AudioRendererParameters WorkerParams)
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 4, RequestUpdateAudioRenderer },
                { 5, StartAudioRenderer         },
                { 6, StopAudioRenderer          },
                { 7, QuerySystemEvent           }
            };

            UpdateEvent = new KEvent();

            this.Params = WorkerParams;
        }

        public long RequestUpdateAudioRenderer(ServiceCtx Context)
        {
            long OutputPosition = Context.Request.GetBufferType0x22().Position;
            long InputPosition  = Context.Request.GetBufferType0x21().Position;

            AudioRendererConfig InputData = AMemoryHelper.Read<AudioRendererConfig>(Context.Memory, InputPosition);

            int MemoryPoolOffset = Marshal.SizeOf(InputData) + InputData.BehaviourSize;

            AudioRendererOutput OutputData = new AudioRendererOutput();

            OutputData.Revision               = Params.Revision;
            OutputData.ErrorInfoSize          = 0xb0;
            OutputData.MemoryPoolsSize        = (Params.EffectCount + (Params.VoiceCount * 4)) * 0x10;
            OutputData.VoicesSize             = Params.VoiceCount  * 0x10;
            OutputData.EffectsSize            = Params.EffectCount * 0x10;
            OutputData.SinksSize              = Params.SinkCount   * 0x20;
            OutputData.PerformanceManagerSize = 0x10;
            OutputData.TotalSize              = Marshal.SizeOf(OutputData) + OutputData.ErrorInfoSize + OutputData.MemoryPoolsSize +
                OutputData.VoicesSize + OutputData.EffectsSize + OutputData.SinksSize + OutputData.PerformanceManagerSize;

            AMemoryHelper.Write(Context.Memory, OutputPosition, OutputData);

            for (int Offset = 0x40; Offset < 0x40 + OutputData.MemoryPoolsSize; Offset += 0x10, MemoryPoolOffset += 0x20)
            {
                MemoryPoolStates PoolState = (MemoryPoolStates) Context.Memory.ReadInt32(InputPosition + MemoryPoolOffset + 0x10);

                if (PoolState == MemoryPoolStates.RequestAttach)
                {
                    Context.Memory.WriteInt32(OutputPosition + Offset, (int)MemoryPoolStates.Attached);
                }
                else if (PoolState == MemoryPoolStates.RequestDetach)
                {
                    Context.Memory.WriteInt32(OutputPosition + Offset, (int)MemoryPoolStates.Detached);
                }
                else
                {
                    Context.Memory.WriteInt32(OutputPosition + Offset, (int)PoolState);
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
 