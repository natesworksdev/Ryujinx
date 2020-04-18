#if M_DEBUG
using System.Collections.Concurrent;
using System.Collections.Generic;
#endif

namespace ARMeilleure.Diagnostics
{
    static class Symbols
    {
#if M_DEBUG

        private static readonly ConcurrentDictionary<ulong, string> _symbols;
        private static readonly List<(ulong Start, ulong End, ulong ElementSize, string Name)> _rangedSymbols;

        static Symbols()
        {
            _symbols = new ConcurrentDictionary<ulong, string>();
            _rangedSymbols = new List<(ulong Start, ulong End, ulong ElementSize, string Name)>();
        }
#endif

        public static string Get(ulong address)
        {
#if M_DEBUG
            if (_symbols.TryGetValue(address, out string result))
            {
                return result;
            }

            lock (_rangedSymbols)
            {
                foreach ((ulong Start, ulong End, ulong ElementSize, string Name) in _rangedSymbols)
                {
                    if (address >= Start && address <= End)
                    {
                        return Name + "_" + (address - Start) / ElementSize;
                    }
                }
            }
#endif

            return null;
        }

        public static void Add(ulong address, string name)
        {
#if M_DEBUG
            _symbols.TryAdd(address, name);
#endif
        }

        public static void Add(ulong address, ulong size, ulong elemSize, string name)
        {
#if M_DEBUG
            lock (_rangedSymbols)
            {
                _rangedSymbols.Add((address, address + size, elemSize, name));
            }
#endif
        }
    }
}
