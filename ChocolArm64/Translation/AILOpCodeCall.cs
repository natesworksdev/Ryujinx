using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    struct AilOpCodeCall : IailEmit
    {
        private MethodInfo _mthdInfo;

        public AilOpCodeCall(MethodInfo mthdInfo)
        {
            _mthdInfo = mthdInfo;
        }

        public void Emit(AilEmitter context)
        {
            context.Generator.Emit(OpCodes.Call, _mthdInfo);
        }
    }
}