using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using System.Reflection.Emit;

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
        private bool ShowKanjiMenu { get; set; }
        private bool ShowLessonsMenu { get; set; }

        private Dictionary<int, List<StudyItem>> GroupedKanji { get; set; } = new();
        private List<string> GrammarLessons { get; set; } = new() { "N5", "N4", "N3", "N2", "N1" };

        protected override void OnInitialized()
        {
            GroupedKanji = KanjiMethods.LoadDefaultKanjiList().GroupBy(x => x.Level).ToDictionary(g => g.Key, g => g.ToList());
        }

        private async void Navigate(string page)
        {
            HideMenus();

            await Anim.RequestAnimation(Motions.ZoomOut);
            Nav.NavigateTo(page);
        }

        private void GoToLevelDetail(KeyValuePair<int, List<StudyItem>> kanji)
        {
            var level = kanji.Value.First().Level;
            var vocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!).Where(x => x.Level == level);

            User.SelectedKanjiGroup = kanji.Value;
            User.SelectedKanjiGroup.AddRange(vocab);

            Navigate($"/kanjileveldetail/{level}");
        }

        private void GoToLessonDetail(string lesson)
        {
            Navigate($"/grammarlessons/{lesson}");
        }

        private void ToggleKanjiMenu()
        {
            ShowLessonsMenu = false;
            ShowKanjiMenu = !ShowKanjiMenu;
        }

        private void ToggleLessonsMenu()
        {
            ShowKanjiMenu = false;
            ShowLessonsMenu = !ShowLessonsMenu;
        }

        private void HideMenus()
        {
            ShowLessonsMenu = false;
            ShowKanjiMenu = false;
        }
    }
}
