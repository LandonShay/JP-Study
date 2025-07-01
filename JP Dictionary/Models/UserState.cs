namespace JP_Dictionary.Models
{
    public class UserState
    {
        public Profile? Profile { get; set; }
        public string SelectedDeck { get; set; } = string.Empty;

        public void ResetSelectedDeck()
        {
            SelectedDeck = string.Empty;
        }
    }
}
