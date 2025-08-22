using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
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
        [Inject] public ToastService Toast { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public AnimationService Anim { get; set; }
#nullable enable
        #endregion

        private Motion Animate { get; set; } = default!;

        private BarConfig KRVBarConfig { get; set; } = new();
        private BarConfig JLPTBarConfig { get; set; } = new();
        private BarConfig GrammarBarConfig { get; set; } = new();

        private List<StudyItem> Kanji { get; set; } = new();
        private List<StudyItem> KanjiVocab { get; set; } = new();
        private List<GrammarItem> Grammar { get; set; } = new();

        private string DeckName { get; set; } = string.Empty;
        private DeckType DeckType { get; set; } = DeckType.Vocab;
        private string DeckColor { get; set; } = string.Empty;
        private Deck? DeckToDelete { get; set; }
        private Deck? DraggedDeck { get; set; }

        protected override void OnInitialized()
        {
            LoadDashboard();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Anim.OnAnimate += AnimatePage;

                await AnimatePage(Motions.ZoomIn);
                await JS.InvokeVoidAsync("enableDragDrop");
            }
        }

        #region Loading
        private void LoadDashboard()
        {
            Kanji = KanjiMethods.LoadUserKanji(User.Profile!);
            Grammar = GrammarMethods.LoadUserGrammar(User.Profile!);
            KanjiVocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!);

            User.SelectedDeck = null;
            User.SelectedKanji = null;
            User.SelectedGrammar = null;

            DeckName = string.Empty;
            DeckColor = string.Empty;

            LoadGraphs();
        }

        private void LoadGraphs()
        {
            var vocabData = new List<int>();
            var kanjiData = new List<int>();
            var radicalData = new List<int>();
            var configs = new List<BarConfig>() { KRVBarConfig, JLPTBarConfig, GrammarBarConfig };

            foreach (var config in configs)
            {
                config.Data.Datasets.Clear();

                config.Options = new BarOptions
                {
                    Legend = new Legend { Display = true, Labels = new LegendLabels { FontColor = "#aaa" } },
                    Scales = new BarScales
                    {
                        XAxes =
                            [
                                new BarCategoryAxis
                                {
                                    Stacked = true,
                                    Ticks = new CategoryTicks { FontColor = "#aaa" },
                                    GridLines = new GridLines { Color = "rgba(255,255,255,0.1)" }
                                }
                            ],
                        YAxes =
                            [
                                new BarLinearCartesianAxis
                                {
                                    Stacked = true,
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
            }

            foreach (var tier in new[] { MasteryTier.Novice, MasteryTier.Beginner, MasteryTier.Proficient, MasteryTier.Expert, MasteryTier.Mastered })
            {
                vocabData.Add(KanjiVocab.Count(x => x.Type == StudyType.Vocab && x.Learned && x.MasteryTier == tier));
                kanjiData.Add(Kanji.Count(x => x.Type == StudyType.Kanji && x.Learned && x.MasteryTier == tier));
                radicalData.Add(Kanji.Count(x => x.Type == StudyType.Radical && x.Learned && x.MasteryTier == tier));
            }

            KRVBarConfig.Data.Datasets.Add(new BarDataset<int>(vocabData)
            {
                Label = "Vocab",
                BackgroundColor = "#8174A0",
                HoverBackgroundColor = "#8174A0",
                BorderWidth = 1
            });

            KRVBarConfig.Data.Datasets.Add(new BarDataset<int>(kanjiData)
            {
                Label = "Kanji",
                BackgroundColor = "#A888B5",
                HoverBackgroundColor = "#A888B5",
                BorderWidth = 1
            });

            KRVBarConfig.Data.Datasets.Add(new BarDataset<int>(radicalData)
            {
                Label = "Radicals",
                BackgroundColor = "#EFB6C8",
                HoverBackgroundColor = "#EFB6C8",
                BorderWidth = 1
            });

            foreach (var deckName in User.Profile!.Decks)
            {
                var deck = DeckMethods.LoadDeck(User.Profile, deckName.Name);
                var deckData = new List<int>();

                foreach (var tier in new[] { MasteryTier.Novice, MasteryTier.Beginner, MasteryTier.Proficient, MasteryTier.Expert, MasteryTier.Mastered })
                {
                    deckData.Add(deck.Count(x => x.Unlocked && x.MasteryTier == tier));
                }

                JLPTBarConfig.Data.Datasets.Add(new BarDataset<int>(deckData)
                {
                    Label = deckName.Name,
                    BackgroundColor = deckName.GraphColor,
                    HoverBackgroundColor = deckName.GraphColor,
                    BorderWidth = 1
                });
            }

            var counter = 1;

            foreach (var level in new[] { "N5", "N4", "N3", "N2", "N1" })
            {
                var grammarData = new List<int>();

                var color = counter switch
                {
                    1 => "rgba(100, 149, 237, 1)",
                    2 => "rgba(72, 209, 204, 1)",
                    3 => "rgba(144, 238, 144, 1)",
                    4 => "#8174A0",
                    _ => "#EFB6C8"
                };

                foreach (var tier in new[] { MasteryTier.Novice, MasteryTier.Beginner, MasteryTier.Proficient, MasteryTier.Expert, MasteryTier.Mastered })
                {
                    grammarData.Add(Grammar.Count(x => x.JLPTLevel == level && x.MasteryTier == tier && x.Unlocked));
                }

                GrammarBarConfig.Data.Datasets.Add(new BarDataset<int>(grammarData)
                {
                    Label = level,
                    BackgroundColor = color,
                    HoverBackgroundColor = color,
                    BorderWidth = 1
                });

                counter++;
            }
        }
        #endregion

        #region Statistics
        private float GetUnlockedPercentage(List<StudyItem> words)
        {
            return float.Round(words.Count(x => x.Unlocked) / (float)words.Count * 100, 2);
        }

        private float GetLearnedPercentage(StudyType type)
        {
            if (type != StudyType.Vocab)
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
        private async Task GoToLearnKanji()
        {
            var kanjiToLearn = KanjiMethods.GetItemsToLearn(Kanji);

            if (kanjiToLearn.Count > 0)
            {
                User.TriggerLearnMode = true;
                User.SelectedKanjiGroup = kanjiToLearn;

                await AnimatePage(Motions.ZoomOut);
                Nav.NavigateTo("/kanjireview");
            }
        }

        private async void GoToReviewKanji(StudyType type)
        {
            if (type != StudyType.Vocab)
            {
                User.SelectedKanjiGroup = KanjiMethods.GetItemsToReview(Kanji);
            }
            else
            {
                User.SelectedKanjiGroup = KanjiMethods.GetItemsToReview(KanjiVocab);
            }

            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/studyvocab");
        }

        private async Task GoToLearnGrammar()
        {
            var grammarToLearn = GrammarMethods.GetItemsToLearn(Grammar);

            if (grammarToLearn.Count > 0)
            {
                User.TriggerLearnMode = true;
                User.SelectedGrammarGroup = grammarToLearn;

                await AnimatePage(Motions.ZoomOut);
                Nav.NavigateTo("/grammardetail");
            }
        }

        private async void GoToReviewGrammar()
        {
            User.SelectedGrammarGroup = GrammarMethods.GetItemsToReview(Grammar);

            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/studygrammar");
        }

        private async void ToStudy(Deck deck)
        {
            User.SelectedDeck = deck;

            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/studyvocab");
        }

        private async Task AnimatePage(Motions motion)
        {
            await Animate.Animate(motion);
        }

        public void Dispose()
        {
            Anim.OnAnimate -= AnimatePage;
        }
        #endregion

        #region Decks
        private async void ToViewDeck(Deck deck)
        {
            User.SelectedDeck = deck;

            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/viewdeck");
        }

        private void CreateDeck()
        {
            if (!User.Profile!.Decks.Select(x => x.Name).Contains(DeckName))
            {
                DeckMethods.CreateDeck(DeckName, DeckType, DeckColor, User.Profile!);
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

                DeckMethods.SaveDeck(deck, User.Profile!.Name, DeckToDelete.Name);

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
