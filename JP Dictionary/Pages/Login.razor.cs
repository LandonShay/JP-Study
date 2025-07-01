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

        #region Injections
#nullable disable
        [Inject] public UserState UserState { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var filePath = HelperMethods.GetFilePath("Profiles.txt");

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
            { // Already logged in today 
            }
            else if (lastLoginDate == today.AddDays(-1))
            { // Logged in yesterday
                profile.LoginStreak++;
            }
            else
            { // Missed a day or first login
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

                    if (profile.CurrentWeek == byte.MaxValue - 1)
                    {
                        profile.CurrentWeek--;
                    }
                }
            }

            HelperMethods.SaveProfile(profile);
            UserState.Profile = profile;

            Nav.NavigateTo("/dashboard");
        }

        private void CreateProfile()
        {
            var profiles = new List<Profile>();
            var directory = HelperMethods.GetFilePath("");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var profilesPath = HelperMethods.CreateFile("Profiles.txt");

            if (File.Exists(profilesPath))
            {
                string content;

                using (var reader = new StreamReader(profilesPath))
                {
                    content = reader.ReadToEnd();
                }

                if (content.Length > 0)
                {
                    profiles = JsonSerializer.Deserialize<List<Profile>>(content) ?? new List<Profile>();
                }
            }

            var profile = new Profile() { Name = CreateName, Decks = new List<string>() { "Core" } };
            profiles.Add(profile);

            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            using (var writer = new StreamWriter(profilesPath, false))
            {
                writer.Write(json);
            }

            // create core deck
            var wordsPath = HelperMethods.CreateFile($"{CreateName}Deck-Core.csv");

            if (File.Exists(wordsPath))
            {
                var coreWords = DeckMethods.LoadDefaultCoreWords();
                DeckMethods.UpdateDeck(coreWords, CreateName, "Core");
            }

            CreateName = string.Empty;
            LoadProfiles();
        }
    }
}
