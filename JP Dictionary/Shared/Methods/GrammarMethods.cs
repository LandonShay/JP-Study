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

                UnlockNextSet(profile);
            }
        }

        public static void SaveUserGrammar(Profile profile, List<GrammarItem> items)
        {
            var filePath = HelperMethods.GetFilePath($"{profile.Name} Grammar.json");

            File.Delete(filePath);
            HelperMethods.CreateFile(filePath);

            var content = JsonSerializer.Serialize(items);
            File.WriteAllText(filePath, content);
        }

        public static void UnlockNextSet(Profile profile)
        {
            var grammar = LoadUserGrammar(profile);

            foreach (var item in grammar.Where(x => !x.Unlocked && x.JLPTLevel == profile.GrammarLevel).Take(4))
            {
                item.Unlocked = true;
            }

            SaveUserGrammar(profile, grammar);
        }

        public static List<GrammarItem> GetItemsToLearn(List<GrammarItem> grammar)
        {
            return grammar.FindAll(x => x.Unlocked && !x.Learned);
        }

        public static List<GrammarItem> GetItemsToReview(List<GrammarItem> grammar)
        {
            var grammarToStudy = new List<GrammarItem>();

            foreach (var k in grammar.Where(x => x.Learned && x.Unlocked))
            {
                var nextDueDate = HelperMethods.GetNextStudyDate(k.LastStudied, k.CorrectStreak);

                if (DateTime.Today.Date >= nextDueDate)
                {
                    grammarToStudy.Add(k);
                }
            }

            return grammarToStudy;
        }

        public static string GetHighestGrammarLevel(List<GrammarItem> grammar)
        {
            var ranks = new Dictionary<string, int> { ["N1"] = 1, ["N2"] = 2, ["N3"] = 3, ["N4"] = 4, ["N5"] = 5 };
            return grammar.Where(x => x.Unlocked).OrderBy(x => ranks[x.JLPTLevel]).Select(x => x.JLPTLevel).FirstOrDefault() ?? "N5";
        }

        public static string GetCurrentGrammarLesson(List<GrammarItem> grammar)
        {
            var currentLevel = GetHighestGrammarLevel(grammar);
            var lessonNum = grammar.Where(x => x.JLPTLevel == currentLevel && x.Unlocked).Select(x => ParseLessonNumber(x.Lesson)).DefaultIfEmpty(1).Max();

            return $"Lesson {lessonNum}";
        }

        public static int ParseLessonNumber(string lesson)
        {
            if (lesson != null && lesson.StartsWith("Lesson") && int.TryParse(lesson.Split(' ').Last(), out var num))
            {
                return num;
            }

            return 1;
        }
    }
}
