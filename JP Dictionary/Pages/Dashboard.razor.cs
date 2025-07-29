using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
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

        private int VocabNoviceCount { get; set; }
        private int VocabBeginnerCount { get; set; }
        private int VocabProficientCount { get; set; }
        private int VocabExpertCount { get; set; }
        private int VocabMasteredCount { get; set; }

        private int KanjiNoviceCount { get; set; }
        private int KanjiBeginnerCount { get; set; }
        private int KanjiProficientCount { get; set; }
        private int KanjiExpertCount { get; set; }
        private int KanjiMasteredCount { get; set; }

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
            VocabNoviceCount = 0;
            VocabBeginnerCount = 0;
            VocabProficientCount = 0;
            VocabExpertCount = 0;
            VocabMasteredCount = 0;

            foreach (var deck in User.Profile!.Decks.OrderBy(x => x.SortOrder))
            {
                var words = DeckMethods.LoadDeck(User.Profile!, deck.Name).FindAll(x => x.Unlocked);

                VocabNoviceCount += words.Count(x => x.MasteryTier == MasteryTier.Novice);
                VocabBeginnerCount += words.Count(x => x.MasteryTier == MasteryTier.Beginner);
                VocabProficientCount += words.Count(x => x.MasteryTier == MasteryTier.Proficient);
                VocabExpertCount += words.Count(x => x.MasteryTier == MasteryTier.Expert);
                VocabMasteredCount += words.Count(x => x.MasteryTier == MasteryTier.Mastered);
            }

            User.Kanji = KanjiMethods.LoadUserKanji(User.Profile!);

            var validKanji = User.Kanji.FindAll(x => x.Unlocked && x.Learned);

            KanjiNoviceCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Novice);
            KanjiBeginnerCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Beginner);
            KanjiProficientCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Proficient);
            KanjiExpertCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Expert);
            KanjiMasteredCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Mastered);

            User.SelectedDeck = null;
            User.SelectedKanji = null;
        }

        private void ToStudy(Deck deck)
        {
            User.SelectedDeck = deck;
            Nav.NavigateTo("/studyvocab");
        }

        #region Statistics
        private float GetUnlockedPercentage(List<StudyWord> words)
        {
            return float.Round(words.Count(x => x.Unlocked) / (float)words.Count * 100, 2);
        }

        private float GetLearnedKanjiPercentage()
        {
            var kanji = User.Kanji.FindAll(x => x.Type == KanjiType.Kanji);
            return float.Round(kanji.Count(x => x.Learned) / (float)kanji.Count * 100, 2);
        }

        private float GetLearnedRadicalPercentage()
        {
            var kanji = User.Kanji.FindAll(x => x.Type == KanjiType.Radical);
            return float.Round(kanji.Count(x => x.Learned) / (float)kanji.Count * 100, 2);
        }
        #endregion

        #region Kanji Nav
        private void GoToLearnKanji()
        {
            var kanjiToLearn = KanjiMethods.GetItemsToLearn(User.Kanji);

            if (kanjiToLearn.Count > 0)
            {
                User.TriggerLearnMode = true;
                User.SelectedKanjiGroup = kanjiToLearn;

                Nav.NavigateTo("/kanjireview");
            }
        }

        private void GoToReviewKanji()
        {
            var kanjiToReview = KanjiMethods.GetItemsToReview(User.Kanji);

            User.SelectedKanjiGroup = kanjiToReview;
            Nav.NavigateTo("/studyvocab");
        }
        #endregion

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
