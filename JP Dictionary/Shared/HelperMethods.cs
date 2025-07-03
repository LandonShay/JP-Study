using System.Text.Json;
using System.Globalization;
using CsvHelper.Configuration;
using JP_Dictionary.Models;
using CsvHelper;

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
                5 => 14,
                _ => 30
            };
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
