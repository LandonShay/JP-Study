
namespace JP_Dictionary.Models
{
    public class UserState
    {
        public Profile? Profile { get; set; }

        public Deck? SelectedDeck { get; set; }
        public StudyItem? SelectedKanji { get; set; } = null;

        public List<StudyItem> SelectedKanjiGroup { get; set; } = new();
        public List<StudyItem> PreviousKanjiGroup { get; set; } = new();

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

    public enum Motions
    {
        FadeOut,
        FadeIn,
        ZoomIn,
        ZoomOut,
        SlideLeftIn,
        SlideLeftOut,
        SlideRightIn,
        SlideRightOut,
        FlipLeftIn,
        FlipLeftOut,
        FlipRightIn,
        FlipRightOut
    }
}
