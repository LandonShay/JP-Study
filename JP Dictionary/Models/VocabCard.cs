namespace JP_Dictionary.Models
{
    public class VocabCard
    {
        public string Word { get; set; }
        public List<string> ReadingAnswers { get; set; } = new();
        public List<string> DefinitionAnswers { get; set; } = new();
        public string OriginalFormatReading { get; set; }
        public string OriginalFormatDefinition { get; set; }
        public StudyWord StudyWord { get; set; }
        public bool Correct { get; set; }
    }

    public enum CardType
    {
        Definition,
        Pronounciation
    }
}
