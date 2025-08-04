using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using MyNihongo.KanaConverter;
using Microsoft.JSInterop;
using WanaKanaSharp;
using MoreLinq;

namespace JP_Dictionary.Pages
{
    public partial class StudyVocab
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        private Queue<VocabCard> StudyCards { get; set; } = new();
        private List<StudyWord> StudyWords { get; set; } = new(); // store all words for easy updating
        private List<StudyKanji> StudyKanji { get; set; } = new(); // store all kanji for easy updating
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
        private bool AutoSpeak
        {
            get => User.Profile!.AutoSpeak;
            set
            {
                User.Profile!.AutoSpeak = value;
                HelperMethods.SaveProfile(User.Profile);
            }
        }

        #region Init + Rendering
        protected override void OnInitialized()
        {
            try
            {
                if (User.SelectedDeck != null)
                { // Vocab / Grammar
                    DeckName = User.SelectedDeck!.Name;
                    StudyWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);

                    var availableWords = DeckMethods.LoadWordsToStudy(StudyWords);
                    var type = User.SelectedDeck.Type switch
                    {
                        DeckType.Vocab => StudyCardType.Vocab,
                        DeckType.Grammar => StudyCardType.Grammar,
                        _ => StudyCardType.Kanji
                    };

                    foreach (var word in availableWords.Shuffle())
                    {
                        var studyCard = new VocabCard
                        {
                            StudyWord = word,
                            Type = type,
                            Word = word.Word,
                            OriginalFormatDefinition = word.Definitions,
                            OriginalFormatReading = word.Romaji,
                            DefinitionAnswers = word.Definitions.Split(',').Select(str => str.Trim().ToLower()).ToList(),
                            ReadingAnswers = word.Romaji.Split(',').Select(str => str.Trim().ToLower()).ToList()
                        };

                        StudyCards.Enqueue(studyCard);
                    }

                    TestReading = User.SelectedDeck.Type != DeckType.Grammar;
                }
                else
                {
                    if (User.SelectedKanjiGroup.All(x => x.Type != KanjiType.Vocab))
                    { // Kanji / Radicals
                        StudyKanji = KanjiMethods.LoadUserKanji(User.Profile!);

                        foreach (var item in User.SelectedKanjiGroup)
                        {
                            var studyCard = new VocabCard
                            {
                                Word = item.Item,
                                Type = StudyCardType.Kanji,
                                OriginalFormatDefinition = string.Join(", ", item.Meaning),
                                StudyKanji = item
                            };

                            if (item.Type == KanjiType.Radical)
                            {
                                studyCard.Type = StudyCardType.Radical;
                                studyCard.OriginalFormatDefinition = item.Name;
                                studyCard.OriginalFormatReading = "None";
                                studyCard.DefinitionAnswers.Add(item.Name.ToLower());
                            }
                            else
                            {
                                foreach (var reading in item.Onyomi.Where(WanaKana.IsKana))
                                {
                                    var romaji = reading.Trim().ToRomaji();
                                    studyCard.ReadingAnswers.Add(romaji);
                                }

                                foreach (var reading in item.Kunyomi.Where(WanaKana.IsKana))
                                {
                                    var romaji = reading.Trim().ToRomaji();
                                    studyCard.ReadingAnswers.Add(romaji);
                                }

                                studyCard.DefinitionAnswers = item.Meaning;
                            }

                            if (item.Onyomi.Count > 0)
                            {
                                studyCard.OriginalFormatReading = string.Join(", ", item.Onyomi);
                            }

                            if (item.Kunyomi.Count > 0)
                            {
                                var readings = string.Join(", ", item.Kunyomi);

                                if (studyCard.OriginalFormatReading.Length > 0)
                                {
                                    studyCard.OriginalFormatReading += ", " + string.Join(", ", item.Kunyomi);
                                }
                                else
                                {
                                    studyCard.OriginalFormatReading = string.Join(", ", item.Kunyomi);
                                }
                            }

                            StudyCards.Enqueue(studyCard);
                        }

                        TestReading = true;
                        ShowTestOptionModal = false;

                        ShowNextCard();
                    }
                    else
                    { // Kanji Vocab
                        StudyKanji = KanjiMethods.LoadUserKanjiVocab(User.Profile!);

                        foreach (var item in User.SelectedKanjiGroup)
                        {
                            var studyCard = new VocabCard
                            {
                                Word = item.Item,
                                Type = StudyCardType.Vocab,
                                OriginalFormatDefinition = string.Join(", ", item.Meaning),
                                OriginalFormatReading = item.Reading.ToRomaji(),
                                DefinitionAnswers = item.Meaning,
                                ReadingAnswers = [item.Reading.ToRomaji()],
                                StudyKanji = item
                            };

                            StudyCards.Enqueue(studyCard);
                        }

                        TestReading = true;
                        ShowTestOptionModal = false;

                        ShowNextCard();
                    }
                }
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
                    await TextToSpeech(CurrentCard.StudyWord.Audio);
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
            ShowTestOptionModal = false;

            SetCurrentCard();

            ElementToFocus = TestReading ? "reading" : "definition";
            await FocusElement(ElementToFocus);
        }
        #endregion

        #region Submit
        private void SubmitAnswer()
        {
            var readingCorrect = false;
            var definitionCorrect = false;

            ReadingStatus = string.Empty;
            DefinitionStatus = string.Empty;

            if (!string.IsNullOrWhiteSpace(ReadingAnswer))
            {
                var cleanedAnswer = ReadingAnswer.Trim().ToLower();
                readingCorrect = CurrentCard.ReadingAnswers.Contains(cleanedAnswer);
            }
            else if (!TestReading)
            {
                readingCorrect = true;
            }

            if (!string.IsNullOrWhiteSpace(DefinitionAnswer))
            {
                var cleanedAnswer = DefinitionAnswer.Trim().ToLower();

                if (CurrentCard.DefinitionAnswers.Any(x => x.ToLower() == cleanedAnswer))
                {
                    definitionCorrect = true;
                }
            }

            if (readingCorrect && definitionCorrect)
            {
                CurrentCard.Correct = true;
                ShowResults = true;

                UpdateWord(1);
                FetchExampleSentence();

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
                ShowResults = true;
                ElementToFocus = "incorrect-next";
                UpdateWord(-1);
            }

            if (ShowResults)
            {
                FetchExampleSentence();
            }
        }

        private void GiveUp()
        {
            ShowResults = true;
            ElementToFocus = "incorrect-next";
        }
        #endregion

        #region Cards
        private void ShowNextCard()
        {
            if (!CurrentCard.Correct)
            {
                ReaddFailedCard();
            }

            SetCurrentCard();

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

            if (TestReading)
            {
                ElementToFocus = "reading";
            }
            else
            {
                ElementToFocus = "definition";
            }
        }

        private void SetCurrentCard()
        {
            if (StudyCards.Count > 0)
            {
                CurrentCard = StudyCards.Dequeue();
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
                if (card.Word != string.Empty)
                {
                    StudyCards.Enqueue(card);
                }
            }
        }

        private bool CardIsNew()
        {
            return (CurrentCard.StudyWord.Id != string.Empty && CurrentCard.StudyWord?.LastStudied == DateTime.MinValue) ||
                   (CurrentCard.StudyKanji.Item != string.Empty && CurrentCard.StudyKanji?.LastStudied == DateTime.MinValue);
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
            EditingDefinition = false;
            CurrentCard.StudyWord.Definitions = NewDefinition;
            CurrentCard.OriginalFormatDefinition = NewDefinition;
            CurrentCard.DefinitionAnswers = NewDefinition.Split(',').Select(str => str.Trim().ToLower()).ToList();

            if (User.SelectedDeck != null)
            {
                var word = StudyWords.First(x => x.Id == CurrentCard.StudyWord.Id);
                word.Definitions = NewDefinition;

                DeckMethods.UpdateDeck(StudyWords, User.Profile!.Name, User.SelectedDeck!.Name);
            }
            else
            {
                var word = StudyKanji.First(x => x.Item == CurrentCard.StudyKanji.Item && x.Type == CurrentCard.StudyKanji.Type);
                word.Meaning = NewDefinition.Split(',').ToList();

                if (CurrentCard.Type == StudyCardType.Vocab)
                {
                    KanjiMethods.SaveUserKanjiVocab(User.Profile!, StudyKanji);
                }
                else
                {
                    KanjiMethods.SaveUserKanji(User.Profile!, StudyKanji);
                }
            }
        }
        #endregion

        #region Result Actions
        private void UpdateWord(int change)
        {
            if (User.SelectedDeck != null)
            {
                var word = StudyWords.First(x => x.Id == CurrentCard.StudyWord.Id);

                if (change > 0)
                {
                    word.CorrectStreak += change;
                    word.LastStudied = DateTime.Today.Date;

                    if (word.CorrectStreak > 11)
                    {
                        word.CorrectStreak = 11;
                    }
                }
                else
                { // if you get a word wrong in beginner or below, start from 0. get it wrong in proficient or above, start from beginner
                    if (word.MasteryTier == MasteryTier.Novice || word.MasteryTier == MasteryTier.Beginner)
                    {
                        word.CorrectStreak = 0;
                        word.LastStudied = DateTime.MinValue;
                    }
                    else
                    {
                        word.CorrectStreak = HelperMethods.GetTierFloor(MasteryTier.Beginner);
                        word.LastStudied = DateTime.Today.Date;
                    }
                }

                DeckMethods.UpdateDeck(StudyWords, User.Profile!.Name, User.SelectedDeck!.Name);
            }
            else
            {
                var kanji = StudyKanji.First(x => x.Item == CurrentCard.StudyKanji.Item && x.Type == CurrentCard.StudyKanji.Type);

                if (change > 0)
                {
                    kanji.CorrectStreak += change;
                    kanji.LastStudied = DateTime.Today.Date;

                    if (kanji.CorrectStreak > 11)
                    {
                        kanji.CorrectStreak = 11;
                    }
                }
                else
                {
                    if (kanji.MasteryTier == MasteryTier.Novice || kanji.MasteryTier == MasteryTier.Beginner)
                    {
                        kanji.CorrectStreak = 0;
                        kanji.LastStudied = DateTime.MinValue;
                    }
                    else
                    {
                        kanji.CorrectStreak = HelperMethods.GetTierFloor(MasteryTier.Beginner);
                        kanji.LastStudied = DateTime.Today.Date;
                    }
                }

                if (kanji.Type != KanjiType.Vocab)
                {
                    KanjiMethods.SaveUserKanji(User.Profile!, StudyKanji);
                }
                else
                {
                    KanjiMethods.SaveUserKanjiVocab(User.Profile!, StudyKanji);
                }
            }
        }

        private void MarkAsCorrect()
        {
            CurrentCard.Correct = true;
            UpdateWord(1);
        }

        private void FetchExampleSentence()
        {
            if (CurrentCard.Type == StudyCardType.Radical)
            {
                return;
            }

            var word = CurrentCard.Word;

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

        private void CheckLevelUp()
        {
            if (User.SelectedDeck == null)
            {
                var currentLevelKanji = StudyKanji.Where(x => x.Level == User.Profile!.KanjiLevel && x.Type == KanjiType.Kanji);
                var percentAtBeginner = currentLevelKanji.Count(x => x.MasteryTier == MasteryTier.Beginner) / (float)currentLevelKanji.Count() * 100;

                if (percentAtBeginner > 90)
                {
                    User.Profile!.KanjiLevel++;
                    HelperMethods.SaveProfile(User.Profile!);
                }
            }
        }
        #endregion

        #region JS
        private async Task SearchJisho()
        {
            var link = Path.Combine("https://jisho.org/search/", CurrentCard.Word);
            await JS.InvokeVoidAsync("openInNewTab", link);
        }

        private async Task FocusElement(string elementId)
        {
            await JS.InvokeVoidAsync("focusElementById", elementId);
        }

        private async Task TextToSpeech(string audioPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(audioPath))
                {
                    if (!Talking)
                    {
                        Talking = true;

                        var filePath = HelperMethods.GetFilePath(audioPath);
                        var bytes = await File.ReadAllBytesAsync(filePath);
                        var base64 = Convert.ToBase64String(bytes);

                        await JS.InvokeVoidAsync("speakText", base64);
                    }
                }
                else
                {
                    if (CurrentCard.Word != string.Empty && CurrentCard.Type != StudyCardType.Radical && User.SelectedDeck != null)
                    {
                        Toast.ShowWarning($"{CurrentCard.Word} does not have audio");
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
    }
}
