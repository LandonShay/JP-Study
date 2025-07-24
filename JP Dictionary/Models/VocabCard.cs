namespace JP_Dictionary.Models
{
    public class VocabCard
    {
        public string Word { get; set; } = string.Empty;
        public StudyCardType Type { get; set; } 
        public List<string> ReadingAnswers { get; set; } = new();
        public List<string> DefinitionAnswers { get; set; } = new();
        public string OriginalFormatReading { get; set; } = string.Empty;
        public string OriginalFormatDefinition { get; set; } = string.Empty;
        public StudyWord StudyWord { get; set; } = new();
        public StudyKanji StudyKanji { get; set; } = new();
        public bool Correct { get; set; }
    }

    public enum StudyCardType
    {
        Vocab,
        Kanji,
        Radical,
        Grammar
    }
}
