using Concentus.Structs;
using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    class IHardwareOpusDecoder : IpcService
    {
        private const int FixedSampleRate = 48000;

        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private int SampleRate;
        private int ChannelsCount;

        private OpusDecoder Decoder;

        public IHardwareOpusDecoder(int SampleRate, int ChannelsCount)
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, DecodeInterleaved }
            };

            this.SampleRate    = SampleRate;
            this.ChannelsCount = ChannelsCount;

            Decoder = new OpusDecoder(FixedSampleRate, ChannelsCount);
        }

        public long DecodeInterleaved(ServiceCtx Context)
        {
            long InPosition = Context.Request.SendBuff[0].Position;
            long InSize     = Context.Request.SendBuff[0].Size;

            long OutPosition = Context.Request.ReceiveBuff[0].Position;
            long OutSize     = Context.Request.ReceiveBuff[0].Size;

            byte[] OpusData = Context.Memory.ReadBytes(InPosition, InSize);

            short[] Pcm = new short[OutSize / 2];

            int FrameSize = Pcm.Length / (ChannelsCount * 2);

            int Result = Decoder.Decode(OpusData, 0, OpusData.Length, Pcm, 0, FrameSize);

            foreach (short Sample in Pcm)
            {
                Context.Memory.WriteInt16(OutPosition, Sample);

                OutPosition += 2;
            }

            return 0;
        }
    }
}
