using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Shared
{
    public partial class MainLayout
    {
        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public AnimationService Anim { get; set; }
#nullable enable
        #endregion

        private bool IsLoginPage => Nav.Uri.EndsWith("/");
        private bool ShowKanjiMenu = false;

        private Dictionary<int, List<StudyKanji>> GroupedKanji { get; set; } = new();

        protected override void OnInitialized()
        {
            GroupedKanji = KanjiMethods.LoadDefaultKanjiList().GroupBy(x => x.Level).ToDictionary(g => g.Key, g => g.ToList());
        }

        private async void Navigate(string page)
        {
            HideKanjiMenu();

            await Anim.RequestAnimation(Motions.ZoomOut);
            Nav.NavigateTo(page);
        }

        private void GoToLevelDetail(KeyValuePair<int, List<StudyKanji>> kanji)
        {
            var level = kanji.Value.First().Level;
            var vocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!).Where(x => x.Level == level);

            User.SelectedKanjiGroup = kanji.Value;
            User.SelectedKanjiGroup.AddRange(vocab);

            Navigate($"/kanjileveldetail/{level}");
        }

        private void ToggleKanjiMenu()
        {
            ShowKanjiMenu = !ShowKanjiMenu;
        }

        private void HideKanjiMenu()
        {
            ShowKanjiMenu = false;
        }
    }
}
