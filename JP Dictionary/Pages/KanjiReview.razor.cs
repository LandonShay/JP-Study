using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JP_Dictionary.Pages
{
    public partial class KanjiReview
    {
        #region Injections
#nullable disable
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService Toast { get; set; }
#nullable enable
        #endregion

        private StudyKanji ActiveItem { get; set; } = new();
        private List<StudyKanji> Items { get; set; } = new();
        private List<StudyKanji> AllRadicals { get; set; } = new();

        protected override void OnInitialized()
        {
            AllRadicals = KanjiMethods.LoadDefaultKanjiList().Where(x => x.Type == KanjiType.Radical).ToList();

            if (User.SelectedKanji != null)
            {
                ActiveItem = User.SelectedKanjiGroup.First(x => x.Item == User.SelectedKanji.Item && x.Type == User.SelectedKanji.Type);
                Items = User.SelectedKanjiGroup.FindAll(x => x.Type == User.SelectedKanji.Type);

                User.SelectedKanji = null;
            }
            else
            {
                ActiveItem = User.SelectedKanjiGroup.First();
                Items = User.SelectedKanjiGroup;
            }
        }

        private string GetReadings(List<string> readings)
        {
            var readingsAsString = string.Empty;

            foreach (var reading in readings)
            {
                readingsAsString += reading;

                if (reading != readings.Last())
                {
                    readingsAsString += ", ";
                }
            }

            return readingsAsString != string.Empty ? readingsAsString : "None";
        }

        private string GetRadicals(List<string> radicals)
        {
            var radicalsAsString = string.Empty;

            foreach (var radical in radicals)
            {
                var radicalName = AllRadicals.First(x => x.Item == radical).Name;

                radicalsAsString += $"{radical} ({radicalName})";

                if (radical != radicals.Last())
                {
                    radicalsAsString += " + ";
                }
            }

            return radicalsAsString != string.Empty ? radicalsAsString : "None";
        }
    }
}
