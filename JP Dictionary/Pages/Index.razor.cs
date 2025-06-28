using JP_Dictionary.Models;
using System.Text.Json;

namespace JP_Dictionary.Pages
{
    public partial class Index
    {
        public Pages ActivePage = Pages.Dashboard;
        public Profile? ActiveUser;

        protected override void OnInitialized()
        {
            var profiles = new List<Profile>();

            var filePath = @"L:\JP Dictionary\JP Dictionary\JP Dictionary\Data\Profiles.txt";

            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);

                if (content.Length > 0)
                {
                    profiles = JsonSerializer.Deserialize<List<Profile>>(content);
                }

                var profile = profiles.First(x => x.Name == "Landon");

                if (profile.LastLogin.Date != DateTime.Now.Date)
                {
                    profile.LastLogin = DateTime.Now;
                }

                SetUser(profile);
            }
            else
            {
                Console.WriteLine("Profile not found, aborting...");
            }
        }

        public void SetUser(Profile profile)
        {
            ActiveUser = profile;
        }

        public void ChangePage(Pages page)
        {
            ActivePage = page;
        }
    }

    public enum Pages
    {
        Login,
        Dashboard,
        StudyVocab,
        UnlockedWords
    }
}
