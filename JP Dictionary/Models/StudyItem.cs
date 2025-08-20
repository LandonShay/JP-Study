using JP_Dictionary.Shared.Methods;

namespace JP_Dictionary.Models
{
    public class StudyItem
    {
        public int Level { get; set; }
        public string Item { get; set; } = string.Empty;
        public StudyType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Meaning { get; set; } = new();
        public string Mnemonic { get; set; } = string.Empty;
        public string ReadingMnemonic { get; set; } = string.Empty;
        public List<string> Onyomi { get; set; } = new();
        public List<string> Kunyomi { get; set; } = new();
        public List<string> Radicals { get; set; } = new();
        public List<string> Kanji { get; set; } = new(); // Vocab only, the kanji that comprise the word (and hiragana/katakana...)
        public string Reading { get; set; } = string.Empty; // Vocab only, kanji readings are in onyomi, kunyomi
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
        public int StudyOrder { get; set; }
    }

    public enum StudyType
    {
        Radical,
        Kanji,
        Vocab,
        Deck,
        Grammar
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
