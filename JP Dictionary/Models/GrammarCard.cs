namespace JP_Dictionary.Models
{
    public class GrammarCard
    {
        public GrammarItem GrammarItem { get; set; } = new();
        public string Answer { get; set; } = string.Empty;
        public string JP { get; set; } = string.Empty;
        public string EN { get; set; } = string.Empty;
        public string FullJP { get; set; } = string.Empty;
        public int CurrentCorrectStreak { get; set; }
        public bool Correct { get; set; }
    }
}
