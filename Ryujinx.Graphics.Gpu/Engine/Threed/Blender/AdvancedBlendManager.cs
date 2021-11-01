using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    /// <summary>
    /// Advanced blend manager.
    /// </summary>
    class AdvancedBlendManager
    {
        private const int InstructionRamSize = 128;
        private const int InstructionRamSizeMask = InstructionRamSize - 1;

        private readonly DeviceStateWithShadow<ThreedClassState> _state;

        private readonly uint[] _code;
        private int _ip;

        public AdvancedBlendManager(DeviceStateWithShadow<ThreedClassState> state)
        {
            _state = state;
            _code = new uint[InstructionRamSize];
        }

        /// <summary>
        /// Sets the start offset of the blend microcode in memory.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void LoadBlendUcodeStart(int argument)
        {
            _ip = argument;
        }

        /// <summary>
        /// Pushes one word of blend microcode.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void LoadBlendUcodeInstruction(int argument)
        {
            _code[_ip++ & InstructionRamSizeMask] = (uint)argument;
        }

        public bool TryGetAdvancedBlend(out AdvancedBlendDescriptor descriptor)
        {
            Span<uint> currentCode = new Span<uint>(_code);
            byte codeLength = (byte)_state.State.BlendUcodeSize;

            if (currentCode.Length > codeLength)
            {
                currentCode = currentCode.Slice(0, codeLength);
            }

            foreach (var entry in AdvancedBlendFunctions.Table)
            {
                if (currentCode.Length != entry.Code.Length || !currentCode.SequenceEqual(entry.Code))
                {
                    continue;
                }

                if (entry.Constants != null)
                {
                    bool constantsMatch = true;

                    for (int i = 0; i < entry.Constants.Length; i++)
                    {
                        RgbFloat constant = entry.Constants[i];
                        RgbHalf constant2 = _state.State.BlendUcodeConstants[i];

                        if ((Half)constant.R != constant2.UnpackR() ||
                            (Half)constant.G != constant2.UnpackG() ||
                            (Half)constant.B != constant2.UnpackB())
                        {
                            constantsMatch = false;
                            break;
                        }
                    }

                    if (!constantsMatch)
                    {
                        continue;
                    }
                }

                if (entry.Alpha.Enable != _state.State.BlendUcodeEnable)
                {
                    continue;
                }

                if (entry.Alpha.Enable == BlendUcodeEnable.EnableRGBA &&
                    (entry.Alpha.AlphaOp != _state.State.BlendStateCommon.AlphaOp ||
                    entry.Alpha.AlphaSrcFactor != _state.State.BlendStateCommon.AlphaSrcFactor ||
                    entry.Alpha.AlphaDstFactor != _state.State.BlendStateCommon.AlphaDstFactor))
                {
                    continue;
                }

                descriptor = new AdvancedBlendDescriptor(entry.Mode, entry.Overlap, entry.SrcPreMultiplied);
                return true;
            }

            descriptor = default;
            return false;
        }
    }
}