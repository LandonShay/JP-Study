﻿using JP_Dictionary.Models;
using JP_Dictionary.Shared;
using JP_Dictionary.Services;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Pages
{
    public partial class CreateCard
    {
        #region Injections
#nullable disable
        [Inject] public UserState User { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public ToastService ToastService { get; set; }
#nullable enable
        #endregion

        private bool ShowConfirmation { get; set; } = false;
        private string ConflictDeckName { get; set; } = string.Empty;
        private StudyWord PendingWord { get; set; } = new();

        private string Japanese { get; set; } = string.Empty;
        private string Romaji { get; set; } = string.Empty;
        private string Definitions { get; set; } = string.Empty;
        private bool UnlockImmediately { get; set; }

        private void Create()
        {
            if (!string.IsNullOrEmpty(Japanese) &&
                !string.IsNullOrEmpty(Romaji) &&
                !string.IsNullOrEmpty(Definitions))
            {
                PendingWord = new StudyWord
                {
                    Word = Japanese,
                    Romaji = Romaji,
                    Definitions = Definitions,
                    Id = Guid.NewGuid().ToString(),
                    LastStudied = DateTime.MinValue,
                    CorrectStreak = 0,
                    Unlocked = UnlockImmediately
                };

                foreach (var deck in User.Profile!.Decks)
                {
                    var checkDeck = DeckMethods.LoadDeck(User.Profile, deck.Name);

                    if (checkDeck.Any(x => x.Word == Japanese))
                    {
                        ShowConfirmation = true;
                        ConflictDeckName = deck.Name;

                        return;
                    }
                }

                AddWordToDeck();
            }
        }

        private void AddWordToDeck()
        {
            var deck = DeckMethods.LoadDeck(User.Profile!, User.SelectedDeck!.Name);
            deck.Add(PendingWord);

            DeckMethods.UpdateDeck(deck, User.Profile!.Name, User.SelectedDeck!.Name);
            ToastService.ShowSuccess($"{PendingWord.Word} added to {User.SelectedDeck.Name} deck");

            Japanese = string.Empty;
            Romaji = string.Empty;
            Definitions = string.Empty;

            PendingWord = new();
            ShowConfirmation = false;
        }
    }
}
