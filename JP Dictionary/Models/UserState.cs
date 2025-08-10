
namespace JP_Dictionary.Models
{
    public class UserState
    {
        public Profile? Profile { get; set; }

        public Deck? SelectedDeck { get; set; }
        public StudyKanji? SelectedKanji { get; set; } = null;

        public List<StudyKanji> SelectedKanjiGroup { get; set; } = new();
        public List<StudyKanji> PreviousKanjiGroup { get; set; } = new();

        public List<Sentence> Sentences { get; set; } = new();
        public bool TriggerLearnMode { get; set; }
        public bool WipeSelectedKanjiGroup { get; set; }

        public void ResetPreviousKanjiGroup()
        {
            PreviousKanjiGroup = new();
        }

        public void UpdatePreviousKanjiGroup()
        {
            PreviousKanjiGroup = SelectedKanjiGroup.ToList();
        }
    }
}
