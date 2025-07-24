using JP_Dictionary.Models;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JP_Dictionary.Shared
{
    public static class KanjiMethods
    {
        public static List<StudyKanji> LoadDefaultKanjiList()
        {
            var kanjiFilePath = @"Data\Kanji + Radicals.json";
            var content = File.ReadAllText(kanjiFilePath);

            return JsonSerializer.Deserialize<List<StudyKanji>>(content)!;
        }

        public static void CreateUserKanji(Profile profile)
        {
            var path = HelperMethods.CreateFile($"{profile.Name} Kanji List.json");
            var userKanjis = LoadUserKanji(profile);

            if (userKanjis.Count == 0)
            {
                var content = JsonSerializer.Serialize(LoadDefaultKanjiList());
                File.WriteAllText(path, content);
            }
        }

        public static List<StudyKanji> LoadUserKanji(Profile profile)
        {
            var filePath = HelperMethods.GetFilePath($"{profile.Name} Kanji List.json");
            var content = File.ReadAllText(filePath);

            if (content.Length > 0)
            {
                return JsonSerializer.Deserialize<List<StudyKanji>>(content)!;
            }

            return new();
        }

        public static void SaveUserKanji(Profile profile, List<StudyKanji> kanji)
        {
            var filePath = HelperMethods.GetFilePath($"{profile.Name} Kanji List.json");

            File.Delete(filePath);
            HelperMethods.CreateFile(filePath);

            var content = JsonSerializer.Serialize(kanji);
            File.WriteAllText(filePath, content);
        }

        public static void UnlockNextSet(Profile profile)
        {
            var kanji = LoadUserKanji(profile);

            foreach (var k in kanji.Where(x => x.Level == profile.KanjiLevel && !x.Unlocked).Take(10))
            {
                k.Unlocked = true;
            }

            SaveUserKanji(profile, kanji);
        }

        public static List<StudyKanji> GetItemsToLearn(List<StudyKanji> kanji)
        {
            return kanji.FindAll(x => x.Unlocked && !x.Learned);
        }

        public static List<StudyKanji> GetItemsToReview(List<StudyKanji> kanji)
        {
            var kanjiToStudy = new List<StudyKanji>();

            foreach (var k in kanji.Where(x => x.Learned && x.Unlocked))
            {
                var nextDueDate = HelperMethods.GetNextStudyDate(k);

                if (DateTime.Today.Date >= nextDueDate)
                {
                    kanjiToStudy.Add(k);
                }
            }

            return kanjiToStudy;
        }
    }
}
