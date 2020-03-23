using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.State;

namespace Ryujinx.Graphics.Gpu.Engine
{
    partial class Methods
    {
        private bool GetRenderEnable(GpuState state)
        {
            ConditionState condState = state.Get<ConditionState>(MethodOffset.ConditionState);

            switch (condState.Condition)
            {
                case Condition.Always:
                    return true;
                case Condition.Never:
                    return false;
                case Condition.ResultNonZero:
                case Condition.Equal:
                case Condition.NotEqual:
                    return false; // TODO (should we use the host API?)
            }

            Logger.PrintWarning(LogClass.Gpu, $"Invalid conditional render condition \"{condState.Condition}\".");

            return true;
        }
    }
}
