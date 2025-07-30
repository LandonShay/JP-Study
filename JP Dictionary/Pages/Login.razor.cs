using System.Text.Json;
using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class Login
    {
        private List<Profile> Profiles { get; set; } = new();
        private string CreateName { get; set; } = string.Empty;
        private bool LoadingSentences { get; set; }

        #region Injections
#nullable disable
        [Inject] public UserState UserState { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            LoadSentencesAsync();
            LoadProfiles();
        }

        #region Loading
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

        private async void LoadSentencesAsync()
        {
            if (UserState.Sentences.Count == 0 && !LoadingSentences)
            {
                LoadingSentences = true;

                try
                {
                    UserState.Sentences = await HelperMethods.LoadExampleSentences();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load sentences: {ex.Message}");
                }

                LoadingSentences = false;
            }
        }
        #endregion

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

            DeckMethods.CreateDefaultDecks(profile);
            KanjiMethods.CreateUserKanji(profile);

            var userKanjis = KanjiMethods.LoadUserKanji(profile);

            if (lastLoginDate != today)
            {
                profile.LastLogin = DateTime.Now;

                // unlock 15 locked words from each deck that isn't paused for gradual study
                foreach (var deck in profile.Decks.Where(x => !x.Paused))
                {
                    var words = DeckMethods.LoadDeck(profile, deck.Name);

                    foreach (var word in words.OrderBy(x => x.StudyOrder).Where(x => !x.Unlocked).Take(15))
                    {
                        word.Unlocked = true;
                    }

                    DeckMethods.OverwriteDeck(words, profile.Name, deck.Name);
                }

                if (userKanjis.All(x => !x.Unlocked) || userKanjis.Where(x => x.Unlocked).All(x => x.Learned))
                {
                    KanjiMethods.UnlockNextSet(profile);
                }
            }

            HelperMethods.SaveProfile(profile);

            UserState.Profile = profile;
            UserState.Kanji = KanjiMethods.LoadUserKanji(profile);

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

            var profile = new Profile() { Name = CreateName, Decks = new List<Deck>() { new Deck { Name = "Core", Type = DeckType.Vocab, SortOrder = 1 } } };
            profiles.Add(profile);

            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            using (var writer = new StreamWriter(profilesPath, false))
            {
                writer.Write(json);
            }

            DeckMethods.CreateDefaultDecks(profile);
            KanjiMethods.CreateUserKanji(profile);

            CreateName = string.Empty;
            LoadProfiles();
        }
    }
}
