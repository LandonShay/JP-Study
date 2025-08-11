﻿using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
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

        private BarConfig KRVBarConfig { get; set; } = new();
        private BarConfig JLPTBarConfig { get; set; } = new();

        private List<StudyKanji> Kanji { get; set; } = new();
        private List<StudyKanji> KanjiVocab { get; set; } = new();

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

        #region Loading
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

            Kanji = KanjiMethods.LoadUserKanji(User.Profile!);
            KanjiVocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!);

            var validKanji = Kanji.FindAll(x => x.Learned);
            validKanji.AddRange(KanjiVocab.FindAll(x => x.Learned));

            KanjiNoviceCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Novice);
            KanjiBeginnerCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Beginner);
            KanjiProficientCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Proficient);
            KanjiExpertCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Expert);
            KanjiMasteredCount = validKanji.Count(x => x.MasteryTier == MasteryTier.Mastered);

            User.SelectedDeck = null;
            User.SelectedKanji = null;

            LoadGraphs();
        }

        private void LoadGraphs()
        {
            var configs = new List<BarConfig>() { KRVBarConfig, JLPTBarConfig };

            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];

                config.Options = new BarOptions
                {
                    Legend = new Legend { Display = false },
                    Scales = new BarScales 
                    {
                        XAxes =
                        [
                            new BarCategoryAxis
                            {
                                Ticks = new CategoryTicks { FontColor = "#aaa" },
                                GridLines = new GridLines { Color = "rgba(255,255,255,0.1)" }
                            }
                        ],
                        YAxes =
                        [
                            new BarLinearCartesianAxis
                            {
                                Ticks = new LinearCartesianTicks { FontColor = "#aaa" },
                                GridLines = new GridLines { Color = "rgba(255,255,255,0.1)" }
                            }
                        ]
                    }
                };

                foreach (var tier in new[] { "Novice", "Beginner", "Proficient", "Expert", "Mastered" })
                {
                    config.Data.Labels.Add(tier);
                }

                int[] tierCounts = i == 0 ? [KanjiNoviceCount, KanjiBeginnerCount, KanjiProficientCount, KanjiExpertCount, KanjiMasteredCount] :
                                            [VocabNoviceCount, VocabBeginnerCount, VocabProficientCount, VocabExpertCount, VocabMasteredCount];

                var dataset = new BarDataset<int>(tierCounts)
                {
                    BorderWidth = 1,
                    BackgroundColor = new[]
                    {
                        "rgba(100, 149, 237, 1)", // Novice - Cornflower Blue
                        "rgba(72, 209, 204, 1)",  // Beginner - Turquoise
                        "rgba(144, 238, 144, 1)", // Proficient - Light Green
                        "rgba(255, 215, 0, 1)",   // Expert - Gold
                        "rgba(255, 99, 71, 1)"    // Mastered - Tomato
                    },
                };

                config.Data.Datasets.Add(dataset);
            }
        }
        #endregion

        #region Statistics
        private float GetUnlockedPercentage(List<StudyWord> words)
        {
            return float.Round(words.Count(x => x.Unlocked) / (float)words.Count * 100, 2);
        }

        private float GetLearnedPercentage(KanjiType type)
        {
            if (type != KanjiType.Vocab)
            {
                var item = Kanji.FindAll(x => x.Type == type);
                return float.Round(item.Count(x => x.Learned) / (float)item.Count * 100, 2);
            }

            return float.Round(KanjiVocab.Count(x => x.Learned) / (float)KanjiVocab.Count * 100, 2);
        }

        private float LevelProgress()
        {
            var levelItems = Kanji.FindAll(x => x.Level == User.Profile!.KanjiLevel);

            if (levelItems.Count == 0)
            {
                return 0;
            }

            var beginnerOrAbove = levelItems.Count(x => x.MasteryTier >= MasteryTier.Beginner);
            var target = levelItems.Count * 0.9f;

            var progress = beginnerOrAbove / target;

            return float.Round(MathF.Min(progress, 1f) * 100, 2);
        }
        #endregion

        #region Nav
        private void GoToLearnKanji()
        {
            var kanjiToLearn = KanjiMethods.GetItemsToLearn(Kanji);

            if (kanjiToLearn.Count > 0)
            {
                User.TriggerLearnMode = true;
                User.SelectedKanjiGroup = kanjiToLearn;

                Nav.NavigateTo("/kanjireview");
            }
        }

        private void GoToReviewKanji(KanjiType type)
        {
            if (type != KanjiType.Vocab)
            {
                User.SelectedKanjiGroup = KanjiMethods.GetItemsToReview(Kanji);
            }
            else
            {
                User.SelectedKanjiGroup = KanjiMethods.GetItemsToReview(KanjiVocab);
            }

            Nav.NavigateTo("/studyvocab");
        }

        private void ToStudy(Deck deck)
        {
            User.SelectedDeck = deck;
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
