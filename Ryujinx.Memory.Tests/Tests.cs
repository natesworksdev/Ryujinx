using NUnit.Framework;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Memory.Tests
{
    public class Tests
    {
        private const ulong MemorySize = 0x8000;

        private MemoryBlock _memoryBlock;

        [SetUp]
        public void Setup()
        {
            _memoryBlock = new MemoryBlock(MemorySize);

            // Ensure the entire memory block is written and zero-initialized.
            _memoryBlock.Write(0, new byte[MemorySize]);
        }

        [TearDown]
        public void Teardown()
        {
            _memoryBlock.Dispose();
        }

        [Test]
        public void Test_Read()
        {
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0x1234abcd);

            Assert.AreEqual(_memoryBlock.Read<int>(0x2020), 0x1234abcd);
        }

        [Test]
        public void Test_Write()
        {
            _memoryBlock.Write(0x2040, 0xbadc0de);

            Assert.AreEqual(Marshal.ReadInt32(_memoryBlock.Pointer, 0x2040), 0xbadc0de);
        }

        [Test]
        public void Test_QueryModified_SinglePage_Unaligned()
        {
            MemoryRange rangeX20 = _memoryBlock.CreateMemoryRange(0x2020, 4);
            MemoryRange rangeX1C = _memoryBlock.CreateMemoryRange(0x201c, 4);
            MemoryRange rangeX24 = _memoryBlock.CreateMemoryRange(0x2024, 4);

            Assert.IsFalse(rangeX20.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0x1234abcd);

            Assert.IsFalse(rangeX1C.QueryModified());
            Assert.IsFalse(rangeX24.QueryModified());
            Assert.IsTrue(rangeX20.QueryModified());

            Marshal.WriteByte(_memoryBlock.Pointer, 0x201f, 0xaa);

            Assert.IsFalse(rangeX20.QueryModified());
            Assert.IsTrue(rangeX1C.QueryModified());
            Assert.IsFalse(rangeX24.QueryModified());

            MemoryRange range3 = _memoryBlock.CreateMemoryRange(0x2000, 1);

            Marshal.WriteByte(_memoryBlock.Pointer, 0x2000, 0xaa);

            Assert.IsTrue(range3.QueryModified());
            Assert.IsFalse(range3.QueryModified());
        }

        [Test]
        public void Test_QueryModified_SinglePage_Aligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0x2000, 0x1000);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3000, 0x1234abcd);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2100, 0x1234abcd);

            Assert.IsTrue(range.QueryModified());

            Marshal.WriteByte(_memoryBlock.Pointer, 0x1fff, 0xaa);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteByte(_memoryBlock.Pointer, 0x2001, 0xaa);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0x2000, 0x1000);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
        }

        [Test]
        public void Test_QueryModified_TwoPages_Unaligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0x2020, 0x1500);

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2010, 0x1234abcd);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3f00, 0x1234abcd);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0x1234abcd);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0x2020, 0x1500);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2010, 0xbadcafe);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3f00, 0xbadc0de);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3020, 0xbadc0de);

            Assert.IsTrue(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());
        }

        [Test]
        public void Test_QueryModified_TwoPages_Aligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0x2000, 0x2000);

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x4010, 0x1234abcd);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x1f00, 0x1234abcd);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0x1234abcd);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0x4000, 0x2000);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x6010, 0xbadcafe);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x7f00, 0xbadc0de);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteByte(_memoryBlock.Pointer, 0x5fff, 0xaa);

            Assert.IsTrue(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());
            Assert.IsFalse(range2.QueryModified());
        }

        [Test]
        public void Test_QueryModified_MultiPages_Unaligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0x2020, 0x5300);

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x7320, 0x1234abcd);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x201c, 0x1234abcd);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x731c, 0x1234abcd);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0x2020, 0x1500);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0xbadcafe);

            Assert.IsTrue(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3020, 0xbadc0de);

            Assert.IsTrue(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());
        }

        [Test]
        public void Test_QueryModified_MultiPages_Aligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0x2000, 0x5000);

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x1f00, 0x1234abcd);

            Assert.IsFalse(range.QueryModified());

            Marshal.ReadInt32(_memoryBlock.Pointer, 0x2000);

            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x6fff, 0x1234abcd);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0x4000, 0x2000);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x6010, 0xbadcafe);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x7f00, 0xbadc0de);

            Assert.IsFalse(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range.QueryModified());

            Marshal.WriteByte(_memoryBlock.Pointer, 0x5fff, 0xaa);

            Assert.IsTrue(range2.QueryModified());
            Assert.IsTrue(range.QueryModified());
            Assert.IsFalse(range2.QueryModified());
            Assert.IsFalse(range.QueryModified());
        }

        [Test]
        public void Test_QueryModified2_SinglePage_Unaligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0x2000, 0x6000);

            Span<byte> data = new byte[0x8000];

            Assert.IsFalse(range.QueryModified(0x20, 4, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0x1234abcd);

            Assert.IsFalse(range.QueryModified(0x1c, 4, data));
            Assert.IsFalse(range.QueryModified(0x24, 4, data));
            Assert.IsTrue(range.QueryModified(0x20, 4, data));

            Marshal.WriteByte(_memoryBlock.Pointer, 0x201f, 0xaa);

            Assert.IsFalse(range.QueryModified(0x20, 4, data));
            Assert.IsTrue(range.QueryModified(0x1c, 4, data));
            Assert.IsFalse(range.QueryModified(0x24, 4, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2000, 0xcafe);

            Assert.IsFalse(range.QueryModified(4, 4, data));
            Assert.IsFalse(range.QueryModified(8, 4, data));
            Assert.IsTrue(range.QueryModified(0, 4, data));
        }

        [Test]
        public void Test_QueryModified2_SinglePage_Aligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0, 0x8000);

            Span<byte> data = new byte[0x8000];

            Assert.IsFalse(range.QueryModified(0x2000, 0x1000, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3000, 0x1234abcd);

            Assert.IsFalse(range.QueryModified(0x2000, 0x1000, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2100, 0x1234abcd);

            Assert.IsTrue(range.QueryModified(0x2000, 0x1000, data));

            Marshal.WriteByte(_memoryBlock.Pointer, 0x1fff, 0xaa);

            Assert.IsFalse(range.QueryModified(0x2000, 0x1000, data));

            Marshal.WriteByte(_memoryBlock.Pointer, 0x2001, 0xaa);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0, 0x8000);

            Span<byte> data2 = new byte[0x8000];

            Assert.IsFalse(range2.QueryModified(0x2000, 0x1000, data2));
            Assert.IsTrue(range.QueryModified(0x2000, 0x1000, data));
        }

        [Test]
        public void Test_QueryModified2_TwoPages_Unaligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0, 0x8000);

            Span<byte> data = new byte[0x8000];

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2010, 0x1234abcd);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3f00, 0x1234abcd);

            Assert.IsFalse(range.QueryModified(0x2020, 0x1500, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0x1234abcd);

            Assert.IsTrue(range.QueryModified(0x2020, 0x1500, data));
            Assert.IsFalse(range.QueryModified(0x2020, 0x1500, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2010, 0xbadcafe);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3f00, 0xbadc0de);

            Assert.IsFalse(range.QueryModified(0x2020, 0x1500, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3020, 0xbadc0de);

            Assert.IsTrue(range.QueryModified(0x2020, 0x1500, data));
            Assert.IsFalse(range.QueryModified(0x2020, 0x1500, data));
            Assert.IsTrue(range.QueryModified(0x2000, 0x1500, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0, 0xbadc0de);

            Assert.IsTrue(range.QueryModified(0, 0x1500, data));
            Assert.IsFalse(range.QueryModified(0, 0x1500, data));
            Assert.IsFalse(range.QueryModified(0x1000, 0x1500, data));
        }

        [Test]
        public void Test_QueryModified2_MultiPages_Unaligned()
        {
            MemoryRange range = _memoryBlock.CreateMemoryRange(0, 0x8000);

            Span<byte> data = new byte[0x8000];

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x7320, 0x1234abcd);
            Marshal.WriteInt32(_memoryBlock.Pointer, 0x201c, 0x1234abcd);

            Assert.IsFalse(range.QueryModified(0x2020, 0x5300, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x731c, 0x1234abcd);

            MemoryRange range2 = _memoryBlock.CreateMemoryRange(0, 0x8000);

            Span<byte> data2 = new byte[0x8000];

            Assert.IsFalse(range2.QueryModified(0x2020, 0x1500, data2));
            Assert.IsTrue(range.QueryModified(0x2020, 0x5300, data));
            Assert.IsFalse(range.QueryModified(0x2020, 0x5300, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x2020, 0xbadcafe);

            Assert.IsTrue(range2.QueryModified(0x2020, 0x1500, data2));
            Assert.IsTrue(range.QueryModified(0x2020, 0x5300, data));
            Assert.IsFalse(range2.QueryModified(0x2020, 0x1500, data2));
            Assert.IsFalse(range.QueryModified(0x2020, 0x5300, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3020, 0xbadc0de);

            Assert.IsTrue(range2.QueryModified(0x2020, 0x1500, data2));
            Assert.IsTrue(range.QueryModified(0x2020, 0x5300, data));
            Assert.IsFalse(range2.QueryModified(0x2020, 0x1500, data2));
            Assert.IsFalse(range.QueryModified(0x2020, 0x5300, data));

            Marshal.WriteInt32(_memoryBlock.Pointer, 0x3020, 0);

            Assert.IsTrue(range2.QueryModified(0x3010, 0x1500, data2));
            Assert.IsTrue(range.QueryModified(0x3010, 0x4300, data));
            Assert.IsFalse(range2.QueryModified(0x3010, 0x1500, data2));
            Assert.IsFalse(range.QueryModified(0x3010, 0x4300, data));
        }
    }
}