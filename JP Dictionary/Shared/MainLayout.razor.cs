using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

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

        private async void Navigate(string page, bool forceLoad)
        {
            HideKanjiMenu();

            await Anim.RequestAnimation(Motions.ZoomOut);
            Nav.NavigateTo(page, forceLoad);
        }

        private void GoToLevelDetail(KeyValuePair<int, List<StudyKanji>> kanji)
        {
            var vocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!).Where(x => x.Level == kanji.Value.First().Level);

            User.SelectedKanjiGroup = kanji.Value;
            User.SelectedKanjiGroup.AddRange(vocab);

            Navigate("/kanjileveldetail", true);
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
