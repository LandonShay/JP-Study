using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class ViewDeck
    {
        private Motion Animate { get; set; } = default!;

        public List<StudyItem> AllWords = new();

        // sort/search
        private string? SortColumn = null;
        private bool SortDescending = false;
        private string SearchTerm = string.Empty;

        // pagination
        private int CurrentPage = 1;
        private int PageSize = 25;
        private int TotalPages => (int)Math.Ceiling(GetFilteredAndSortedWords().Count / (double)PageSize);
        private IEnumerable<StudyItem> PagedWords => GetFilteredAndSortedWords().Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        // inline editing
        private StudyItem? EditingEntry;
        private string EditingValue = string.Empty;

        public static bool Talking { get; set; }

        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public ToastService Toast { get; set; }
        [Inject] public AnimationService Anim { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            LoadPage();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Anim.OnAnimate += AnimatePage;
                await AnimatePage(Motions.ZoomIn);
            }
        }

        #region Loading
        private void LoadPage()
        {
            AllWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
        }
        #endregion

        #region Table Buttons
        private async Task SearchJisho(string word)
        {
            var link = Path.Combine("https://jisho.org/search/", word);
            await JS.InvokeVoidAsync("openInNewTab", link);
        }

        private void ResetStreak(StudyItem word)
        {
            var allWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
            var StudyItem = allWords.First(x => x.Item == word.Item && x.Meaning == word.Meaning);

            StudyItem.CorrectStreak = 0;
            StudyItem.LastStudied = DateTime.MinValue;

            DeckMethods.UpdateDeck(allWords, User.Profile!, User.SelectedDeck!.Name);
            LoadPage();
        }

        private void UnlockWord(StudyItem word)
        {
            var allWords = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
            allWords.First(x => x.Item == word.Item && x.Meaning == word.Meaning).Unlocked = true;

            DeckMethods.UpdateDeck(allWords, User.Profile!, User.SelectedDeck!.Name);
            LoadPage();
        }

        private void DeleteCard(StudyItem word)
        {
            var deck = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);

            deck.RemoveAll(x => x.Item == word.Item && x.Meaning == word.Meaning);
            DeckMethods.SaveDeck(deck, User.Profile!.Name, User.SelectedDeck!.Name);

            LoadPage();
        }
        #endregion

        #region Definition Editing
        private void StartEditing(StudyItem entry)
        {
            EditingEntry = entry;
            EditingValue = string.Join(',', entry.Meaning);
        }

        private void FinishEditing()
        {
            if (EditingEntry != null)
            {
                var word = AllWords.First(x => x.Item == EditingEntry.Item);
                word.Meaning = EditingValue.Split(',').ToList();

                DeckMethods.UpdateDeck(AllWords, User.Profile!, User.SelectedDeck!.Name);

                EditingEntry = null;
                EditingValue = string.Empty;

                LoadPage();
            }
        }
        #endregion

        #region Search/Sort
        private List<StudyItem> GetFilteredAndSortedWords()
        {
            var query = AllWords.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();

                query = query.Where(w =>
                    w.Item.ToLower().Contains(term) ||
                    w.Reading.ToLower().Contains(term) ||
                    w.Meaning.Any(x => x.ToLower().Contains(term)));
            }

            if (!string.IsNullOrEmpty(SortColumn))
            {
                query = SortColumn switch
                {
                    nameof(StudyItem.Item) => SortDescending ? query.OrderByDescending(w => w.Item) : query.OrderBy(w => w.Item),
                    nameof(StudyItem.Reading) => SortDescending ? query.OrderByDescending(w => w.Reading) : query.OrderBy(w => w.Reading),
                    nameof(StudyItem.Meaning) => SortDescending ? query.OrderByDescending(w => w.Meaning) : query.OrderBy(w => w.Meaning),
                    nameof(StudyItem.CorrectStreak) => SortDescending ? query.OrderByDescending(w => w.CorrectStreak) : query.OrderBy(w => w.CorrectStreak),
                    nameof(StudyItem.MasteryTier) => SortDescending ? query.OrderByDescending(w => w.MasteryTier) : query.OrderBy(w => w.MasteryTier),
                    nameof(StudyItem.LastStudied) => SortDescending ? query.OrderByDescending(w => w.LastStudied) : query.OrderBy(w => w.LastStudied),
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
        private async Task TextToSpeech(StudyItem word)
        {
            try
            {
                if (!Talking)
                {
                    Talking = true;
                    await JS.InvokeVoidAsync("speakTextBasic", word.Reading, 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured during TTS, see console for details");
            }
        }
        #endregion

        private async Task GoToCreate()
        {
            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/createcard");
        }

        private async Task AnimatePage(Motions motion)
        {
            await Animate.Animate(motion);
        }

        public void Dispose()
        {
            Anim.OnAnimate -= AnimatePage;
        }
    }
}
