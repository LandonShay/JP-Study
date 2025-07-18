using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class Dashboard
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        private string DeckName { get; set; } = string.Empty;
        private DeckType DeckType { get; set; } = DeckType.Vocab;
        private Deck? DeckToDelete { get; set; }
        private Deck? DraggedDeck { get; set; }

        private int WordsUnlocked { get; set; }
        private int RemainingWords { get; set; }

        private int NoviceCount { get; set; }
        private int BeginnerCount { get; set; }
        private int ProficientCount { get; set; }
        private int ExpertCount { get; set; }
        private int MasteredCount { get; set; }

        protected override void OnInitialized()
        {
            LoadDashboard();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("enableDragDrop");
            }
        }

        private void LoadDashboard()
        {
            NoviceCount = 0;
            BeginnerCount = 0;
            ProficientCount = 0;
            ExpertCount = 0;
            MasteredCount = 0;

            //var coreDeck = DeckMethods.LoadDeck(User.Profile!, "Core");

            //WordsUnlocked = coreDeck.Count(x => (x.Week == User.Profile!.CurrentWeek && x.Day <= User.Profile!.CurrentDay) ||
            //                                    (x.Week < User.Profile!.CurrentWeek));
            //RemainingWords = coreDeck.Count - WordsUnlocked;

            foreach (var deck in User.Profile!.Decks.OrderBy(x => x.SortOrder))
            {
                var words = DeckMethods.LoadDeck(User.Profile!, deck.Name).FindAll(x => x.Unlocked);

                NoviceCount += words.Count(x => x.MasteryTier == MasteryTier.Novice);
                BeginnerCount += words.Count(x => x.MasteryTier == MasteryTier.Beginner);
                ProficientCount += words.Count(x => x.MasteryTier == MasteryTier.Proficient);
                ExpertCount += words.Count(x => x.MasteryTier == MasteryTier.Expert);
                MasteredCount += words.Count(x => x.MasteryTier == MasteryTier.Mastered);
            }

            User.ResetSelectedDeck();
        }

        private int GetRemainingWordsPerDeck(string deckName)
        {
            var deck = DeckMethods.LoadDeck(User.Profile!, deckName);
            return DeckMethods.LoadWordsToStudy(deck).Count;
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
                DeckMethods.CreateDeck(DeckName, DeckType, User.Profile!);
                LoadDashboard();
            }
            else
            {
                Toast.ShowWarning($"You already have a deck named {DeckName}. Please choose another name");
            }
        }

        private void PauseDeck(Deck deck)
        {
            deck.Paused = !deck.Paused;
            HelperMethods.SaveProfile(User.Profile!);
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

        #region Table Drag
        private void OnDragStart(Deck deck)
        {
            DraggedDeck = deck;
        }

        private void OnDrop(Deck targetDeck)
        {
            if (DraggedDeck == null || DraggedDeck == targetDeck)
            {
                return;
            }

            var draggedIndex = User.Profile!.Decks.IndexOf(DraggedDeck);
            var targetIndex = User.Profile.Decks.IndexOf(targetDeck);

            if (draggedIndex < 0 || targetIndex < 0)
            {
                return;
            }

            User.Profile.Decks.Remove(DraggedDeck);
            User.Profile.Decks.Insert(targetIndex, DraggedDeck);

            for (int i = 0; i < User.Profile.Decks.Count; i++)
            {
                User.Profile.Decks[i].SortOrder = i;
            }

            HelperMethods.SaveProfile(User.Profile);
            DraggedDeck = null;
        }

        private void OnDragEnd()
        {
            DraggedDeck = null;
        }
        #endregion
    }
}
