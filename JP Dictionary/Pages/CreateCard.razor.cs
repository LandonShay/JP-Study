using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class CreateCard
    {
        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService ToastService { get; set; }
#nullable enable
        #endregion

        private bool ShowConfirmation { get; set; } = false;
        private string ConflictDeckName { get; set; } = string.Empty;
        private StudyWord PendingWord { get; set; } = new();

        private string Japanese { get; set; } = string.Empty;
        private string Romaji { get; set; } = string.Empty;
        private string Definitions { get; set; } = string.Empty;

        private void Create()
        {
            if (!string.IsNullOrEmpty(Japanese) &&
                !string.IsNullOrEmpty(Romaji) &&
                !string.IsNullOrEmpty(Definitions))
            {
                PendingWord = new StudyWord
                {
                    Japanese = Japanese,
                    Pronounciation = Romaji,
                    Definitions = Definitions,
                    Id = Guid.NewGuid().ToString(),
                    Day = 1,
                    Week = 1,
                    LastStudied = DateTime.MinValue,
                    CorrectStreak = 0
                };

                foreach (var deckName in User.Profile!.Decks)
                {
                    var checkDeck = DeckMethods.LoadDeck(User.Profile, deckName);

                    if (checkDeck.Any(x => x.Japanese == Japanese))
                    {
                        ShowConfirmation = true;
                        ConflictDeckName = deckName;

                        return;
                    }
                }

                AddWordToDeck();
            }
        }

        private void AddWordToDeck()
        {
            var deck = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck);
            deck.Add(PendingWord);

            DeckMethods.UpdateDeck(deck, User.Profile!.Name, User.SelectedDeck);
            ToastService.ShowSuccess($"{PendingWord.Japanese} added to {User.SelectedDeck} deck");

            Japanese = string.Empty;
            Romaji = string.Empty;
            Definitions = string.Empty;

            PendingWord = new();
            ShowConfirmation = false;
        }
    }
}
