using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using MyNihongo.KanaConverter;
using Microsoft.JSInterop;
using WanaKanaSharp;
using FuzzySharp;
using MoreLinq;

namespace JP_Dictionary.Pages
{
    public partial class StudyVocab
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
        private Motion ModalAnimate { get; set; } = default!;

        private Queue<VocabCard> StudyCards { get; set; } = new();
        private List<StudyItem> StudyItems { get; set; } = new(); // store all words for easy updating
        private VocabCard CurrentCard { get; set; } = new();

        // user's answers
        private string DefinitionAnswer { get; set; } = string.Empty;
        private string ReadingAnswer { get; set; } = string.Empty;

        // css classes
        private string DefinitionStatus { get; set; } = string.Empty;
        private string ReadingStatus { get; set; } = string.Empty;
        private string ItemTypeCss { get; set; } = string.Empty;

        private Sentence? ExampleSentence { get; set; }
        private bool ShowExampleSentenceTranslation { get; set; }

        private string ElementToFocus { get; set; } = string.Empty; // controlled element focus
        public byte AttemptsRemaining { get; set; } = 3;

        // study options
        private string DeckName { get; set; } = string.Empty; // using this to prevent null error when studying kanji
        private bool ShowTestOptionModal { get; set; } = true;
        private bool TestReading { get; set; } // whether or not you are tested on the reading of the word

        // inline editing
        private bool EditingDefinition { get; set; }
        private string NewDefinition { get; set; } = string.Empty;

        private bool ShowResults { get; set; }
        private bool FirstResults { get; set; } = true; // tracking if it's your first time being shown the results per word (to prevent unintentional auto-speak)
        private bool FinishedStudying { get; set; }
        public static bool Talking { get; set; }

        #region Init + Rendering
        protected override void OnInitialized()
        {
            try
            {
                if (User.SelectedDeck != null)
                {
                    CreateDeckCards();
                }
                else if (User.SelectedKanjiGroup.Count > 0)
                {
                    if (User.SelectedKanjiGroup.Count == 0 && User.PreviousKanjiGroup.Count > 0)
                    {
                        User.SelectedKanjiGroup = User.PreviousKanjiGroup.ToList();
                        User.ResetPreviousKanjiGroup();
                    }

                    if (User.SelectedKanjiGroup.All(x => x.Type != StudyType.Vocab))
                    {
                        CreateKanjiAndRadicalCards();
                    }
                    else
                    {
                        StudyItems = KanjiMethods.LoadUserKanjiVocab(User.Profile!);
                        CreateDefaultCards(User.SelectedKanjiGroup, StudyCardType.Vocab);
                    }

                    TestReading = true;
                    ShowTestOptionModal = false;

                    ShowNextCard();
                }

                if (StudyCards.Count == 0)
                {
                    ShowTestOptionModal = false;
                    FinishedStudying = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occurred during initialization, see console for details");
            }
        }

        private void CreateDeckCards()
        {
            DeckName = User.SelectedDeck!.Name;
            StudyItems = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);

            CreateDefaultCards(DeckMethods.LoadWordsToStudy(StudyItems).Shuffle().ToList(), StudyCardType.Deck);
            TestReading = User.SelectedDeck.Type != DeckType.Grammar;
        }

        private void CreateKanjiAndRadicalCards()
        {
            StudyItems = KanjiMethods.LoadUserKanji(User.Profile!);

            foreach (var item in User.SelectedKanjiGroup)
            {
                var studyCard = new VocabCard
                {
                    StudyItem = item,
                    CurrentCorrectStreak = item.CorrectStreak,
                    Type = item.Type == StudyType.Radical ? StudyCardType.Radical : StudyCardType.Kanji
                };

                if (item.Type == StudyType.Radical)
                {
                    studyCard.OriginalFormatReading = "None";
                    studyCard.OriginalFormatDefinition = item.Name;
                    studyCard.DefinitionAnswers.Add(item.Name.ToLower());
                }
                else
                {
                    studyCard.DefinitionAnswers = item.Meaning;
                    studyCard.OriginalFormatDefinition = string.Join(", ", item.Meaning);

                    var romajiReadings = item.Onyomi.Concat(item.Kunyomi).Where(WanaKana.IsKana).Select(r => r.Trim().ToRomaji());

                    foreach (var r in romajiReadings)
                    {
                        studyCard.ReadingAnswers.Add(r);
                    }

                    studyCard.OriginalFormatReading = string.Join(", ", item.Onyomi.Concat(item.Kunyomi).Where(r => !string.IsNullOrWhiteSpace(r))
                                                                                                        .Select(r => r.Trim()));
                }

                StudyCards.Enqueue(studyCard);
            }
        }

        private void CreateDefaultCards(List<StudyItem> items, StudyCardType type)
        {
            foreach (var item in items)
            {
                StudyCards.Enqueue(new VocabCard
                {
                    Type = type,
                    StudyItem = item,
                    CurrentCorrectStreak = item.CorrectStreak,
                    OriginalFormatDefinition = string.Join(", ", item.Meaning),
                    OriginalFormatReading = item.Reading,
                    DefinitionAnswers = item.Meaning,
                    ReadingAnswers = WanaKana.IsJapanese(item.Reading) ? [item.Reading.ToRomaji()] : [item.Reading]
                });
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender)
                {
                    Anim.OnAnimate += AnimatePage;

                    if (ShowTestOptionModal)
                    {
                        _ = AnimatePage(Motions.ZoomIn);
                        await AnimateElement(ModalAnimate, Motions.ZoomIn);
                    }
                    else
                    {
                        CardAnimate?.ToggleVisibility(true);
                        await AnimatePage(Motions.ZoomIn);
                    }
                }

                if (ElementToFocus != string.Empty)
                {
                    await FocusElement(ElementToFocus);
                    ElementToFocus = string.Empty;
                }

                if (!firstRender &&
                   ((ShowResults && User.Profile!.AutoSpeak && FirstResults) || 
                   (!ShowResults && !TestReading && AttemptsRemaining == 3 && !FinishedStudying)))
                {
                    FirstResults = false;
                    await TextToSpeech();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured during rendering, see console for details");
            }
        }
        #endregion

        #region Study Options
        private async Task ConfirmTestOptions()
        {
            await AnimateElement(ModalAnimate, Motions.FadeOut);
            await Task.Delay(100);

            ShowTestOptionModal = false;

            StateHasChanged();
            await SetCurrentCard(true);

            ElementToFocus = TestReading ? "reading" : "definition";
            await FocusElement(ElementToFocus);
        }
        #endregion

        #region Submit
        private async Task SubmitAnswer()
        {
            ReadingStatus = string.Empty;
            DefinitionStatus = string.Empty;

            var readingCorrect = !TestReading || AnswerIsAccurate(ReadingAnswer, CurrentCard.ReadingAnswers, 100);
            var definitionCorrect = AnswerIsAccurate(DefinitionAnswer, CurrentCard.DefinitionAnswers, 75);

            if (readingCorrect && definitionCorrect)
            {
                CurrentCard.Correct = true;

                await AnimateElement(CardAnimate, Motions.FlipLeftOut);

                ShowResults = true;

                UpdateWord(1);
                FetchExampleSentence();

                await AnimateElement(CardAnimate, Motions.FlipLeftIn);

                ElementToFocus = "correct-next";
                return;
            }

            if (readingCorrect)
            {
                ReadingStatus = "input-correct";
                DefinitionStatus = "input-incorrect";
            }
            else if (definitionCorrect)
            {
                ReadingStatus = "input-incorrect";
                DefinitionStatus = "input-correct";
            }
            else
            {
                ReadingStatus = "input-incorrect";
                DefinitionStatus = "input-incorrect";
            }

            AttemptsRemaining--;

            if (AttemptsRemaining == 0)
            {
                await AnimateElement(CardAnimate, Motions.FlipLeftOut);

                ShowResults = true;
                FetchExampleSentence();

                await AnimateElement(CardAnimate, Motions.FlipLeftIn);

                ElementToFocus = "incorrect-next";
                StateHasChanged();

                UpdateWord(-1);
            }
        }

        private bool AnswerIsAccurate(string? userAnswer, IEnumerable<string> validAnswers, int threshold)
        {
            if (string.IsNullOrWhiteSpace(userAnswer))
            {
                return false;
            }

            return validAnswers.Any(valid => Fuzz.Ratio(valid.Trim().ToLowerInvariant(), userAnswer.Trim().ToLowerInvariant()) >= threshold);
        }

        private async Task GiveUp()
        {
            await AnimateElement(CardAnimate, Motions.FlipLeftOut);
            ShowResults = true;
            await AnimateElement(CardAnimate, Motions.FlipLeftIn);

            ElementToFocus = "incorrect-next";
            StateHasChanged();
        }

        private async void GoToDashboard()
        {
            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/dashboard");
        }
        #endregion

        #region Cards
        private async void ShowNextCard()
        {
            if (!CurrentCard.Correct)
            {
                ReaddFailedCard();
            }
            else
            {
                User.SelectedKanjiGroup?.RemoveAll(x => x.Item == CurrentCard.StudyItem.Item && x.Type == CurrentCard.StudyItem.Type);
            }

            await AnimateElement(CardAnimate, Motions.FlipLeftOut);
            await SetCurrentCard(false);

            ReadingAnswer = string.Empty;
            DefinitionAnswer = string.Empty;

            ReadingStatus = string.Empty;
            DefinitionStatus = string.Empty;

            ShowResults = false;
            FirstResults = true;

            ShowExampleSentenceTranslation = false;

            AttemptsRemaining = 3;
            ExampleSentence = null;

            if (User.SelectedDeck == null)
            {
                if (CurrentCard.Type == StudyCardType.Radical)
                {
                    TestReading = false;
                    ItemTypeCss = "radical";
                }
                else if (CurrentCard.Type == StudyCardType.Kanji)
                {
                    TestReading = true;
                    ItemTypeCss = "kanji";
                }
                else
                {
                    TestReading = true;
                    ItemTypeCss = string.Empty;
                }
            }
            else
            {
                ItemTypeCss = string.Empty;
            }

            if (!FinishedStudying)
            {
                await AnimateElement(CardAnimate, Motions.FlipLeftIn);
            }

            if (TestReading)
            {
                ElementToFocus = "reading";
            }
            else
            {
                ElementToFocus = "definition";
            }

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
                User.ResetPreviousKanjiGroup();
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
            return CurrentCard.StudyItem.LastStudied == DateTime.MinValue;
        }
        #endregion

        #region Editing
        private void StartEditing()
        {
            EditingDefinition = true;
            NewDefinition = CurrentCard.OriginalFormatDefinition;
        }

        private void FinishEditing()
        {
            var word = StudyItems.First(x => x.Item == CurrentCard.StudyItem.Item && x.Type == CurrentCard.StudyItem.Type);
            word.Meaning = NewDefinition.Split(',').Select(str => str.Trim().ToLower()).ToList(); ;

            CurrentCard.StudyItem.Meaning = word.Meaning;
            CurrentCard.DefinitionAnswers = word.Meaning;
            CurrentCard.OriginalFormatDefinition = NewDefinition;

            UpdateByType(word);
            EditingDefinition = false;
        }
        #endregion

        #region Result Actions
        private void UpdateWord(int change)
        {
            var item = StudyItems.First(x => x.Item == CurrentCard.StudyItem.Item && x.Type == CurrentCard.StudyItem.Type);
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

            UpdateByType(item);
        }

        private void UpdateByType(StudyItem item)
        {
            if (item.Type == StudyType.Deck)
            {
                DeckMethods.UpdateDeck(StudyItems, User.Profile!, User.SelectedDeck!.Name);
            }
            else if (item.Type == StudyType.Vocab)
            {
                KanjiMethods.SaveUserKanjiVocab(User.Profile!, StudyItems);
            }
            else if (item.Type == StudyType.Kanji || item.Type == StudyType.Radical)
            {
                KanjiMethods.SaveUserKanji(User.Profile!, StudyItems);
            }
        }

        private void MarkAsCorrect()
        {
            CurrentCard.Correct = true;
            CurrentCard.StudyItem.CorrectStreak = CurrentCard.CurrentCorrectStreak;

            UpdateWord(1);
        }

        private void FetchExampleSentence()
        {
            if (CurrentCard.Type == StudyCardType.Radical)
            {
                return;
            }

            var word = CurrentCard.StudyItem.Item;

            if (word.StartsWith('~') || word.StartsWith('～'))
            {
                word = word.Remove(0, 1);
            }
            
            var sentencesWithWord = User.Sentences.FindAll(x => x.Keywords.Contains(word));

            if (sentencesWithWord.Count > 0)
            {
                ExampleSentence = sentencesWithWord.Shuffle().First();
                return;
            }

            ExampleSentence = null;
        }

        private void ViewItemDetails()
        {
            User.UpdatePreviousKanjiGroup();

            User.WipeSelectedKanjiGroup = true;
            User.SelectedKanji = CurrentCard.StudyItem;
            User.SelectedKanjiGroup = new() { CurrentCard.StudyItem };

            Nav.NavigateTo("/kanjireview");
        }

        private void CheckLevelUp()
        {
            if (User.SelectedDeck == null)
            {
                var currentLevelKanji = StudyItems.Where(x => x.Level == User.Profile!.KanjiLevel && x.Type == StudyType.Kanji);
                var percentAboveNovice = currentLevelKanji.Count(x => x.MasteryTier != MasteryTier.Novice) / (float)currentLevelKanji.Count() * 100;

                if (percentAboveNovice > 90)
                {
                    User.Profile!.KanjiLevel++;

                    HelperMethods.SaveProfile(User.Profile!);
                    KanjiMethods.UnlockNextSet(User.Profile!);
                }
            }
        }
        #endregion

        #region JS
        private async Task SearchJisho()
        {
            var link = Path.Combine("https://jisho.org/search/", CurrentCard.StudyItem.Item);
            await JS.InvokeVoidAsync("openInNewTab", link);
        }

        private async Task FocusElement(string elementId)
        {
            await JS.InvokeVoidAsync("focusElementById", elementId);
        }

        private async Task TextToSpeech()
        {
            try
            {
                if (!Talking && (CurrentCard.Type == StudyCardType.Deck || CurrentCard.Type == StudyCardType.Vocab))
                {
                    var word = CurrentCard.ReadingAnswers.FirstOrDefault();

                    if (word != null)
                    {
                        Talking = true;
                        await JS.InvokeVoidAsync("speakTextBasic", word.ToHiragana(), 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured during TTS, see console for details");
            }
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
