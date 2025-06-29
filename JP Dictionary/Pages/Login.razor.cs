using System.Text.Json;
using Microsoft.AspNetCore.Components;
using JP_Dictionary.Models;
using JP_Dictionary.Shared;

namespace JP_Dictionary.Pages
{
    public partial class Login
    {
        public List<Profile> Profiles = new();
        public string CreateName = string.Empty;

        [Inject] public UserState UserState { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        protected override void OnInitialized()
        {
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var filePath = @"L:\JP Dictionary\JP Dictionary\JP Dictionary\Data\Profiles.txt";

            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);

                if (content.Length > 0)
                {
                    Profiles = JsonSerializer.Deserialize<List<Profile>>(content);
                }
            }
        }

        private void LogIn(Profile profile)
        {
            var today = DateTime.Now.Date;
            var lastLoginDate = profile.LastLogin.Date;

            if (lastLoginDate == today)
            {
                // Already logged in today
            }
            else if (lastLoginDate == today.AddDays(-1))
            {
                // Logged in yesterday
                profile.LoginStreak++;
            }
            else
            {
                // Missed a day or first login
                profile.LoginStreak = 1;
            }

            if (lastLoginDate != today)
            {
                profile.LastLogin = DateTime.Now;
                profile.CurrentDay++;

                if (profile.CurrentDay > 7)
                {
                    profile.CurrentDay = 1;
                    profile.CurrentWeek++;
                }
            }

            HelperMethods.SaveProfile(profile);
            UserState.Profile = profile;

            Nav.NavigateTo("/dashboard");
        }

        private void CreateProfile()
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

            var profile = new Profile() { Name = CreateName };
            profiles.Add(profile);

            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            CreateName = string.Empty;
            LoadProfiles();
        }
    }
}
