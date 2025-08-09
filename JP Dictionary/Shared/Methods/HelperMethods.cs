using System.Text.Json;
using System.Globalization;
using JP_Dictionary.Models;
using JP_Dictionary.Pages;
using Microsoft.JSInterop;
using CsvHelper.Configuration;
using CsvHelper;

namespace JP_Dictionary.Shared.Methods
{
    public static class HelperMethods
    {
        public static string GetFilePath(string fileName)
        {
            var filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(filePath, "JP Study", fileName);
        }

        public static DateTime GetNextStudyDate(DateTime lastStudied, int streak)
        {
            var date = lastStudied.AddDays(GetDelayFromStreak(streak));

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

        private static int GetDelayFromStreak(int streak)
        {
            return streak switch
            {
                0 => 1,
                1 => 1,
                2 => 2,
                3 => 4,
                4 => 5,
                5 => 7,
                6 => 10,
                7 => 14,
                8 => 14,
                9 => 21,
                _ => 30
            };
        }

        public static int GetTierFloor(MasteryTier tier)
        {
            return tier switch
            {
                MasteryTier.Novice => 0,
                MasteryTier.Beginner => 3,
                MasteryTier.Proficient => 5,
                MasteryTier.Expert => 8,
                _ => 11
            };
        }

        public static MasteryTier GetMasteryTier(int streak)
        {
            if (streak <= 2) // 0 - 2
            {
                return MasteryTier.Novice;
            }
            else if (streak <= 4) // 3 - 4
            {
                return MasteryTier.Beginner;
            }
            else if (streak <= 7) // 5 - 7
            {
                return MasteryTier.Proficient;
            }
            else if (streak <= 10) // 8 - 9
            {
                return MasteryTier.Expert;
            }

            return MasteryTier.Mastered; // 10
        }

        [JSInvokable]
        public static Task SetTalkingFalse()
        {
            ListeningComp.Talking = false;
            StudyVocab.Talking = false;
            ViewDeck.Talking = false;

            return Task.CompletedTask;
        }

        #region Files
        public static string CreateFile(string fileName)
        {
            var filePath = GetFilePath(fileName);

            if (!File.Exists(filePath))
            {
                using (File.Create(filePath)) { }
            }

            return filePath;
        }

        public static List<StudyWord> LoadCSVFile(string filePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);
            return csv.GetRecords<StudyWord>().ToList();
        }

        public static async Task<List<Sentence>> LoadExampleSentences()
        {
            var sentences = new List<Sentence>();

            await Task.Run(() =>
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    HeaderValidated = null,
                    MissingFieldFound = null
                };

                using var reader = new StreamReader(@"Data\ExampleSentences.csv");
                using var csv = new CsvReader(reader, config);

                sentences = csv.GetRecords<Sentence>().ToList();

                foreach (var sentence in sentences)
                {
                    if (!string.IsNullOrEmpty(sentence.KeywordsAsString))
                    {
                        sentence.Keywords = sentence.KeywordsAsString.Substring(0, sentence.KeywordsAsString.Length - 2).Split(";").ToList();
                    }
                }
            });

            return sentences;
        }

        public static void WriteToCSVFile(string filePath, List<StudyWord> words)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                MissingFieldFound = null
            };

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, config);
            csv.WriteRecords(words);
        }

        public static void DeleteFile(string fileName)
        {
            var filePath = GetFilePath(fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        #endregion
    }
}
