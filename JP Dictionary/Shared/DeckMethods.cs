using JP_Dictionary.Models;

namespace JP_Dictionary.Shared
{
    public class DeckMethods
    {
        #region Load
        public static List<StudyWord> LoadDefaultDeck(string deckName)
        {
            var filePath = @$"Data\{deckName}.csv";

            if (File.Exists(filePath))
            {
                return HelperMethods.LoadCSVFile(filePath);
            }

            Console.WriteLine($"There is no default deck named {deckName}, no words loaded");
            return new List<StudyWord>();
        }

        public static List<StudyWord> LoadWordsToStudy(List<StudyWord> deck)
        {
            var wordsToStudy = new List<StudyWord>();

            foreach (var word in deck.Where(x => x.Unlocked))
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
        #endregion

        #region Create
        public static void CreateDeck(string deckName, DeckType deckType, Profile profile)
        {
            var deck = new Deck
            {
                Name = deckName,
                Type = deckType,
                SortOrder = profile.Decks.Max(x => x.SortOrder) + 1
            };

            HelperMethods.CreateFile($"{profile!.Name}Deck-{deckName}.csv");
            profile.Decks.Add(deck);

            deckName = string.Empty;
            deckType = DeckType.Vocab;

            HelperMethods.SaveProfile(profile);
        }

        public static void CreateDefaultDecks(Profile profile)
        {
            for (int i = 5; i > 0; i--)
            {
                var fileName = $"{profile.Name}Deck-N{i} Vocab.csv";
                var filePath = HelperMethods.GetFilePath(fileName);

                if (!File.Exists(filePath))
                {
                    var vocabPath = HelperMethods.CreateFile(fileName);

                    if (File.Exists(vocabPath))
                    {
                        var deck = LoadDefaultDeck($"JLPT N{i} Vocab");

                        if (deck.Count > 0)
                        {
                            CreateDeck($"N{i} Vocab", DeckType.Vocab, profile);

                            foreach (var word in deck.OrderBy(x => x.StudyOrder).Where(x => !x.Unlocked).Take(10))
                            {
                                word.Unlocked = true;
                            }

                            UpdateDeck(deck, profile.Name, $"N{i} Vocab");
                        }
                    }
                }
            }
        }
        #endregion

        #region Update
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
        #endregion
    }
}
