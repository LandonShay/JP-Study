using JP_Dictionary.Models;
using System.Text.Json;

namespace JP_Dictionary.Shared.Methods
{
    public class DeckMethods
    {
        #region Load
        public static List<StudyItem> LoadDefaultDeck(string deckName)
        {
            var filePath = @$"Data\{deckName}.csv";

            if (File.Exists(filePath))
            {
                return HelperMethods.LoadCSVFile(filePath);
            }

            Console.WriteLine($"There is no default deck named {deckName}, no words loaded");
            return new List<StudyItem>();
        }

        public static List<StudyItem> LoadWordsToStudy(List<StudyItem> deck)
        {
            var wordsToStudy = new List<StudyItem>();

            foreach (var word in deck.Where(x => x.Unlocked))
            {
                var nextDueDate = HelperMethods.GetNextStudyDate(word.LastStudied, word.CorrectStreak);

                if (DateTime.Today.Date >= nextDueDate)
                {
                    wordsToStudy.Add(word);
                }
            }

            return wordsToStudy;
        }

        public static List<StudyItem> LoadDeck(Profile user, string deckName)
        {
            var filePath = HelperMethods.GetFilePath($"{user.Name}Deck-{deckName}.json");
            var content = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<List<StudyItem>>(content)!;
        }
        #endregion

        #region Create
        public static void CreateDeck(string deckName, DeckType deckType, string color, Profile profile)
        {
            var deck = new Deck
            {
                Name = deckName,
                Type = deckType,
                SortOrder = profile.Decks.Max(x => x.SortOrder) + 1,
                GraphColor = color
            };

            HelperMethods.CreateFile($"{profile!.Name}Deck-{deckName}.json");
            profile.Decks.Add(deck);

            deckName = string.Empty;
            deckType = DeckType.Vocab;

            HelperMethods.SaveProfile(profile);
        }

        public static void CreateDefaultDecks(Profile profile)
        {
            for (int i = 5; i > 0; i--)
            {
                var fileName = $"{profile.Name}Deck-N{i} Vocab.json";
                var filePath = HelperMethods.GetFilePath(fileName);

                if (!File.Exists(filePath))
                {
                    var vocabPath = HelperMethods.CreateFile(fileName);

                    if (File.Exists(vocabPath))
                    {
                        var deck = LoadDefaultDeck($"JLPT N{i} Vocab");

                        if (deck.Count > 0)
                        {
                            var color = i switch
                            {
                                1 => "rgba(100, 149, 237, 1)",
                                2 => "rgba(72, 209, 204, 1)",
                                3 => "rgba(144, 238, 144, 1)",
                                4 => "rgba(255, 215, 0, 1)",
                                _ => "rgba(255, 99, 71, 1)"
                            };

                            CreateDeck($"N{i} Vocab", DeckType.Vocab, color, profile);

                            foreach (var word in deck.OrderBy(x => x.StudyOrder).Where(x => !x.Unlocked).Take(10))
                            {
                                word.Unlocked = true;
                            }

                            UpdateDeck(deck, profile, $"N{i} Vocab");
                        }
                    }
                }
            }
        }
        #endregion

        #region Update
        public static void UpdateDeck(List<StudyItem> words, Profile user, string deckName)
        {
            var existingWords = LoadDeck(user, deckName);

            if (existingWords.Count > 0)
            {
                foreach (var updatedWord in words)
                {
                    var index = existingWords.FindIndex(w => w.Item == updatedWord.Item);

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

            SaveDeck(existingWords, user.Name, deckName);
        }

        public static void SaveDeck(List<StudyItem> words, string user, string deckName)
        {
            var filePath = HelperMethods.GetFilePath($"{user}Deck-{deckName}.json");

            File.Delete(filePath);
            HelperMethods.CreateFile(filePath);

            var content = JsonSerializer.Serialize(words);
            File.WriteAllText(filePath, content);
        }
        #endregion
    }
}
