using NUnit.Framework;
using Ryujinx.Memory.Range;
using System.Collections.Generic;

namespace Ryujinx.Tests.Memory.RangeTests
{
    public class PagedMemoryRangeCoalescingEnumeratorTests
    {
        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For3AlignedPages_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 12;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
                results.Add(memoryRange);

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(0, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For2PagesWithPartialFirstAndLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 2;
            const int Size = 6;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
                results.Add(memoryRange);

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For4PagesWithPartialFirst_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 2;
            const int Size = 14;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
                results.Add(memoryRange);

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For4PagesWithPartialLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 14;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
                results.Add(memoryRange);

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }
    }
}
