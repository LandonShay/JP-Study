using CsvHelper;
using CsvHelper.Configuration;
using JP_Dictionary.Models;
using JP_Dictionary.Pages;
using Microsoft.JSInterop;
using System.Globalization;
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
                5 => 7,
                6 => 14,
                7 => 14,
                8 => 30,
                9 => 30,
                10 => 45,
                _ => 60
            };
        }

        [JSInvokable]
        public static Task SetTalkingFalse()
        {
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

        public static List<Sentence> LoadExampleSentences()
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

            var sentences = csv.GetRecords<Sentence>().ToList();

            foreach (var sentence in sentences)
            {
                if (!string.IsNullOrEmpty(sentence.KeywordsAsString))
                {
                    sentence.Keywords = sentence.KeywordsAsString.Substring(0, sentence.KeywordsAsString.Length - 2).Split(";").ToList();
                }
            }

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
