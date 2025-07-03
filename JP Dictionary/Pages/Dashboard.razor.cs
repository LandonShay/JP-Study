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
        private DeckType DeckType { get; set; } = DeckType.Vocab;
        private Deck? DeckToDelete { get; set; }

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

            foreach (var deck in User.Profile!.Decks)
            {
                var words = DeckMethods.LoadDeck(User.Profile!, deck.Name);

                var unlockedWords = words.FindAll(x => (x.Week == User.Profile!.CurrentWeek && x.Day <= User.Profile!.CurrentDay) ||
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

        private void ToStudy(Deck deck)
        {
            User.SelectedDeck = deck;
            Nav.NavigateTo("/studyvocab");
        }

        #region Decks
        private void ToViewDeck(Deck deck)
        {
            User.SelectedDeck = deck;
            Nav.NavigateTo("/viewdeck");
        }

        private void CreateDeck()
        {
            if (!User.Profile!.Decks.Select(x => x.Name).Contains(DeckName))
            {
                var deck = new Deck
                {
                    Name = DeckName,
                    Type = DeckType
                };

                HelperMethods.CreateFile($"{User.Profile!.Name}Deck-{DeckName}.csv");
                User.Profile.Decks.Add(deck);

                DeckName = string.Empty;
                DeckType = DeckType.Vocab;

                HelperMethods.SaveProfile(User.Profile);
                LoadDashboard();
            }
        }
        #endregion

        #region Confirm Delete
        private void PromptDeleteDeck(Deck deck)
        {
            DeckToDelete = deck;
        }

        private void ConfirmDeleteDeck()
        {
            if (DeckToDelete != null)
            {
                var deck = DeckMethods.LoadDeck(User.Profile!, DeckToDelete.Name);
                deck.Clear();

                DeckMethods.OverwriteDeck(deck, User.Profile!.Name, DeckToDelete.Name);

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
