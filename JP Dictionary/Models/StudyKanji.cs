
using JP_Dictionary.Shared;

namespace JP_Dictionary.Models
{
    public class StudyKanji
    {
        public int Level { get; set; }
        public string Item { get; set; } = string.Empty;
        public KanjiType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Meaning { get; set; } = new();
        public string Mnemonic { get; set; } = string.Empty;
        public string ReadingMnemonic { get; set; } = string.Empty;
        public List<string> Onyomi { get; set; } = new();
        public List<string> Kunyomi { get; set; } = new();
        public List<string> Radicals { get; set; } = new();
        public bool Learned { get; set; }
        public bool Unlocked { get; set; }
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

    public enum KanjiType
    {
        Radical,
        Kanji
    }
}
