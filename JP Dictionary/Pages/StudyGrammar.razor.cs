using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MyNihongo.KanaConverter;
using Microsoft.JSInterop;
using MoreLinq;

namespace JP_Dictionary.Pages
{
    public partial class StudyGrammar
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public ToastService Toast { get; set; }
        [Inject] public AnimationService Anim { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        private Motion PageAnimate { get; set; } = default!;
        private Motion CardAnimate { get; set; } = default!;

        private Queue<GrammarCard> StudyCards { get; set; } = new();
        private List<GrammarItem> StudyItems { get; set; } = new();
        private GrammarCard CurrentCard { get; set; } = new();

        private string Answer { get; set; } = string.Empty;
        private string AnswerStatus { get; set; } = string.Empty;

        private string ElementToFocus { get; set; } = string.Empty; // controlled element focus
        public byte AttemptsRemaining { get; set; } = 3;

        private bool ShowResults { get; set; }
        private bool FinishedStudying { get; set; }

        #region Init + Rendering
        protected override void OnInitialized()
        {
            try
            {
                CreateGrammarCards();
                ShowNextCard(true);

                FinishedStudying = StudyCards.Count == 0 && CurrentCard.GrammarItem.Name == string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occurred during initialization, see console for details");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    Anim.OnAnimate += AnimatePage;

                    CardAnimate?.ToggleVisibility(true);
                    await AnimatePage(Motions.ZoomIn);
                }

                if (ElementToFocus != string.Empty)
                {
                    await FocusElement(ElementToFocus);
                    ElementToFocus = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured during rendering, see console for details");
            }
        }
        #endregion

        #region Cards
        private void CreateGrammarCards()
        {
            StudyItems = GrammarMethods.LoadUserGrammar(User.Profile!);

            foreach (var item in User.SelectedGrammarGroup.Shuffle())
            {
                var rnd = new Random();
                var rndIndex = rnd.Next(0, item.Questions.Count);

                var studyCard = new GrammarCard
                {
                    GrammarItem = item,
                    CurrentCorrectStreak = item.CorrectStreak,
                    FullJP = item.Questions[rndIndex].JapaneseHTML
                };

                var matches = Regex.Matches(studyCard.FullJP, @"<strong>(.*?)</strong>");

                if (matches.Count > 0)
                {
                    studyCard.Answer = matches[0].Groups[1].Value;

                    studyCard.EN = item.Questions[rndIndex].EnglishHTML;
                    studyCard.JP = Regex.Replace(studyCard.FullJP, @"<strong>.*?</strong>", "___");

                    StudyCards.Enqueue(studyCard);
                }
                else
                {
                    Toast.ShowWarning($"Grammar {item.Name} could not identify an answer on sentence {studyCard.FullJP}.");
                    Console.WriteLine(studyCard.FullJP);
                }
            }
        }

        private async void ShowNextCard(bool isInitial)
        {
            if (!isInitial)
            {
                if (!CurrentCard.Correct)
                {
                    ReaddFailedCard();
                }
                else
                {
                    User.SelectedGrammarGroup?.RemoveAll(x => x.Name == CurrentCard.GrammarItem.Name);
                }
            }

            await AnimateElement(CardAnimate, Motions.FlipLeftOut);
            await SetCurrentCard(false);

            Answer = string.Empty;
            AnswerStatus = string.Empty;

            ShowResults = false;
            AttemptsRemaining = 3;

            if (!FinishedStudying)
            {
                await AnimateElement(CardAnimate, Motions.FlipLeftIn);
            }

            ElementToFocus = "definition";
            StateHasChanged();
        }

        private async Task SetCurrentCard(bool animateCard)
        {
            if (StudyCards.Count > 0)
            {
                CurrentCard = StudyCards.Dequeue();

                if (CardAnimate != null && animateCard)
                {
                    await AnimateElement(CardAnimate, Motions.SlideRightIn);
                }
            }
            else
            {
                FinishedStudying = true;
                ElementToFocus = "return";

                CheckLevelUp();
            }
        }

        private void ReaddFailedCard()
        {
            var remainingCards = StudyCards.ToList();
            remainingCards.Add(CurrentCard);

            StudyCards.Clear();

            foreach (var card in remainingCards.Shuffle())
            {
                StudyCards.Enqueue(card);
            }
        }

        private bool CardIsNew()
        {
            return CurrentCard.GrammarItem.LastStudied == DateTime.MinValue;
        }
        #endregion

        #region Submit
        private async void SubmitAnswer()
        {
            AnswerStatus = string.Empty;

            var correct = CurrentCard.Answer.Trim().ToRomaji().Equals(Answer.Trim(), StringComparison.CurrentCultureIgnoreCase);

            if (correct)
            {
                CurrentCard.Correct = true;

                await AnimateElement(CardAnimate, Motions.FlipLeftOut);

                ShowResults = true;
                UpdateWord(1);

                await AnimateElement(CardAnimate, Motions.FlipLeftIn);

                ElementToFocus = "correct-next";
                StateHasChanged();
                return;
            }
            else
            {
                AnswerStatus = "input-incorrect";
            }

            AttemptsRemaining--;

            if (AttemptsRemaining == 0)
            {
                await AnimateElement(CardAnimate, Motions.FlipLeftOut);
                ShowResults = true;
                await AnimateElement(CardAnimate, Motions.FlipLeftIn);

                ElementToFocus = "incorrect-next";
                StateHasChanged();

                UpdateWord(-1);
            }
        }

        private void UpdateWord(int change)
        {
            var item = StudyItems.First(x => x.Name == CurrentCard.GrammarItem.Name);
            item.CorrectStreak = CurrentCard.CurrentCorrectStreak;

            if (change > 0)
            {
                item.CorrectStreak += change;
                item.LastStudied = DateTime.Today.Date;

                if (item.CorrectStreak > 11)
                {
                    item.CorrectStreak = 11;
                }
            }
            else
            {
                if (item.MasteryTier == MasteryTier.Novice || item.MasteryTier == MasteryTier.Beginner)
                {
                    item.CorrectStreak = 0;
                    item.LastStudied = DateTime.MinValue;
                }
                else
                {
                    item.CorrectStreak = HelperMethods.GetTierFloor(MasteryTier.Beginner);
                    item.LastStudied = DateTime.Today.Date;
                }
            }

            GrammarMethods.SaveUserGrammar(User.Profile!, StudyItems);
        }

        private void MarkAsCorrect()
        {
            CurrentCard.Correct = true;
            CurrentCard.GrammarItem.CorrectStreak = CurrentCard.CurrentCorrectStreak;

            UpdateWord(1);
        }

        private async Task GiveUp()
        {
            await AnimateElement(CardAnimate, Motions.FlipLeftOut);
            ShowResults = true;
            await AnimateElement(CardAnimate, Motions.FlipLeftIn);

            ElementToFocus = "incorrect-next";
            StateHasChanged();
        }

        private void CheckLevelUp()
        {
            var highestLesson = GrammarMethods.GetCurrentGrammarLesson(StudyItems);
            var highestGrammar = StudyItems.Where(x => x.JLPTLevel == User.Profile!.GrammarLevel && x.Lesson == highestLesson);

            if (highestGrammar.All(x => x.MasteryTier == MasteryTier.Beginner))
            {
                var nextLesson = GrammarMethods.ParseLessonNumber(highestLesson) + 1;
                var nextLessonGrammar = StudyItems.FindAll(x => x.JLPTLevel == User.Profile!.GrammarLevel && x.Lesson == $"Lesson {nextLesson}");

                if (nextLessonGrammar.Count > 0)
                {
                    GrammarMethods.UnlockNextSet(User.Profile!);
                }
                else
                {
                    var jlptLevel = int.TryParse(User.Profile!.GrammarLevel.Split('N').Last(), out var num);
                    num--;

                    if (num > 0)
                    {
                        User.Profile.GrammarLevel = $"N{num}";
                        HelperMethods.SaveProfile(User.Profile!);
                    }

                    GrammarMethods.UnlockNextSet(User.Profile!);
                }
            }
        }

        private async void GoToDashboard()
        {
            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/dashboard");
        }

        private void ViewItemDetails()
        {
            //User.UpdatePreviousKanjiGroup();

            //User.WipeSelectedKanjiGroup = true;
            //User.SelectedKanji = CurrentCard.StudyItem;
            //User.SelectedKanjiGroup = new() { CurrentCard.StudyItem };

            //Nav.NavigateTo("/kanjireview");
        }
        #endregion

        #region JS
        private async Task FocusElement(string elementId)
        {
            await JS.InvokeVoidAsync("focusElementById", elementId);
        }
        #endregion

        #region Animation
        private async Task AnimatePage(Motions motion)
        {
            await PageAnimate.Animate(motion);
        }

        private async Task AnimateElement(Motion motion, Motions action)
        {
            if (motion != null)
            {
                if (action == Motions.SlideLeftOut || action == Motions.SlideRightOut || action == Motions.SlideLeftIn || action == Motions.SlideRightIn)
                {
                    motion.ToggleVisibility(true);
                    await motion.AnimateSlide(action);
                }
                else if (action == Motions.FlipLeftIn || action == Motions.FlipLeftOut || action == Motions.FlipRightIn || action == Motions.FlipRightOut)
                {
                    await motion.AnimateFlip(action);
                }
                else
                {
                    await motion.Animate(action);
                }
            }
        }

        public void Dispose()
        {
            Anim.OnAnimate -= AnimatePage;
        }
        #endregion
    }
}
