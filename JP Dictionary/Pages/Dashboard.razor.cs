using System.Text.Json;
using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class Dashboard
    {
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        private int WordsToStudy { get; set; }
        private int TotalWords { get; set; }

        protected override void OnInitialized()
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            WordsToStudy = HelperMethods.LoadWordsToStudy(User.Profile).Count;
            TotalWords = HelperMethods.LoadUnlockedWords(User.Profile).Count;
        }

        private void ChangePage(string route)
        {
            Nav.NavigateTo(route);
        }

        private void UnlockNextTier()
        {
            User.Profile.CurrentDay++;

            if (User.Profile.CurrentDay > 7)
            {
                User.Profile.CurrentDay = 1;
                User.Profile.CurrentWeek++;
            }

            HelperMethods.SaveProfile(User.Profile);
            LoadDashboard();
        }
    }
}
