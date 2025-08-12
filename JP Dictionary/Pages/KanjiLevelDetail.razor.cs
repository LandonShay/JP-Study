using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
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
        [Inject] public AnimationService Anim { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        private Motion Animate { get; set; } = default!;
        private List<StudyKanji> UserKanji { get; set; } = new();

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Anim.OnAnimate += AnimatePage;
                await AnimatePage(Motions.ZoomIn);
            }
        }

        private async Task ViewItem(StudyKanji item)
        {
            User.SelectedKanji = item;

            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/kanjireview");
        }

        private void DeleteVocab(StudyKanji vocab)
        {
            var userVocab = KanjiMethods.LoadUserKanjiVocab(User.Profile!);

            UserKanji.Remove(vocab);
            userVocab.RemoveAll(x => x.Item == vocab.Item);
            User.SelectedKanjiGroup.RemoveAll(x => x.Item == vocab.Item);

            KanjiMethods.SaveUserKanjiVocab(User.Profile!, userVocab);
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
