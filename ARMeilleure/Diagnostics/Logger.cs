using ARMeilleure.Translation;
using System;

namespace ARMeilleure.Diagnostics
{
    static class Logger
    {
        public static void StartPass(PassName name)
        {
#if DEBUG
            WriteOutput(name + " pass started...");
#endif
        }

        public static void EndPass(PassName name, ControlFlowGraph cfg)
        {
#if DEBUG
            EndPass(name);

            WriteOutput("IR after " + name + " pass:");

            WriteOutput(IRDumper.GetDump(cfg));
#endif
        }

        public static void EndPass(PassName name)
        {
#if DEBUG
            WriteOutput(name + " pass ended...");
#endif
        }

        private static void WriteOutput(string text)
        {
            Console.WriteLine(text);
        }
    }
}