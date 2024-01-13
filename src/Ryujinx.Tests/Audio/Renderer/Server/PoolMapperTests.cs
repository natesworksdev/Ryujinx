using NUnit.Framework;
using Ryujinx.Audio;
using Ryujinx.Audio.Renderer.Server.MemoryPool;
using System;
using static Ryujinx.Audio.Renderer.Common.BehaviourParameter;
using CpuAddress = System.UInt64;
using DspAddress = System.UInt64;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class PoolMapperTests
    {
        private const uint DummyProcessHandle = 0xCAFEBABE;

        [Test]
        public void TestInitializeSystemPool()
        {
            PoolMapper poolMapper = new(DummyProcessHandle, true);
            MemoryPoolState memoryPoolDsp = MemoryPoolState.Create(MemoryPoolState.LocationType.Dsp);
            MemoryPoolState memoryPoolCpu = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);

            const CpuAddress CpuAddress = 0x20000;
            const DspAddress DspAddress = CpuAddress; // TODO: DSP LLE
            const ulong CpuSize = 0x1000;

            Assert.That(poolMapper.InitializeSystemPool(ref memoryPoolCpu, CpuAddress, CpuSize), Is.False);
            Assert.That(poolMapper.InitializeSystemPool(ref memoryPoolDsp, CpuAddress, CpuSize), Is.True);

            Assert.That(CpuAddress, Is.EqualTo(memoryPoolDsp.CpuAddress));
            Assert.That(CpuSize, Is.EqualTo(memoryPoolDsp.Size));
            Assert.That(DspAddress, Is.EqualTo(memoryPoolDsp.DspAddress));
        }

        [Test]
        public void TestGetProcessHandle()
        {
            PoolMapper poolMapper = new(DummyProcessHandle, true);
            MemoryPoolState memoryPoolDsp = MemoryPoolState.Create(MemoryPoolState.LocationType.Dsp);
            MemoryPoolState memoryPoolCpu = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);

            Assert.That(0xFFFF8001, Is.EqualTo(poolMapper.GetProcessHandle(ref memoryPoolCpu)));
            Assert.That(DummyProcessHandle, Is.EqualTo(poolMapper.GetProcessHandle(ref memoryPoolDsp)));
        }

        [Test]
        public void TestMappings()
        {
            PoolMapper poolMapper = new(DummyProcessHandle, true);
            MemoryPoolState memoryPoolDsp = MemoryPoolState.Create(MemoryPoolState.LocationType.Dsp);
            MemoryPoolState memoryPoolCpu = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);

            const CpuAddress CpuAddress = 0x20000;
            const DspAddress DspAddress = CpuAddress; // TODO: DSP LLE
            const ulong CpuSize = 0x1000;

            memoryPoolDsp.SetCpuAddress(CpuAddress, CpuSize);
            memoryPoolCpu.SetCpuAddress(CpuAddress, CpuSize);

            Assert.That(DspAddress, Is.EqualTo(poolMapper.Map(ref memoryPoolCpu)));
            Assert.That(DspAddress, Is.EqualTo(poolMapper.Map(ref memoryPoolDsp)));
            Assert.That(DspAddress, Is.EqualTo(memoryPoolDsp.DspAddress));
            Assert.That(poolMapper.Unmap(ref memoryPoolCpu), Is.True);

            memoryPoolDsp.IsUsed = true;
            Assert.That(poolMapper.Unmap(ref memoryPoolDsp), Is.False);
            memoryPoolDsp.IsUsed = false;
            Assert.That(poolMapper.Unmap(ref memoryPoolDsp), Is.True);
        }

        [Test]
        public void TestTryAttachBuffer()
        {
            const CpuAddress CpuAddress = 0x20000;
            const DspAddress DspAddress = CpuAddress; // TODO: DSP LLE
            const ulong CpuSize = 0x1000;

            const int MemoryPoolStateArraySize = 0x10;
            const CpuAddress CpuAddressRegionEnding = CpuAddress * MemoryPoolStateArraySize;

            MemoryPoolState[] memoryPoolStateArray = new MemoryPoolState[MemoryPoolStateArraySize];

            for (int i = 0; i < memoryPoolStateArray.Length; i++)
            {
                memoryPoolStateArray[i] = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);
                memoryPoolStateArray[i].SetCpuAddress(CpuAddress + (ulong)i * CpuSize, CpuSize);
            }


            AddressInfo addressInfo = AddressInfo.Create();

            PoolMapper poolMapper = new(DummyProcessHandle, true);

            Assert.That(poolMapper.TryAttachBuffer(out ErrorInfo errorInfo, ref addressInfo, 0, 0), Is.True);

            Assert.That(ResultCode.InvalidAddressInfo, Is.EqualTo(errorInfo.ErrorCode));
            Assert.That(0, Is.EqualTo(errorInfo.ExtraErrorInfo));
            Assert.That(0, Is.EqualTo(addressInfo.ForceMappedDspAddress));

            Assert.That(poolMapper.TryAttachBuffer(out errorInfo, ref addressInfo, CpuAddress, CpuSize), Is.True);

            Assert.That(ResultCode.InvalidAddressInfo, Is.EqualTo(errorInfo.ErrorCode));
            Assert.That(CpuAddress, Is.EqualTo(errorInfo.ExtraErrorInfo));
            Assert.That(DspAddress, Is.EqualTo(addressInfo.ForceMappedDspAddress));

            poolMapper = new PoolMapper(DummyProcessHandle, false);

            Assert.That(poolMapper.TryAttachBuffer(out _, ref addressInfo, 0, 0), Is.False);

            addressInfo.ForceMappedDspAddress = 0;

            Assert.That(poolMapper.TryAttachBuffer(out errorInfo, ref addressInfo, CpuAddress, CpuSize), Is.False);

            Assert.That(ResultCode.InvalidAddressInfo, Is.EqualTo(errorInfo.ErrorCode));
            Assert.That(CpuAddress, Is.EqualTo(errorInfo.ExtraErrorInfo));
            Assert.That(0, Is.EqualTo(addressInfo.ForceMappedDspAddress));

            poolMapper = new PoolMapper(DummyProcessHandle, memoryPoolStateArray.AsMemory(), false);

            Assert.That(poolMapper.TryAttachBuffer(out errorInfo, ref addressInfo, CpuAddressRegionEnding, CpuSize), Is.False);

            Assert.That(ResultCode.InvalidAddressInfo, Is.EqualTo(errorInfo.ErrorCode));
            Assert.That(CpuAddressRegionEnding, Is.EqualTo(errorInfo.ExtraErrorInfo));
            Assert.That(0, Is.EqualTo(addressInfo.ForceMappedDspAddress));
            Assert.That(addressInfo.HasMemoryPoolState, Is.False);

            Assert.That(poolMapper.TryAttachBuffer(out errorInfo, ref addressInfo, CpuAddress, CpuSize), Is.True);

            Assert.That(ResultCode.Success, Is.EqualTo(errorInfo.ErrorCode));
            Assert.That(0, Is.EqualTo(errorInfo.ExtraErrorInfo));
            Assert.That(0, Is.EqualTo(addressInfo.ForceMappedDspAddress));
            Assert.That(addressInfo.HasMemoryPoolState, Is.True);
        }
    }
}
