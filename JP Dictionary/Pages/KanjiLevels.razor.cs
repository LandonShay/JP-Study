using JP_Dictionary.Models;
using JP_Dictionary.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class KanjiLevels
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        private Dictionary<int, List<StudyKanji>> GroupedKanji = new();

        protected override void OnInitialized()
        {
            GroupedKanji = User.Kanji.GroupBy(x => x.Level).ToDictionary(g => g.Key, g => g.ToList());
        }

        private int GetUnlockedKanjiPerGroup(int level)
        {
            return User.Kanji.Count(x => x.Level == level && x.Unlocked);
        }

        private void GoToLevelDetail(KeyValuePair<int, List<StudyKanji>> kanji)
        {
            User.SelectedKanjiGroup = kanji.Value;
            Nav.NavigateTo("/kanjileveldetail");
        }

        private void GoToLearnKanji(KeyValuePair<int, List<StudyKanji>> kanji)
        {
            User.SelectedKanjiGroup = kanji.Value.FindAll(x => x.Unlocked && !x.Learned);
            Nav.NavigateTo("/kanjireview");
        }

        private void GoToViewKanji(KeyValuePair<int, List<StudyKanji>> kanji)
        {
            User.SelectedKanjiGroup = kanji.Value;
            Nav.NavigateTo("/kanjileveldetail");
        }
    }
}
