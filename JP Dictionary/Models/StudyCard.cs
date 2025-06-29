namespace JP_Dictionary.Models
{
    public class StudyCard
    {
        public string Question { get; set; }
        public CardType CardType { get; set; }
        public string Word { get; set; }
        public List<string> Answers { get; set; } = new();
        public string OriginalFormatAnswer { get; set; }
        public StudyWord StudyWord { get; set; }
        public StudyCard Pair { get; set; }
        public bool Correct { get; set; }
    }

    public enum CardType
    {
        Definition,
        Pronounciation
    }
}
