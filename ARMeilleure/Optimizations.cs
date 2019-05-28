namespace ARMeilleure
{
    public static class Optimizations
    {
        public static bool AssumeStrictAbiCompliance { get; set; }

        public static bool FastFP { get; set; } = true;

        public static bool UseSseIfAvailable   { get; set; }
        public static bool UseSse2IfAvailable  { get; set; }
        public static bool UseSse3IfAvailable  { get; set; }
        public static bool UseSsse3IfAvailable { get; set; }
        public static bool UseSse41IfAvailable { get; set; }
        public static bool UseSse42IfAvailable { get; set; }
    }
}