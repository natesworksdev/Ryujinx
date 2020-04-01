using System;

namespace ARMeilleure.Common
{
    static class EnumUtils
    {
        public static int GetCount(Type enumType)
        {
            return Enum.GetNames(enumType).Length;
        }

        public static int GetMaxValue(Type enumType)
        {
            int maxValue = int.MinValue;
            foreach (int item in Enum.GetValues(enumType))
            {
                if (item > maxValue)
                {
                    maxValue = item;
                }
            }

            return maxValue;
        }
    }
}
