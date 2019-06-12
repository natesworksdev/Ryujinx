namespace ARMeilleure
{
    public static class Optimizations
    {
        public static bool AssumeStrictAbiCompliance { get; set; }

        public static bool FastFP { get; set; } = true;

        public static bool UseSseIfAvailable    { get; set; }
        public static bool UseSse2IfAvailable   { get; set; }
        public static bool UseSse3IfAvailable   { get; set; }
        public static bool UseSsse3IfAvailable  { get; set; }
        public static bool UseSse41IfAvailable  { get; set; }
        public static bool UseSse42IfAvailable  { get; set; }
        public static bool UsePopCntIfAvailable { get; set; }

        internal static bool UseSse    { get; set; } = true;
        internal static bool UseSse2   { get; set; } = true;
        internal static bool UseSse3   { get; set; } = true;
        internal static bool UseSsse3  { get; set; } = true;
        internal static bool UseSse41  { get; set; } = true;
        internal static bool UseSse42  { get; set; } = true;
        internal static bool UsePopCnt { get; set; } = true;
    }
}