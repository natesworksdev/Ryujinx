using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    internal struct AILOpCodeCall : IAilEmit
    {
        private MethodInfo _mthdInfo;

        public AILOpCodeCall(MethodInfo mthdInfo)
        {
            this._mthdInfo = mthdInfo;
        }

        public void Emit(AILEmitter context)
        {
            context.Generator.Emit(OpCodes.Call, _mthdInfo);
        }
    }
}