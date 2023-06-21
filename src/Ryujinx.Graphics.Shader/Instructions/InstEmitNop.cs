using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.Translation;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Graphics.Shader.Instructions
{
    [SuppressMessage("Style", "IDE0059: Remove unnecessary value assignment")]
    static partial class InstEmit
    {
        public static void Nop(EmitterContext context)
        {
            InstNop op = context.GetOp<InstNop>();

            // No operation.
        }
    }
}
