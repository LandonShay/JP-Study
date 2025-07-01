using JP_Dictionary.Models;

namespace JP_Dictionary.Shared
{
    public class DeckMethods
    {
        public static List<StudyWord> LoadDefaultCoreWords()
        {
            var filePath = @"Data\CoreWords.csv";

            if (File.Exists(filePath))
            {
                return HelperMethods.LoadCSVFile(filePath);
            }

            Console.WriteLine("Core words file not found, no words loaded");
            return new List<StudyWord>();
        }

        public static List<StudyWord> LoadWordsToStudy(Profile user, List<StudyWord> deck)
        {
            var wordsToStudy = new List<StudyWord>();
            var unlockedWords = deck.Where(x => (x.Week == user.CurrentWeek && x.Day <= user.CurrentDay) ||
                                               (x.Week < user.CurrentWeek)).ToList();

            foreach (var word in unlockedWords)
            {
                var nextDueDate = HelperMethods.GetNextStudyDate(word);

                if (DateTime.Today.Date >= nextDueDate)
                {
                    wordsToStudy.Add(word);
                }
            }

            return wordsToStudy;
        }

        public static List<StudyWord> LoadDeck(Profile user, string deckName)
        {
            var filePath = HelperMethods.GetFilePath($"{user.Name}Deck-{deckName}.csv");
            return HelperMethods.LoadCSVFile(filePath);
        }

        public static void UpdateDeck(List<StudyWord> words, string user, string deckName)
        {
            var filePath = HelperMethods.GetFilePath($"{user}Deck-{deckName}.csv");
            var existingWords = HelperMethods.LoadCSVFile(filePath);

            if (existingWords.Count > 0)
            {
                foreach (var updatedWord in words)
                {
                    var index = existingWords.FindIndex(w => w.Id == updatedWord.Id);

                    if (index != -1)
                    {
                        existingWords[index] = updatedWord;
                    }
                    else
                    {
                        existingWords.Add(updatedWord);
                    }
                }
            }
            else
            {
                existingWords = words;
            }

            HelperMethods.WriteToCSVFile(filePath, existingWords);
        }

        public static void OverwriteDeck(List<StudyWord> words, string user, string deckName)
        {
            var filePath = HelperMethods.GetFilePath($"{user}Deck-{deckName}.csv");
            HelperMethods.WriteToCSVFile(filePath, words);
        }
    }
}
