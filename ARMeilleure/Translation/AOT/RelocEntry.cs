namespace ARMeilleure.Translation.AOT
{
    struct RelocEntry
    {
        public int    Position;
        public string Name;

        public RelocEntry(int position, string name)
        {
            Position = position;
            Name     = name;
        }

        public override string ToString()
        {
            return $"({nameof(Position)} = {Position}, {nameof(Name)} = {Name})";
        }
    }
}
