using System.Text.Json;
using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class Dashboard
    {
        [Parameter] public Profile User { get; set; }
        [Parameter] public EventCallback<Pages> OnChangePage { get; set; }

        private int WordsToStudy { get; set; }
        private int TotalWords { get; set; }

        protected override void OnInitialized()
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            WordsToStudy = HelperMethods.LoadWordsToStudy(User).Count;
            TotalWords = HelperMethods.LoadUnlockedWords(User).Count;
        }

        private void ChangePage(Pages page)
        {
            OnChangePage.InvokeAsync(page);
        }

        private void UnlockNextTier()
        {
            User.CurrentDay++;

            if (User.CurrentDay > 7)
            {
                User.CurrentDay = 1;
                User.CurrentWeek++;
            }

            SaveProfile(User);
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
