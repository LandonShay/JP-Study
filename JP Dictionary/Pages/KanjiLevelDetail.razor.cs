using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
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
            var userVocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!);

            foreach (var kanji in User.SelectedKanjiGroup)
            {
                var uk = userKanji.FirstOrDefault(x => x.Item == kanji.Item && x.Type == kanji.Type);

                if (uk != null)
                {
                    UserKanji.Add(uk);
                }
                else
                {
                    var uv = userVocab.First(x => x.Item == kanji.Item);
                    UserKanji.Add(uv);
                }
            }
        }

        private void ViewItem(StudyKanji item)
        {
            User.SelectedKanji = item;
            Nav.NavigateTo("/kanjireview");
        }
    }
}
