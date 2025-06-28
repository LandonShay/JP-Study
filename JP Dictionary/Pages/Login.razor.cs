using System.Text.Json;
using Microsoft.AspNetCore.Components;
using JP_Dictionary.Models;

namespace JP_Dictionary.Pages
{
    public partial class Login
    {
        public List<Profile> Profiles = new();
        public string CreateName = string.Empty;

        [Parameter] public EventCallback<Pages> OnLogin { get; set; }
        [Parameter] public EventCallback<Profile> OnSetUser { get; set; }

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
            if (profile.LastLogin.AddDays(1).Date == DateTime.Now.Date || profile.LoginStreak == 0)
            {
                profile.LoginStreak++;
            }
            else
            {
                profile.LoginStreak = 0;
            }

            if (profile.LastLogin.Date != DateTime.Now.Date)
            {
                profile.LastLogin = DateTime.Now;
                profile.CurrentDay++;

                if (profile.CurrentDay > 7)
                {
                    profile.CurrentDay = 1;
                    profile.CurrentWeek++;
                }
            }

            SaveProfile(profile);

            OnSetUser.InvokeAsync(profile);
            OnLogin.InvokeAsync(Pages.Dashboard);
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
