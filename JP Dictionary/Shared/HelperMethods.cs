using System.Globalization;
using CsvHelper.Configuration;
using JP_Dictionary.Models;
using CsvHelper;
using System.Text.Json;

namespace JP_Dictionary.Shared
{
    public static class HelperMethods
    {
        public static string GetFilePath(string fileName)
        {
            var filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(filePath, "JP Study", fileName);
        }

        public static List<StudyWord> LoadDefaultCoreWords()
        {
            var filePath = @"Data\CoreWords.csv";

            if (File.Exists(filePath))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    MissingFieldFound = null
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    return csv.GetRecords<StudyWord>().ToList();
                }
            }

            Console.WriteLine("Core words file not found, no words loaded");
            return new List<StudyWord>();
        }

        public static List<StudyWord> LoadPersonalCoreWords(Profile user)
        {
            var filePath = GetFilePath($"{user.Name}Words.csv");

            if (File.Exists(filePath))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    MissingFieldFound = null
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    return csv.GetRecords<StudyWord>().ToList();
                }
            }

            Console.WriteLine("Core words file not found, no words loaded");
            return new List<StudyWord>();
        }

        public static List<StudyWord> LoadUnlockedWords(Profile user)
        {
            var allWords = LoadPersonalCoreWords(user);

            return allWords.Where(x => (x.Week == user.CurrentWeek && x.Day <= user.CurrentDay) ||
                                       (x.Week < user.CurrentWeek)).ToList();
        }

        public static List<StudyWord> LoadWordsToStudy(Profile user)
        {
            var wordsToStudy = new List<StudyWord>();
            var unlockedWords = LoadUnlockedWords(user);

            foreach (var word in unlockedWords)
            {
                var nextDueDate = GetNextStudyDate(word);

                if (DateTime.Today.Date >= nextDueDate)
                {
                    wordsToStudy.Add(word);
                }
            }

            return wordsToStudy;
        }

        public static void UpdateWords(List<StudyWord> words, string user)
        {
            var existingWords = new List<StudyWord>();

            var filePath = GetFilePath($"{user}Words.csv");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                existingWords = csv.GetRecords<StudyWord>().ToList();
            }

            if (existingWords.Count > 0)
            {
                foreach (var updatedWord in words)
                {
                    var index = existingWords.FindIndex(w => w.Id == updatedWord.Id);

                    if (index != -1)
                    {
                        existingWords[index] = updatedWord;
                    }
                }
            }
            else
            {
                existingWords = words;
            }

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(existingWords);
            }
        }

        public static DateTime GetNextStudyDate(StudyWord word)
        {
            var date = word.LastStudied.AddDays(GetDelayFromStreak(word.CorrectStreak));

            if (date < DateTime.Today)
            {
                date = DateTime.Today;  
            }

            return date;
        }

        public static void SaveProfile(Profile profile)
        {
            List<Profile> profiles;
            var filePath = GetFilePath("Profiles.txt");

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

        public static int GetDelayFromStreak(int streak)
        {
            return streak switch
            {
                0 => 1,
                1 => 1,
                2 => 2,
                3 => 4,
                4 => 7,
                5 => 14,
                _ => 30
            };
        }
    }
}
