using JP_Dictionary.Shared;

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
                var streak = HelperMethods.GetDelayFromStreak(CorrectStreak);
                return GetMasteryTier(streak);
            }
        }
        public string Audio { get; set; } = string.Empty;
        public int StudyOrder { get; set; }

        private static MasteryTier GetMasteryTier(int streak)
        {
            if (streak < 8) // days 1-7
            {
                return MasteryTier.Starting;
            }
            else if (streak < 15) // days 8-14
            {
                return MasteryTier.Familiar;
            }
            else if (streak < 30) // days 14-30
            {
                return MasteryTier.Good;
            }

            return MasteryTier.Expert; // days 30+
        }

        public bool HasAudio()
        {
            return Audio != string.Empty;
        }
    }

    public enum MasteryTier
    {
        Starting,
        Familiar,
        Good,
        Expert
    }
}
