using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class Dashboard
    {
        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        private int WordsToStudy { get; set; }
        private int WordsUnlocked { get; set; }
        private int RemainingWords { get; set; }

        private int StartingCount { get; set; }
        private int FamiliarCount { get; set; }
        private int GoodCount { get; set; }
        private int ExpertCount { get; set; }

        protected override void OnInitialized()
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            var unlockedWords = HelperMethods.LoadUnlockedWords(User.Profile!);

            WordsToStudy = HelperMethods.LoadWordsToStudy(User.Profile!).Count;
            WordsUnlocked = unlockedWords.Count;
            RemainingWords = HelperMethods.LoadDefaultCoreWords().Count - WordsUnlocked;

            StartingCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Starting);
            FamiliarCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Familiar);
            GoodCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Good);
            ExpertCount += unlockedWords.Count(x => x.MasteryTier == MasteryTier.Expert);
        }

        private void ChangePage(string route)
        {
            Nav.NavigateTo(route);
        }

        private void UnlockNextTier()
        {
            User.Profile!.CurrentDay++;

            if (User.Profile.CurrentDay > 7)
            {
                User.Profile.CurrentDay = 1;
                User.Profile.CurrentWeek++;

                if (User.Profile.CurrentWeek == byte.MaxValue - 1)
                {
                    User.Profile.CurrentWeek--;
                }
            }

            HelperMethods.SaveProfile(User.Profile);
            LoadDashboard();
        }
    }
}
