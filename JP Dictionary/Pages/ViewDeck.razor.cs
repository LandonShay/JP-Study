using JP_Dictionary.Shared;
using JP_Dictionary.Models;
using JP_Dictionary.Services;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class ViewDeck
    {
        public List<StudyWord> AllWords = new();

        // sort/search
        private string? SortColumn = null;
        private bool SortDescending = false;
        private string SearchTerm = string.Empty;

        // pagination
        private int CurrentPage = 1;
        private int PageSize = 25;
        private int TotalPages => (int)Math.Ceiling(GetFilteredAndSortedWords().Count / (double)PageSize);
        private IEnumerable<StudyWord> PagedWords => GetFilteredAndSortedWords().Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        // inline editing
        private StudyWord? EditingEntry;
        private string EditingValue = string.Empty;

        // audio generation
        private int ProgressPercentage => AudioTotal == 0 ? 0 : (AudioProgress * 100 / AudioTotal);
        private bool IsGeneratingAudio = false;
        private int AudioProgress = 0;
        private int AudioTotal = 0;
        public static bool Talking { get; set; }

        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public ToastService Toast { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            LoadPage();
        }

        private void LoadPage()
        {
            AllWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
        }

        private string GetNextReview(StudyWord word)
        {
            if (User.SelectedDeck!.Name == "Core")
            {
                if (word.LastStudied == DateTime.MinValue && 
                   (word.Week > User.Profile!.CurrentWeek ||
                   (word.Week == User.Profile!.CurrentWeek && word.Day > User.Profile!.CurrentDay)))
                {
                    return "🔒";
                }
            }
            else
            {
                if (word.LastStudied == DateTime.MinValue && word.Day > 7)
                {
                    return "🔒";
                }
            }

            return HelperMethods.GetNextStudyDate(word).ToShortDateString();
        }

        private void ResetStreak(StudyWord word)
        {
            var allWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
            var studyWord = allWords.First(x => x.Id == word.Id);

            studyWord.CorrectStreak = 0;
            studyWord.LastStudied = DateTime.MinValue;

            DeckMethods.UpdateDeck(allWords, User.Profile!.Name, User.SelectedDeck!.Name);
            LoadPage();
        }

        private void DeleteCard(StudyWord word)
        {
            var deck = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);

            deck.RemoveAll(x => x.Id == word.Id);
            DeckMethods.OverwriteDeck(deck, User.Profile!.Name, User.SelectedDeck!.Name);

            LoadPage();
        }

        #region Definition Editing
        private void StartEditing(StudyWord entry)
        {
            EditingEntry = entry;
            EditingValue = entry.Definitions;
        }

        private void FinishEditing()
        {
            if (EditingEntry != null)
            {
                var word = AllWords.First(x => x.Id == EditingEntry.Id);
                word.Definitions = EditingValue;

                DeckMethods.UpdateDeck(AllWords, User.Profile!.Name, User.SelectedDeck!.Name);

                EditingEntry = null;
                EditingValue = string.Empty;

                LoadPage();
            }
        }
        #endregion

        #region Search/Sort
        private List<StudyWord> GetFilteredAndSortedWords()
        {
            var query = AllWords.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();

                query = query.Where(w =>
                    w.Japanese.ToLower().Contains(term) ||
                    w.Pronounciation.ToLower().Contains(term) ||
                    w.Definitions.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(SortColumn))
            {
                query = SortColumn switch
                {
                    nameof(StudyWord.Japanese) => SortDescending ? query.OrderByDescending(w => w.Japanese) : query.OrderBy(w => w.Japanese),
                    nameof(StudyWord.Pronounciation) => SortDescending ? query.OrderByDescending(w => w.Pronounciation) : query.OrderBy(w => w.Pronounciation),
                    nameof(StudyWord.Definitions) => SortDescending ? query.OrderByDescending(w => w.Definitions) : query.OrderBy(w => w.Definitions),
                    nameof(StudyWord.CorrectStreak) => SortDescending ? query.OrderByDescending(w => w.CorrectStreak) : query.OrderBy(w => w.CorrectStreak),
                    nameof(StudyWord.MasteryTier) => SortDescending ? query.OrderByDescending(w => w.MasteryTier) : query.OrderBy(w => w.MasteryTier),
                    nameof(StudyWord.LastStudied) => SortDescending ? query.OrderByDescending(w => w.LastStudied) : query.OrderBy(w => w.LastStudied),
                    _ => query
                };
            }

            return query.ToList();
        }

        private void ToggleSort(string column)
        {
            if (SortColumn == column)
            {
                if (!SortDescending)
                {
                    SortDescending = true;
                }
                else
                {
                    SortColumn = null;
                    SortDescending = false;
                }
            }
            else
            {
                SortColumn = column;
                SortDescending = false;
            }
        }

        private MarkupString SortIndicator(string column)
        {
            if (SortColumn != column)
            {
                return (MarkupString)"";
            }

            var arrow = SortDescending ? "▼" : "▲";
            return (MarkupString)$"<span class='sort-icon'>{arrow}</span>";
        }
        #endregion

        #region Pagination
        private void ChangePage(int page)
        {
            if (page < 1 || page > TotalPages) return;
            CurrentPage = page;
        }

        private void SetPageSize(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int size))
            {
                PageSize = size;
                CurrentPage = 1;
            }
        }
        #endregion

        #region Audio
        private async void GenerateAudio()
        {
            var directoryPath = HelperMethods.GetFilePath("") + $@"\{User.Profile!.Name}Audio";

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            IsGeneratingAudio = true;

            try
            {
                var wordsWithoutAudio = AllWords.FindAll(x => x.Audio == string.Empty);

                if (wordsWithoutAudio.Count > 0)
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"C:\Users\Landon\AppData\jp-study-tts-86eb099cbed5.json");

                    var client = await TextToSpeechClient.CreateAsync();
                    AudioTotal = wordsWithoutAudio.Count;

                    for (int i = 0; i < wordsWithoutAudio.Count; i++)
                    {
                        AudioProgress = i + 1;

                        var wordWithoutAudio = wordsWithoutAudio[i];
                        var word = AllWords.First(x => x.Id == wordWithoutAudio.Id);

                        var response = await client.SynthesizeSpeechAsync(new SynthesizeSpeechRequest
                        {
                            Input = new SynthesisInput { Text = wordWithoutAudio.Japanese },
                            Voice = new VoiceSelectionParams { LanguageCode = "ja-JP", SsmlGender = SsmlVoiceGender.Female },
                            AudioConfig = new AudioConfig { AudioEncoding = AudioEncoding.Mp3 }
                        });

                        var audioAsBytes = response.AudioContent.ToByteArray();

                        var fileName = $"{word.Japanese}.mp3";
                        var filePath = Path.Combine(directoryPath, fileName);

                        if (!File.Exists(filePath))
                        {
                            File.WriteAllBytes(filePath, audioAsBytes);
                        }

                        word.Audio = $@"{User.Profile!.Name}Audio\{fileName}";
                        DeckMethods.UpdateDeck(AllWords, User.Profile!.Name, User.SelectedDeck!.Name);

                        StateHasChanged();
                    }

                    Toast.ShowSuccess("Audio successfully generated!");
                }
                else
                {
                    Toast.ShowInfo("All cards in this deck already have audio");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured while generating audio");
            }

            IsGeneratingAudio = false;
            AudioProgress = 0;
            AudioTotal = 0;

            StateHasChanged();
        }

        private async Task TextToSpeech(string audioPath)
        {
            if (!Talking)
            {
                Talking = true;

                var filePath = HelperMethods.GetFilePath(audioPath);
                var bytes = await File.ReadAllBytesAsync(filePath);
                var base64 = Convert.ToBase64String(bytes);

                await JS.InvokeVoidAsync("speakText", base64);
            }
        }
        #endregion
    }
}
