using JP_Dictionary.Models;
using System.Text.Json;

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
            var userKanjis = LoadUserKanji(profile);
            var lockedKanji = userKanjis.Where(x => !x.Unlocked).OrderBy(x => x.Level);

            var level = 0;
            var counter = 1;

            foreach (var kanji in lockedKanji)
            {
                if (level == 0)
                {
                    level = kanji.Level;
                }

                if (counter > 10 || kanji.Level > level)
                {
                    break;
                }

                if (kanji.Level == level)
                {
                    userKanjis.First(x => x.Item == kanji.Item && x.Type == kanji.Type).Unlocked = true;
                    counter++;
                }
            }

            SaveUserKanji(profile, userKanjis);
        }
    }
}
