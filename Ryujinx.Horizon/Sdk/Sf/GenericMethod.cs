using System;
using System.Reflection;

namespace Ryujinx.Horizon.Sdk.Sf
{
    static class GenericMethod
    {
        public static object Invoke(MethodInfo meth, Type[] genericTypes, params object[] args)
        {
            return Create(meth, genericTypes).Invoke(null, args);
        }

        public static T CreateDelegate<T>(MethodInfo meth, params Type[] genericTypes) where T : Delegate
        {
            return Create(meth, genericTypes).CreateDelegate<T>();
        }

        public static MethodInfo Create(MethodInfo meth, params Type[] genericTypes)
        {
            return meth.MakeGenericMethod(genericTypes);
        }
    }
}
