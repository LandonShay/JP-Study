using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class UnlockedWords
    {
        public List<StudyWord> AllWords = new();
        public List<StudyWord> TodaysWords = new();

        private StudyWord? EditingEntry;
        private string EditingValue = string.Empty;

        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            LoadPage();
        }

        private void LoadPage()
        {
            AllWords = HelperMethods.LoadPersonalCoreWords(User.Profile!);
            TodaysWords = HelperMethods.LoadUnlockedWords(User.Profile!);
        }

        private void ResetStreak(StudyWord word)
        {
            var allWords = HelperMethods.LoadPersonalCoreWords(User.Profile!);
            var studyWord = allWords.First(x => x.Id == word.Id);

            studyWord.CorrectStreak = 0;
            studyWord.LastStudied = DateTime.MinValue;

            HelperMethods.UpdateWords(allWords, User.Profile!.Name);
            LoadPage();
        }

        private void StartEditing(StudyWord entry)
        {
            EditingEntry = entry;
            EditingValue = entry.Definitions;
        }

        private void FinishEditing()
        {
            if (EditingEntry != null)
            {
                var word = AllWords.First(x => x.Id == EditingEntry.Id);
                word.Definitions = EditingValue;

                HelperMethods.UpdateWords(AllWords, User.Profile!.Name);

                EditingEntry = null;
                EditingValue = string.Empty;

                LoadPage();
            }
        }
    }
}
