namespace JP_Dictionary.Models
{
    public class Deck
    {
        public string ProfileName { get; set; } = string.Empty;
        public string DeckName { get; set; } = string.Empty;
        public List<StudyWord> Words { get; set; } = new();
    }
}
