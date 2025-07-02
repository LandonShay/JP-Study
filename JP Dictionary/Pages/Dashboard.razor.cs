using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class Dashboard
    {
        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        private string DeckName { get; set; } = string.Empty;
        private string? DeckToDelete { get; set; }

        private int WordsUnlocked { get; set; }
        private int RemainingWords { get; set; }

        private int StartingCount { get; set; }
        private int FamiliarCount { get; set; }
        private int GoodCount { get; set; }
        private int ExpertCount { get; set; }

        protected override void OnInitialized()
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            StartingCount = 0;
            FamiliarCount = 0;
            GoodCount = 0;
            ExpertCount = 0;

            var coreDeck = DeckMethods.LoadDeck(User.Profile!, "Core");

            WordsUnlocked = coreDeck.Count(x => (x.Week == User.Profile!.CurrentWeek && x.Day <= User.Profile!.CurrentDay) ||
                                                (x.Week < User.Profile!.CurrentWeek));
            RemainingWords = coreDeck.Count - WordsUnlocked;

            foreach (var deckName in User.Profile!.Decks)
            {
                var deck = DeckMethods.LoadDeck(User.Profile!, deckName);

                var unlockedWords = deck.FindAll(x => (x.Week == User.Profile!.CurrentWeek && x.Day <= User.Profile!.CurrentDay) ||
                                                      (x.Week < User.Profile!.CurrentWeek));

                StartingCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Starting);
                FamiliarCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Familiar);
                GoodCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Good);
                ExpertCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Expert);
            }

            User.ResetSelectedDeck();
        }

        private int GetRemainingWordsPerDeck(string deckName)
        {
            var deck = DeckMethods.LoadDeck(User.Profile!, deckName);
            return DeckMethods.LoadWordsToStudy(User.Profile!, deck).Count;
        }

        private void ToStudy(string deckName)
        {
            User.SelectedDeck = deckName;
            Nav.NavigateTo("/studyvocab");
        }

        #region Decks
        private void ToViewDeck(string deck)
        {
            User.SelectedDeck = deck;
            Nav.NavigateTo("/viewdeck");
        }

        private void CreateDeck()
        {
            if (!User.Profile!.Decks.Contains(DeckName))
            {
                HelperMethods.CreateFile($"{User.Profile!.Name}Deck-{DeckName}.csv");
                User.Profile.Decks.Add(DeckName);

                DeckName = string.Empty;

                HelperMethods.SaveProfile(User.Profile);
                LoadDashboard();
            }
        }
        #endregion

        #region Confirm Delete
        private void PromptDeleteDeck(string deckName)
        {
            DeckToDelete = deckName;
        }

        private void ConfirmDeleteDeck()
        {
            if (DeckToDelete != null)
            {
                var deck = DeckMethods.LoadDeck(User.Profile!, DeckToDelete);
                deck.Clear();

                DeckMethods.OverwriteDeck(deck, User.Profile!.Name, DeckToDelete);
                User.Profile.Decks.Remove(DeckToDelete);
                HelperMethods.SaveProfile(User.Profile!);
                HelperMethods.DeleteFile($"{User.Profile!.Name}Deck-{DeckToDelete}.csv");

                DeckToDelete = null;
                LoadDashboard();
            }
        }

        private void CancelDeleteDeck()
        {
            DeckToDelete = null;
        }
        #endregion
    }
}
