using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class UnlockedWords
    {
        public List<StudyWord> AllWords = new();
        public List<StudyWord> TodaysWords = new();

        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            AllWords = HelperMethods.LoadCoreWords();
            LoadUnlockedWords();
        }

        private void LoadUnlockedWords()
        {
            TodaysWords = HelperMethods.LoadUnlockedWords(User.Profile!);
        }

        private void ResetStreak(StudyWord word)
        {
            var allWords = HelperMethods.LoadCoreWords();
            var studyWord = allWords.First(x => x.Id == word.Id);

            studyWord.CorrectStreak = 0;
            studyWord.LastStudied = DateTime.MinValue;

            HelperMethods.UpdateWords(allWords);
            LoadUnlockedWords();
        }
    }
}
