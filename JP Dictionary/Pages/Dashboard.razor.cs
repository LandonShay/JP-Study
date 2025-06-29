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

            SaveProfile(User.Profile);
            LoadDashboard();
        }

        private void SaveProfile(Profile profile)
        {
            List<Profile> profiles;
            var filePath = "Data/Profiles.txt";

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                var content = File.ReadAllText(filePath);
                profiles = JsonSerializer.Deserialize<List<Profile>>(content) ?? new List<Profile>();
            }
            else
            {
                profiles = new List<Profile>();
            }

            profiles.RemoveAll(x => x.Name == profile.Name);
            profiles.Add(profile);

            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
