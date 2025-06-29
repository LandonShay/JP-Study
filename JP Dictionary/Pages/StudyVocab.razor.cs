using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;
using MoreLinq;

namespace JP_Dictionary.Pages
{
    public partial class StudyVocab
    {
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        public Queue<StudyCard> StudyCards = new();
        public List<StudyWord> StudyWords = new(); // store all words for easy updating
        public StudyCard? CurrentCard;

        public string Answer = string.Empty;
        public byte AttemptsRemaining = 3;
        public bool ShowResults;
        public bool Finished;

        protected override void OnInitialized()
        {
            var studyCards = new List<StudyCard>();

            var availableWords = HelperMethods.LoadWordsToStudy(User.Profile);
            StudyWords = HelperMethods.LoadCoreWords();

            foreach (var word in availableWords)
            {
                var studyCard = new StudyCard
                {
                    Question = "What does this word mean?",
                    CardType = CardType.Definition,
                    StudyWord = word,
                    Word = word.Japanese,
                    OriginalFormatAnswer = word.Definitions,
                    Answers = word.Definitions.Split(',').Select(str => str.Trim().ToLower()).ToList()
                };

                var studyCard2 = new StudyCard
                {
                    Question = "How is this word pronounced?",
                    CardType = CardType.Pronounciation,
                    StudyWord = word,
                    Word = word.Japanese,
                    OriginalFormatAnswer = word.Pronounciation,
                    Answers = word.Pronounciation.Split(',').Select(str => str.Trim().ToLower()).ToList()
                };

                studyCard.Pair = studyCard2;
                studyCard2.Pair = studyCard;

                studyCards.Add(studyCard);
                studyCards.Add(studyCard2);
            }

            foreach (var studyCard in studyCards.Shuffle())
            {
                StudyCards.Enqueue(studyCard);
            }

            SetCurrentCard();
        }

        private void SubmitAnswer()
        {
            var cleanedAnwer = Answer.Trim().ToLower();

            if (CurrentCard.Answers.Contains(cleanedAnwer))
            {
                CurrentCard.Correct = true;
                ShowResults = true;

                UpdateWord(true);
            }
            else
            {
                AttemptsRemaining--;

                if (AttemptsRemaining == 0)
                {
                    ShowResults = true;

                    UpdateWord(false);
                    ReaddFailedCard();
                }
            }
        }

        private void GiveUp()
        {
            CurrentCard.Correct = false;
            ShowResults = true;

            UpdateWord(false);
            ReaddFailedCard();
        }

        private void ShowNextCard()
        {
            ShowResults = false;
            Answer = string.Empty;
            AttemptsRemaining = 3;

            SetCurrentCard();
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
            }
        }

        private void UpdateWord(bool increase)
        {
            var word = StudyWords.First(x => x.Id == CurrentCard.StudyWord.Id);

            if (CurrentCard != null && CurrentCard.Correct && (CurrentCard.Pair == null || CurrentCard.Pair.Correct))
            {
                if (increase)
                {
                    word.CorrectStreak++;
                }
                else
                {
                    word.CorrectStreak = 0;
                }

                word.LastStudied = DateTime.Today.Date;
                HelperMethods.UpdateWords(StudyWords);
            }
            else if (CurrentCard != null && !increase)
            {
                word.CorrectStreak = 0;
                word.LastStudied = DateTime.MinValue;
                HelperMethods.UpdateWords(StudyWords);
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

        private void ChangePage(string route)
        {
            Nav.NavigateTo(route);
        }
    }
}
