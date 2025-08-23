using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class GrammarLessons
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

        [Parameter] public string Level { get; set; }
        private bool Reloaded { get; set; } // OnLocationChanged gets called several times during routing. Prevents LoadData being called several times in 1 load

        private Motion Animate { get; set; } = default!;
        private Dictionary<string, List<GrammarItem>> GroupedGrammar { get; set; } = new();

        #region Init + Rendering
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
            if (!Reloaded && e.Location.Contains("/grammarlessons", StringComparison.OrdinalIgnoreCase))
            {
                Reloaded = true;

                LoadData();
                await AnimatePage(Motions.ZoomIn);
            }
        }
        #endregion

        private void LoadData()
        {
            var userGrammar = GrammarMethods.LoadUserGrammar(User.Profile!);
            GroupedGrammar = userGrammar.FindAll(x => x.JLPTLevel == Level).GroupBy(x => x.Lesson).ToDictionary(g => g.Key, g => g.ToList());
        }

        private string ShortenString(string text)
        {
            if (text.Length > 35)
            {
                text = string.Concat(text.AsSpan(0, 35), "...");
            }

            return text;
        }

        private void GoToDetail(GrammarItem item)
        {
            var userGrammar = GrammarMethods.LoadUserGrammar(User.Profile!);

            User.SelectedGrammar = item;
            User.SelectedGrammarGroup = userGrammar.FindAll(x => x.JLPTLevel == Level && x.Lesson == item.Lesson);

            Nav.NavigateTo("/grammardetail");
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
