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

        private void DeleteDeck(string deckName)
        {

        }
        #endregion

        //private void UnlockNextTier()
        //{
        //    User.Profile!.CurrentDay++;

        //    if (User.Profile.CurrentDay > 7)
        //    {
        //        User.Profile.CurrentDay = 1;
        //        User.Profile.CurrentWeek++;

        //        if (User.Profile.CurrentWeek == byte.MaxValue - 1)
        //        {
        //            User.Profile.CurrentWeek--;
        //        }
        //    }

        //    HelperMethods.SaveProfile(User.Profile);
        //    LoadDashboard();
        //}
    }
}
