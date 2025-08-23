using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
using JP_Dictionary.Shared.Methods;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class GrammarReview
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

        private const string EmphasisTag = "<span style='color: #e65050; font-weight: bold;'>";

        private Motion Animate { get; set; } = default!;
        private GrammarItem ActiveItem { get; set; } = new();

        private bool LearnMode { get; set; }
        private bool ShowReturnButton { get; set; }

        private string NextItem { get; set; } = string.Empty;
        private string PreviousItem { get; set; } = string.Empty;

        protected override void OnInitialized()
        {
            var targetItem = new GrammarItem();

            if (User.SelectedGrammar != null)
            {
                targetItem = User.SelectedGrammar;
                User.SelectedGrammar = null;
            }
            else
            {
                targetItem = User.SelectedGrammarGroup.First();
            }

            SetActiveItem(targetItem);
            GetLeftItem();
            GetRightItem();

            if (User.TriggerLearnMode)
            {
                LearnMode = true;
                User.TriggerLearnMode = false;
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

        private void SetActiveItem(GrammarItem item)
        {
            ActiveItem = new GrammarItem
            {
                Name = item.Name,
                About = item.About,
                Lesson = item.Lesson,
                Learned = item.Learned,
                Meaning = item.Meaning,
                Unlocked = item.Unlocked,
                Questions = item.Questions,
                JLPTLevel = item.JLPTLevel,
                LastStudied = item.LastStudied,
                CorrectStreak = item.CorrectStreak,
                AboutExamples = item.AboutExamples,
            };

            foreach (var sentence in ActiveItem.AboutExamples)
            {
                sentence.JapaneseHTML = sentence.JapaneseHTML.Replace("<strong>", EmphasisTag);
                sentence.JapaneseHTML = sentence.JapaneseHTML.Replace("</strong>", "</span>");

                sentence.EnglishHTML = sentence.EnglishHTML.Replace("<strong>", EmphasisTag);
                sentence.EnglishHTML = sentence.EnglishHTML.Replace("</strong>", "</span>");
            }

            ActiveItem.About = Regex.Replace(ActiveItem.About, @$"(?<!{EmphasisTag})({ActiveItem.Name})(?!<\/span>)([。！？!?]?)", $"{EmphasisTag}$1</span>$2");
        }

        #region Left + Right Items
        private string GetLeftItem()
        {
            var item = User.SelectedGrammarGroup.First(x => x.Name == ActiveItem.Name);

            var activeItemIndex = User.SelectedGrammarGroup.IndexOf(item);
            var targetIndex = activeItemIndex - 1;

            if (targetIndex > -1)
            {
                PreviousItem = User.SelectedGrammarGroup[targetIndex].Name;
            }
            else
            {
                PreviousItem = string.Empty;
            }

            return PreviousItem;
        }

        private string GetRightItem()
        {
            var item = User.SelectedGrammarGroup.First(x => x.Name == ActiveItem.Name);

            var activeItemIndex = User.SelectedGrammarGroup.IndexOf(item);
            var targetIndex = activeItemIndex + 1;

            if (targetIndex <= User.SelectedGrammarGroup.Count - 1)
            {
                NextItem = User.SelectedGrammarGroup[targetIndex].Name;
            }
            else
            {
                NextItem = string.Empty;
            }

            return NextItem;
        }

        private async Task SetLeftItem()
        {
            var leftItem = GetLeftItem();

            if (leftItem != string.Empty)
            {
                await Animate.AnimateSlide(Motions.SlideRightOut);

                var targetItem = User.SelectedGrammarGroup.First(x => x.Name == leftItem);
                SetActiveItem(targetItem);

                GetLeftItem();
                GetRightItem();

                await Animate.AnimateSlide(Motions.SlideLeftIn);
            }
        }

        private async Task SetRightItem()
        {
            var rightItem = GetRightItem();

            if (rightItem != string.Empty)
            {
                await Animate.AnimateSlide(Motions.SlideLeftOut);

                var targetItem = User.SelectedGrammarGroup.First(x => x.Name == rightItem);
                SetActiveItem(targetItem);

                GetLeftItem();
                GetRightItem();

                await Animate.AnimateSlide(Motions.SlideRightIn);
            }
        }
        #endregion

        #region Nav
        private async Task GoToReview()
        {
            var allGrammar = GrammarMethods.LoadUserGrammar(User.Profile!);

            foreach (var item in User.SelectedGrammarGroup)
            {
                var grammar = allGrammar.First(x => x.Name == item.Name);
                grammar.Learned = true;
            }

            GrammarMethods.SaveUserGrammar(User.Profile!, allGrammar);
            await GoToStudy();
        }

        private async Task GoToStudy()
        {
            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/studygrammar");
        }

        private async void GoToBunpro()
        {
            var link = Path.Combine("https://bunpro.jp/grammar_points/", ActiveItem.Name);
            await JS.InvokeVoidAsync("openInNewTab", link);
        }
        #endregion

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
