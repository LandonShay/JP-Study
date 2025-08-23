namespace JP_Dictionary.Models
{
    public class VocabCard
    {
        public StudyCardType Type { get; set; } 
        public List<string> ReadingAnswers { get; set; } = new();
        public List<string> DefinitionAnswers { get; set; } = new();
        public string OriginalFormatReading { get; set; } = string.Empty;
        public string OriginalFormatDefinition { get; set; } = string.Empty;
        public StudyItem StudyItem { get; set; } = new();
        public GrammarItem GrammarItem { get; set; } = new();
        public bool Correct { get; set; }
        public int CurrentCorrectStreak { get; set; }
    }

    public enum StudyCardType
    {
        Vocab,
        Kanji,
        Radical,
        Grammar,
        Deck
    }
}
