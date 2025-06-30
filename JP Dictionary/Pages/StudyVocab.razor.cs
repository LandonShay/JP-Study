﻿using JP_Dictionary.Models;
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

        public Queue<VocabCard> StudyCards { get; set; } = new();
        public List<StudyWord> StudyWords { get; set; } = new(); // store all words for easy updating
        public VocabCard CurrentCard { get; set; } = new();

        // user's answers
        public string DefinitionAnswer { get; set; } = string.Empty;
        public string ReadingAnswer { get; set; } = string.Empty;

        // css class for indicating correct or incorrect
        private string DefinitionStatus { get; set; } = string.Empty;
        private string ReadingStatus { get; set; } = string.Empty;

        public string ElementToFocus { get; set; } = string.Empty; // controlled element focus
        public byte AttemptsRemaining { get; set; } = 3;
        public bool ShowResults { get; set; }
        public bool Finished { get; set; }
        public bool Talking { get; set; }
        private bool AutoSpeak
        {
            get => User.Profile!.AutoSpeak;
            set
            {
                User.Profile!.AutoSpeak = value;
                HelperMethods.SaveProfile(User.Profile);
            }
        }

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

            if (ShowResults && User.Profile!.AutoSpeak)
            {
                await TextToSpeech(CurrentCard.Word);
            }
        }

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

        private async Task TextToSpeech(string text)
        {
            if (!Talking)
            {
                Talking = true;
                await JS.InvokeVoidAsync("speakText", text);
                Talking = false;
            }
        }
    }
}
