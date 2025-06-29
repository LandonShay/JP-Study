using JP_Dictionary.Models;
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
#nullable enable
        #endregion

        public Queue<VocabCard> StudyCards = new();
        public List<StudyWord> StudyWords = new(); // store all words for easy updating
        public VocabCard CurrentCard = new();

        public string ElementToFocus = string.Empty;

        public string DefinitionAnswer = string.Empty;
        public string ReadingAnswer = string.Empty;

        public byte AttemptsRemaining = 3;
        public bool ShowResults;
        public bool Finished;

        protected override async void OnInitialized()
        {
            var studyCards = new List<VocabCard>();

            var availableWords = HelperMethods.LoadWordsToStudy(User.Profile!);
            StudyWords = HelperMethods.LoadPersonalCoreWords(User.Profile!);

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

            SetCurrentCard();

            ElementToFocus = "reading";
            await FocusElement(ElementToFocus);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (ElementToFocus != string.Empty)
            {
                await FocusElement(ElementToFocus);
                ElementToFocus = string.Empty;
            }
        }

        private void SubmitAnswer()
        {
            if (ReadingAnswer != string.Empty && DefinitionAnswer != string.Empty)
            {
                var cleanedReadingAnswer = ReadingAnswer.Trim().ToLower();
                var cleanedDefinitionAnswer = DefinitionAnswer.Trim().ToLower();

                if (CurrentCard!.ReadingAnswers.Contains(cleanedReadingAnswer) &&
                    CurrentCard.DefinitionAnswers.Contains(cleanedDefinitionAnswer))
                {
                    CurrentCard.Correct = true;
                    ShowResults = true;

                    ElementToFocus = "correct-next";
                }
                else
                {
                    AttemptsRemaining--;

                    if (AttemptsRemaining == 0)
                    {
                        ShowResults = true;
                        ElementToFocus = "incorrect-next";
                    }
                }
            }
        }

        private void GiveUp()
        {
            ShowResults = true;
            ElementToFocus = "incorrect-next";
        }

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
            AttemptsRemaining = 3;

            ShowResults = false;
            ElementToFocus = "reading";
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

            HelperMethods.UpdateWords(StudyWords, User.Profile!.Name);
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

        private void ChangePage(string route)
        {
            Nav.NavigateTo(route);
        }

        private async Task FocusElement(string elementId)
        {
            await JS.InvokeVoidAsync("focusElementById", elementId);
        }
    }
}
