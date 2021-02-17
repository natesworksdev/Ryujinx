using Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations;

namespace Ryujinx.HLE.HOS.Tamper.Atmosphere.CodeEmitters
{
    /// <summary>
    /// Code type 0xFF1 resumes the current process.
    /// </summary>
    class ResumeProcess
    {
        // FF1?????

        public static void Emit(byte[] instruction, CompilationContext context)
        {
            context.CurrentOperations.Add(new OpProcCtrl(context.Process, false));
        }
    }
}
