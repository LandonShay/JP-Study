namespace JP_Dictionary.Models
{
    public class UserState
    {
        public Profile? Profile { get; set; }
        public Deck? SelectedDeck { get; set; }

        public void ResetSelectedDeck()
        {
            SelectedDeck = null;
        }
    }
}
