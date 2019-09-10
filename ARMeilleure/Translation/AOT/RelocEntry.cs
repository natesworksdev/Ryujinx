namespace ARMeilleure.Translation.AOT
{
    public struct RelocEntry
    {
        public int    Position;
        public string Name;

        public RelocEntry(int position, string name)
        {
            Position = position;
            Name     = name;
        }
    }
}