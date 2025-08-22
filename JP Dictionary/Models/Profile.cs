namespace JP_Dictionary.Models
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public int LoginStreak { get; set; }
        public int KanjiLevel { get; set; } = 1;
        public string GrammarLevel { get; set; } = "N5";
        public bool AutoSpeak { get; set; } = true;
        public List<Deck> Decks { get; set; } = new();
    }
}
