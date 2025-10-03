using JP_Dictionary.Shared.Methods;

namespace JP_Dictionary.Models
{
    public class GrammarItem
    {
        public string JLPTLevel { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public List<GrammarSentence> AboutExamples { get; set; } = new();
        public List<GrammarSentence> Questions { get; set; } = new();
        public GrammarType Type { get; set; }
        public bool Learned { get; set; }
        public bool Unlocked { get; set; }
        public bool Study { get; set; } = true;
        public DateTime LastStudied { get; set; } = DateTime.MinValue;
        public int CorrectStreak { get; set; }
        public MasteryTier MasteryTier
        {
            get
            {
                return HelperMethods.GetMasteryTier(CorrectStreak);
            }
        }
    }

    public enum GrammarType
    {
        None,
        Casual,
        Polite,
        Formal
    }
}
