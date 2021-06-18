using ARMeilleure.Translation.TTC;

namespace ARMeilleure.Translation.PTC
{
    /// <summary>
    /// Types of <see cref="Symbol"/>.
    /// </summary>
    enum SymbolType : byte
    {
        /// <summary>
        /// Refers to nothing, i.e no symbol.
        /// </summary>
        None = 0,

        /* Ptc or Ttc (FunctionTable only). */

        /// <summary>
        /// Refers to an entry in <see cref="Translator.FunctionTable"/>.
        /// </summary>
        FunctionTable = 1,

        /// <summary>
        /// Refers to an entry in <see cref="Delegates"/>.
        /// </summary>
        DelegateTable = 2,

        /// <summary>
        /// Refers to a special symbol which is handled by <see cref="Ptc.PatchCode"/>.
        /// </summary>
        Special = 3,

        /* Ttc only. */

        /// <summary>
        /// Refers to a symbol which is handled by <see cref="Ttc.PatchCodeDyn"/>.
        /// </summary>
        DynFunc = byte.MaxValue - 1,

        /// <summary>
        /// Refers to a symbol which is handled by <see cref="Ttc.PatchCodeDyn"/>.
        /// </summary>
        DynFuncAdrp = byte.MaxValue - 0
    }
}
