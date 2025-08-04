using JP_Dictionary.Models;
using System.Text.Json;

namespace JP_Dictionary.Shared.Methods
{
    public static class KanjiMethods
    {
        public static List<StudyKanji> LoadDefaultKanjiList()
        {
            var kanjiFilePath = @"Data\Kanji + Radicals.json";
            var content = File.ReadAllText(kanjiFilePath);

            return JsonSerializer.Deserialize<List<StudyKanji>>(content)!;
        }

        public static List<StudyKanji> LoadDefaultWanikaniVocab()
        {
            var filePath = @"Data\Wanikani Vocab.json";
            var content = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<List<StudyKanji>>(content)!;
        }

        public static void CreateUserKanji(Profile profile)
        {
            var userKanjiPath = HelperMethods.CreateFile($"{profile.Name} Kanji List.json");
            var userKanjiVocabPath = HelperMethods.CreateFile($"{profile.Name} Kanji Vocab.json");

            var userKanjis = LoadUserKanji(profile);
            var userKanjiVocab = LoadUserKanjiVocab(profile);

            if (userKanjis.Count == 0)
            {
                var content = JsonSerializer.Serialize(LoadDefaultKanjiList());
                File.WriteAllText(userKanjiPath, content);

                UnlockNextSet(profile);
            }

            if (userKanjiVocab.Count == 0)
            {
                var content = JsonSerializer.Serialize(LoadDefaultWanikaniVocab());
                File.WriteAllText(userKanjiVocabPath, content);
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

        public static List<StudyKanji> LoadUserKanjiVocab(Profile profile)
        {
            var filePath = HelperMethods.GetFilePath($"{profile.Name} Kanji Vocab.json");
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

        public static void SaveUserKanjiVocab(Profile profile, List<StudyKanji> kanji)
        {
            var filePath = HelperMethods.GetFilePath($"{profile.Name} Kanji Vocab.json");

            File.Delete(filePath);
            HelperMethods.CreateFile(filePath);

            var content = JsonSerializer.Serialize(kanji);
            File.WriteAllText(filePath, content);
        }

        public static void UnlockNextSet(Profile profile)
        {
            var kanji = LoadUserKanji(profile);

            foreach (var k in kanji.Where(x => x.Level == profile.KanjiLevel && !x.Unlocked).Take(15))
            {
                k.Unlocked = true;
            }

            SaveUserKanji(profile, kanji);
        }

        public static void UnlockRelatedVocab(Profile profile)
        {
            var somethingUnlocked = false;

            var kanji = LoadUserKanji(profile);
            var vocab = LoadUserKanjiVocab(profile);

            foreach (var v in vocab.Where(x => x.Level <= profile.KanjiLevel && !x.Unlocked))
            {
                foreach (var k in v.Kanji)
                {
                    var uk = kanji.FirstOrDefault(x => x.Item == k);

                    if (uk != null)
                    {
                        if (uk.Learned)
                        {
                            v.Learned = true;
                            v.Unlocked = true;

                            somethingUnlocked = true;
                        }
                    }
                }
            }

            if (somethingUnlocked)
            {
                SaveUserKanjiVocab(profile, vocab);
            }
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
                var nextDueDate = HelperMethods.GetNextStudyDate(k.LastStudied, k.CorrectStreak);

                if (DateTime.Today.Date >= nextDueDate)
                {
                    kanjiToStudy.Add(k);
                }
            }

            return kanjiToStudy;
        }
    }
}
