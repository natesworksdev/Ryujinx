using System.Text;

namespace ARMeilleure.Translation.AOT
{
    internal static class EncodingCache
    {
        internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    }
}