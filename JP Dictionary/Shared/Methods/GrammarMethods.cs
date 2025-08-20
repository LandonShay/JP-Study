using System.Text.Json;
using JP_Dictionary.Models;

namespace JP_Dictionary.Shared.Methods
{
    public static class GrammarMethods
    {
        #region Load
        public static List<GrammarItem> LoadDefaultGrammar()
        {
            var filePath = @$"Data\BunproData.json";

            var content = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<GrammarItem>>(content)!;
        }

        public static List<GrammarItem> LoadUserGrammar(Profile profile)
        {
            var filePath = HelperMethods.GetFilePath($"{profile.Name} Grammar.json");
            var content = File.ReadAllText(filePath);

            if (content.Length > 0)
            {
                return JsonSerializer.Deserialize<List<GrammarItem>>(content)!;
            }

            return new();
        }
        #endregion

        public static void CreateUserGrammar(Profile profile)
        {
            var filePath = HelperMethods.CreateFile($"{profile.Name} Grammar.json");

            var userGrammar = LoadUserGrammar(profile);

            if (userGrammar.Count == 0)
            {
                var content = JsonSerializer.Serialize(LoadDefaultGrammar());
                File.WriteAllText(filePath, content);

                //UnlockNextSet(profile);
            }
        }
    }
}
