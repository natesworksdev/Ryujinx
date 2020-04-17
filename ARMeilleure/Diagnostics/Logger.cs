using ARMeilleure.Translation;
using System;
using System.Diagnostics;

namespace ARMeilleure.Diagnostics
{
    static class Logger
    {
#if M_DEBUG
        private static long _startTime;

        private static readonly long[] _accumulatedTime = new long[(int)PassName.Count];
#endif

        [Conditional("M_DEBUG")]
        public static void StartPass(PassName name)
        {
#if M_DEBUG
            WriteOutput(name + " pass started...");

            _startTime = Stopwatch.GetTimestamp();
#endif
        }

        [Conditional("M_DEBUG")]
        public static void EndPass(PassName name, ControlFlowGraph cfg)
        {
#if M_DEBUG
            EndPass(name);

            WriteOutput("IR after " + name + " pass:");

            WriteOutput(IRDumper.GetDump(cfg));
#endif
        }

        [Conditional("M_DEBUG")]
        public static void EndPass(PassName name)
        {
#if M_DEBUG
            long elapsedTime = Stopwatch.GetTimestamp() - _startTime;

            _accumulatedTime[(int)name] += elapsedTime;

            WriteOutput($"{name} pass ended after {GetMilliseconds(_accumulatedTime[(int)name])} ms...");
#endif
        }

#if M_DEBUG
        private static long GetMilliseconds(long ticks)
        {
            return (long)(((double)ticks / Stopwatch.Frequency) * 1000);
        }

        private static void WriteOutput(string text)
        {
            Console.WriteLine(text);
        }
#endif
    }
}