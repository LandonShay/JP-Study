namespace JP_Dictionary.Models
{
    public class VocabCard
    {
        public string Word { get; set; } = string.Empty;
        public List<string> ReadingAnswers { get; set; } = new();
        public List<string> DefinitionAnswers { get; set; } = new();
        public string OriginalFormatReading { get; set; } = string.Empty;
        public string OriginalFormatDefinition { get; set; } = string.Empty;
        public StudyWord StudyWord { get; set; } = new();
        public bool Correct { get; set; }
        public int CurrentCorrectStreak { get; set; }
    }
}
