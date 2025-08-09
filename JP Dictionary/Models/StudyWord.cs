using JP_Dictionary.Shared.Methods;

namespace JP_Dictionary.Models
{
    public class StudyWord
    {
        public string Id { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public string Romaji { get; set; } = string.Empty;
        public string Definitions { get; set; } = string.Empty;
        public DateTime LastStudied { get; set; } = DateTime.MinValue;
        public bool Unlocked { get; set; }
        public int CorrectStreak { get; set; }
        public MasteryTier MasteryTier
        {
            get
            {
                return HelperMethods.GetMasteryTier(CorrectStreak);
            }
        }
        public int StudyOrder { get; set; }
    }

    public enum MasteryTier
    {
        Novice,
        Beginner,
        Proficient,
        Expert,
        Mastered
    }
}
