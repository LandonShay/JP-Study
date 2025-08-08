﻿using JP_Dictionary.Models;
using JP_Dictionary.Services;
using JP_Dictionary.Shared.Methods;
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

        private string NextItem { get; set; } = string.Empty;
        private string PreviousItem { get; set; } = string.Empty;

        private bool LearnMode { get; set; }
        private string ItemCSS { get; set; } = string.Empty;

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

            if (User.TriggerLearnMode)
            {
                LearnMode = true;
                User.TriggerLearnMode = false;
            }

            SetItemCSS();
            GetLeftItem();
            GetRightItem();
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

        private string GetLeftItem()
        {
            var activeItemIndex = Items.IndexOf(ActiveItem);
            var targetIndex = activeItemIndex - 1;

            if (targetIndex > -1)
            {
                PreviousItem = Items[targetIndex].Item;
            }
            else
            {
                PreviousItem = string.Empty;
            }

            return PreviousItem;
        }

        private string GetRightItem()
        {
            var activeItemIndex = Items.IndexOf(ActiveItem);
            var targetIndex = activeItemIndex + 1;

            if (targetIndex <= Items.Count - 1)
            {
                NextItem = Items[targetIndex].Item;
            }
            else
            {
                NextItem = string.Empty;
            }

            return NextItem;
        }

        private void SetLeftItem()
        {
            var leftItem = GetLeftItem();

            if (leftItem != string.Empty)
            {
                ActiveItem = Items.First(x => x.Item == leftItem);
                SetItemCSS();
            }

            GetLeftItem();
            GetRightItem();
        }

        private void SetRightItem()
        {
            var rightItem = GetRightItem();

            if (rightItem != string.Empty)
            {
                ActiveItem = Items.First(x => x.Item == rightItem);
                SetItemCSS();
            }

            GetLeftItem();
            GetRightItem();
        }

        private void SetItemCSS()
        {
            if (ActiveItem.Type == KanjiType.Kanji)
            {
                ItemCSS = "kanji-item";
            }
            else if (ActiveItem.Type == KanjiType.Radical)
            {
                ItemCSS = "radical-item";
            }
            else
            {
                ItemCSS = "vocab-item";
            }
        }

        private void GoToReview()
        {
            var allItems = KanjiMethods.LoadUserKanji(User.Profile!);

            foreach (var item in Items)
            {
                var userItem = allItems.First(x => x.Item == item.Item && x.Type == item.Type);
                userItem.Learned = true;
            }

            KanjiMethods.SaveUserKanji(User.Profile!, allItems);
            KanjiMethods.UnlockRelatedVocab(User.Profile!);

            Nav.NavigateTo("/studyvocab");
        }

        private async void GoToWanikani()
        {
            string url;

            if (ActiveItem.Type == KanjiType.Radical)
            {
                url = "https://www.wanikani.com/radicals/" + ActiveItem.Name.ToLower();

            }
            else if (ActiveItem.Type == KanjiType.Vocab)
            {
                url = "https://www.wanikani.com/vocabulary/" + ActiveItem.Item;
            }
            else
            {
                url = "https://www.wanikani.com/kanji/" + ActiveItem.Item;
            }

            await JS.InvokeVoidAsync("openInNewTab", url);
        }

        private async Task SearchJisho()
        {
            var link = Path.Combine("https://jisho.org/search/", ActiveItem.Item);
            await JS.InvokeVoidAsync("openInNewTab", link);
        }
    }
}
