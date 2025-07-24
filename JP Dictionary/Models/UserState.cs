
namespace JP_Dictionary.Models
{
    public class UserState
    {
        public Profile? Profile { get; set; }
        public Deck? SelectedDeck { get; set; }
        public List<StudyKanji> SelectedKanjiGroup { get; set; } = new();
        public StudyKanji? SelectedKanji { get; set; } = null;
        public List<Sentence> Sentences { get; set; } = new();
        public List<StudyKanji> Kanji { get; set; } = new();

        public void ResetSelectedDeck()
        {
            SelectedDeck = null;
        }
    }
}
