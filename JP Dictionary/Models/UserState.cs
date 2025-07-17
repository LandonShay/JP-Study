
namespace JP_Dictionary.Models
{
    public class UserState
    {
        public Profile? Profile { get; set; }
        public Deck? SelectedDeck { get; set; }
        public List<Sentence> Sentences { get; set; } = new();

        public void ResetSelectedDeck()
        {
            SelectedDeck = null;
        }
    }
}
