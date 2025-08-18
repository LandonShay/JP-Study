using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
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

        [Parameter] public int Level { get; set; }
        private bool Reloaded { get; set; } // OnLocationChanged gets called several times during routing. Prevents LoadData being called several times in 1 load

        private Motion Animate { get; set; } = default!;
        private List<StudyItem> UserKanji { get; set; } = new();

        protected override void OnInitialized()
        {
            LoadData();
            Nav.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Anim.OnAnimate += AnimatePage;
                await AnimatePage(Motions.ZoomIn);
            }

            Reloaded = false;
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (!Reloaded && e.Location.Contains("/kanjileveldetail", StringComparison.OrdinalIgnoreCase))
            {
                Reloaded = true;

                LoadData();
                await AnimatePage(Motions.ZoomIn);
            }
        }

        private void LoadData()
        {
            UserKanji.Clear();

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

        private async Task ViewItem(StudyItem item)
        {
            User.SelectedKanji = item;

            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/kanjireview");
        }

        private void DeleteVocab(StudyItem vocab)
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
            Nav.LocationChanged -= OnLocationChanged;
        }
    }
}
