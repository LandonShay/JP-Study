﻿using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class UnlockedWords
    {
        public List<StudyWord> AllWords = new();
        public List<StudyWord> TodaysWords = new();

        private string SearchTerm = "";
        private string? SortColumn = null;
        private bool SortDescending = false;
        private List<StudyWord> FilteredWords => GetFilteredAndSortedWords();

        private StudyWord? EditingEntry;
        private string EditingValue = string.Empty;

        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
#nullable enable
        #endregion

        protected override void OnInitialized()
        {
            LoadPage();
        }

        private void LoadPage()
        {
            AllWords = HelperMethods.LoadPersonalCoreWords(User.Profile!);
            TodaysWords = HelperMethods.LoadUnlockedWords(User.Profile!);
        }

        private void ResetStreak(StudyWord word)
        {
            var allWords = HelperMethods.LoadPersonalCoreWords(User.Profile!);
            var studyWord = allWords.First(x => x.Id == word.Id);

            studyWord.CorrectStreak = 0;
            studyWord.LastStudied = DateTime.MinValue;

            HelperMethods.UpdateWords(allWords, User.Profile!.Name);
            LoadPage();
        }

        #region Definition Editing
        private void StartEditing(StudyWord entry)
        {
            EditingEntry = entry;
            EditingValue = entry.Definitions;
        }

        private void FinishEditing()
        {
            if (EditingEntry != null)
            {
                var word = AllWords.First(x => x.Id == EditingEntry.Id);
                word.Definitions = EditingValue;

                HelperMethods.UpdateWords(AllWords, User.Profile!.Name);

                EditingEntry = null;
                EditingValue = string.Empty;

                LoadPage();
            }
        }
        #endregion

        #region Search/Sort
        private List<StudyWord> GetFilteredAndSortedWords()
        {
            var query = TodaysWords.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();

                query = query.Where(w =>
                    w.Japanese.ToLower().Contains(term) ||
                    w.Pronounciation.ToLower().Contains(term) ||
                    w.Definitions.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(SortColumn))
            {
                query = SortColumn switch
                {
                    nameof(StudyWord.Japanese) => SortDescending ? query.OrderByDescending(w => w.Japanese) : query.OrderBy(w => w.Japanese),
                    nameof(StudyWord.Pronounciation) => SortDescending ? query.OrderByDescending(w => w.Pronounciation) : query.OrderBy(w => w.Pronounciation),
                    nameof(StudyWord.Definitions) => SortDescending ? query.OrderByDescending(w => w.Definitions) : query.OrderBy(w => w.Definitions),
                    nameof(StudyWord.CorrectStreak) => SortDescending ? query.OrderByDescending(w => w.CorrectStreak) : query.OrderBy(w => w.CorrectStreak),
                    nameof(StudyWord.MasteryTier) => SortDescending ? query.OrderByDescending(w => w.MasteryTier) : query.OrderBy(w => w.MasteryTier),
                    nameof(StudyWord.LastStudied) => SortDescending ? query.OrderByDescending(w => w.LastStudied) : query.OrderBy(w => w.LastStudied),
                    _ => query
                };
            }

            return query.ToList();
        }

        private void ToggleSort(string column)
        {
            if (SortColumn == column)
            {
                if (!SortDescending)
                {
                    SortDescending = true;
                }
                else
                {
                    SortColumn = null;
                    SortDescending = false;
                }
            }
            else
            {
                SortColumn = column;
                SortDescending = false;
            }
        }

        private MarkupString SortIndicator(string column)
        {
            if (SortColumn != column)
            {
                return (MarkupString)"";
            }

            var arrow = SortDescending ? "▼" : "▲";
            return (MarkupString)$"<span class='sort-icon'>{arrow}</span>";
        }
        #endregion
    }
}
