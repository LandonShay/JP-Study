namespace JP_Dictionary.Models
{
    public class Deck
    {
        public string Name { get; set; } = string.Empty;
        public DeckType Type { get; set; }
        public bool Paused { get; set; }
        public int SortOrder { get; set; }
        public string GraphColor { get; set; } = string.Empty;
    }

    public enum DeckType
    {
        Vocab,
        Grammar,
        Kanji
    }
}
