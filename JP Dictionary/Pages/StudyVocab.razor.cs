using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
        private VocabCard CurrentCard { get; set; } = new();

        // user's answers
        private string DefinitionAnswer { get; set; } = string.Empty;
        private string ReadingAnswer { get; set; } = string.Empty;

        // css class for indicating correct or incorrect
        private string DefinitionStatus { get; set; } = string.Empty;
        private string ReadingStatus { get; set; } = string.Empty;

        private string ElementToFocus { get; set; } = string.Empty; // controlled element focus
        public byte AttemptsRemaining { get; set; } = 3;

        // study options
        private bool ShowTestOptionModal { get; set; } = true;
        private bool TestReading { get; set; }

        // inline editing
        private bool EditingDefinition { get; set; }
        private string NewDefinition { get; set; } = string.Empty;

        private bool ShowResults { get; set; }
        private bool Finished { get; set; }
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

        protected override void OnInitialized()
        {
            try
            {
                Console.WriteLine(typeof(Program).Assembly.GetName().Name);
                var studyCards = new List<VocabCard>();

                StudyWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
                var availableWords = DeckMethods.LoadWordsToStudy(User.Profile!, StudyWords);

                foreach (var word in availableWords)
                {
                    var studyCard = new VocabCard
                    {
                        StudyWord = word,
                        Word = word.Japanese,
                        OriginalFormatDefinition = word.Definitions,
                        OriginalFormatReading = word.Pronounciation,
                        DefinitionAnswers = word.Definitions.Split(',').Select(str => str.Trim().ToLower()).ToList(),
                        ReadingAnswers = word.Pronounciation.Split(',').Select(str => str.Trim().ToLower()).ToList()
                    };

                    studyCards.Add(studyCard);
                }

                foreach (var studyCard in studyCards.Shuffle())
                {
                    StudyCards.Enqueue(studyCard);
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

                if ((ShowResults && User.Profile!.AutoSpeak) || 
                   (!ShowResults && !TestReading && AttemptsRemaining == 3 && !Finished))
                {
                    await TextToSpeech(CurrentCard.StudyWord.Audio);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured during rendering, see console for details");
            }
        }

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
                definitionCorrect = CurrentCard.DefinitionAnswers.Contains(cleanedAnswer);
            }

            if (readingCorrect && definitionCorrect)
            {
                CurrentCard.Correct = true;
                ShowResults = true;

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
            UpdateWord();

            if (!CurrentCard.Correct)
            {
                ReaddFailedCard();
            }

            SetCurrentCard();

            ReadingAnswer = string.Empty;
            DefinitionAnswer = string.Empty;

            ReadingStatus = string.Empty;
            DefinitionStatus = string.Empty;

            AttemptsRemaining = 3;

            ShowResults = false;

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
                Finished = true;
                ElementToFocus = "return";
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
            CurrentCard.OriginalFormatDefinition = NewDefinition;
            CurrentCard.StudyWord.Definitions = NewDefinition;

            var word = StudyWords.First(x => x.Id == CurrentCard.StudyWord.Id);
            word.Definitions = NewDefinition;

            DeckMethods.UpdateDeck(StudyWords, User.Profile!.Name, User.SelectedDeck!.Name);
        }
        #endregion

        private void UpdateWord()
        {
            var word = StudyWords.First(x => x.Id == CurrentCard.StudyWord.Id);

            if (CurrentCard.Correct)
            {
                word.CorrectStreak++;
                word.LastStudied = DateTime.Today.Date;
            }
            else
            {
                word.CorrectStreak = 0;
                word.LastStudied = DateTime.MinValue;
            }

            DeckMethods.UpdateDeck(StudyWords, User.Profile!.Name, User.SelectedDeck!.Name);
        }

        #region JS
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
                    if (CurrentCard.Word != string.Empty)
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
