using ARMeilleure.Translation;
using System;

namespace ARMeilleure.Diagnostics
{
    class Logger
    {
        public void StartPass(PassName name)
        {
#if DEBUG
            WriteOutput(name + " pass started...");
#endif
        }

        public void EndPass(PassName name, ControlFlowGraph cfg)
        {
#if DEBUG
            EndPass(name);

            WriteOutput("IR after " + name + " pass:");

            WriteOutput(IRDumper.GetDump(cfg));
#endif
        }

        public void EndPass(PassName name)
        {
#if DEBUG
            WriteOutput(name + " pass ended...");
#endif
        }

        private void WriteOutput(string text)
        {
            Console.WriteLine(text);
        }
    }
}