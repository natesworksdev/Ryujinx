using NUnit.Framework;
using Ryujinx.Memory.Range;
using System.Collections.Generic;

namespace Ryujinx.Tests.Memory.RangeTests
{
    public class PagedMemoryRangeEnumeratorTests
    {
        [Test]
        public void PagedMemoryRangeEnumerator_For3AlignedPages_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 12;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(3, results.Count);

            Assert.AreEqual(0, results[0].Address);
            Assert.AreEqual(PageSize, results[0].Size);

            Assert.AreEqual(4, results[1].Address);
            Assert.AreEqual(PageSize, results[1].Size);

            Assert.AreEqual(8, results[2].Address);
            Assert.AreEqual(PageSize, results[2].Size);
        }

        [Test]
        public void PagedMemoryRangeEnumerator_For2PagesWithPartialFirstAndLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 2;
            const int Size = 6;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(2, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(PageSize - StartAddress, results[0].Size);
            Assert.AreEqual(results[1].Address, results[0].EndAddress);

            Assert.AreEqual(4, results[1].Address);
            Assert.AreEqual(PageSize, results[1].Size);
        }

        [Test]
        public void PagedMemoryRangeEnumerator_For4PagesWithPartialFirst_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 2;
            const int Size = 14;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(4, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(PageSize - StartAddress, results[0].Size);
            Assert.AreEqual(results[1].Address, results[0].EndAddress);

            Assert.AreEqual(4, results[1].Address);
            Assert.AreEqual(PageSize, results[1].Size);
            Assert.AreEqual(results[2].Address, results[1].EndAddress);

            Assert.AreEqual(8, results[2].Address);
            Assert.AreEqual(PageSize, results[2].Size);
            Assert.AreEqual(results[3].Address, results[2].EndAddress);

            Assert.AreEqual(12, results[3].Address);
            Assert.AreEqual(PageSize, results[3].Size);
        }

        [Test]
        public void PagedMemoryRangeEnumerator_For4PagesWithPartialLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 14;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(4, results.Count);

            Assert.AreEqual(0, results[0].Address);
            Assert.AreEqual(PageSize, results[0].Size);
            Assert.AreEqual(results[1].Address, results[0].EndAddress);

            Assert.AreEqual(4, results[1].Address);
            Assert.AreEqual(PageSize, results[1].Size);
            Assert.AreEqual(results[2].Address, results[1].EndAddress);

            Assert.AreEqual(8, results[2].Address);
            Assert.AreEqual(PageSize, results[2].Size);
            Assert.AreEqual(results[3].Address, results[2].EndAddress);

            Assert.AreEqual(12, results[3].Address);
            Assert.AreEqual(Size % PageSize, results[3].Size);
        }
    }
}
