using JP_Dictionary.Models;
using JP_Dictionary.Services;
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

        private void ViewItem(StudyKanji item)
        {
            User.SelectedKanji = item;
            Nav.NavigateTo("/kanjireview");
        }
    }
}
