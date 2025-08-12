using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WanaKanaSharp;
using MoreLinq;
using MeCab;
using System.Threading.Tasks;

namespace JP_Dictionary.Pages
{
    public partial class ListeningComp
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public ToastService Toast { get; set; }
        [Inject] public AnimationService Anim { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
#nullable enable
        #endregion

        private Motion Animate { get; set; } = default!;

        private int TimesSpoken { get; set; }
        private bool ShowResults { get; set; }
        public static bool Talking { get; set; }
        private bool OneChanceMode { get; set; }
        private bool SelectingOptions { get; set; } = true;
        private string Answer { get; set; } = string.Empty;
        private float TalkSpeed { get; set; } = .75f;

        private Sentence Sentence { get; set; } = new();
        private List<Sentence> UsedSentences { get; set; } = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Anim.OnAnimate += AnimatePage;
                await AnimatePage(Motions.ZoomIn);
            }
        }

        private void ConfirmTestOptions()
        {
            SelectingOptions = false;
            _ = GetNextSentence();
        }

        private async Task GetNextSentence()
        {
            try
            {
                TimesSpoken = 0;
                ShowResults = false;
                Answer = string.Empty;

                Sentence = User.Sentences.Shuffle().First(x => !UsedSentences.Contains(x));

                using (var tagger = MeCabTagger.Create())
                {
                    var nodes = tagger.ParseToNodes(Sentence.JP);

                    foreach (var node in nodes)
                    {
                        if (node.CharType > 0)
                        {
                            var features = node.Feature.Split(',');
                            var katakana = features.Length > 7 ? features[7] : node.Surface;

                            var romaji = WanaKana.ToRomaji(katakana);

                            if (romaji == "." || romaji == "?" || romaji == "!")
                            {
                                Sentence.RomajiReading = Sentence.RomajiReading.Remove(Sentence.RomajiReading.Length - 1) + romaji;
                            }
                            else
                            {
                                Sentence.RomajiReading += romaji + " ";
                            }
                        }
                    }

                    Sentence.RomajiReading = Sentence.RomajiReading.Trim();
                }

                UsedSentences.Add(Sentence);

                if (!OneChanceMode)
                {
                    await TextToSpeech();
                }

                await FocusElement("answer-area");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("Error getting next sentence");
            }
        }

        private async void ShowAnswer()
        {
            ShowResults = true;
            await FocusElement("next");
        }

        private async Task TextToSpeech()
        {
            try
            {
                if (!Talking && (!OneChanceMode || TimesSpoken == 0))
                {
                    TimesSpoken++;
                    Talking = true;

                    await JS.InvokeVoidAsync("speakTextBasic", Sentence.JP, TalkSpeed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Toast.ShowError("An error occured during TTS, see console for details");
            }
        }

        private async Task ReturnToDashboard()
        {
            await AnimatePage(Motions.ZoomOut);
            Nav.NavigateTo("/dashboard");
        }

        private async Task FocusElement(string elementId)
        {
            await JS.InvokeVoidAsync("focusElementById", elementId);
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
