using System;

namespace ARMeilleure.Common
{
    static class EnumUtils
    {
        public static int GetCount(Type enumType)
        {
            return Enum.GetNames(enumType).Length;
        }

        public static int GetArrayLengthForEnum(Type enumType)
        {
            var values = Enum.GetValues(enumType);
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            int maxValue = 0;
            foreach (int value in values)
            {
                if (value > maxValue)
                {
                    maxValue = value;
                }
            }

            return maxValue + 1;
        }
    }
}
