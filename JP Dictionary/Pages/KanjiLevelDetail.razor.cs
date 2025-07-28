using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class KanjiLevelDetail
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        private List<StudyKanji> UserKanji = new();

        protected override void OnInitialized()
        {
            var userKanji = KanjiMethods.LoadUserKanji(User.Profile!);

            foreach (var kanji in User.SelectedKanjiGroup)
            {
                var uk = userKanji.First(x => x.Item == kanji.Item && x.Type == kanji.Type);
                UserKanji.Add(uk);
            }
        }

        private void ViewItem(StudyKanji item)
        {
            User.SelectedKanji = item;
            Nav.NavigateTo("/kanjireview");
        }
    }
}
