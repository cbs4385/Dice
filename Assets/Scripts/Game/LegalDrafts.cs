using System;
using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    // Enumerates favor-free draft choices only. Random self-play (and the simplest
    // AI tier, M3) only needs "is there any legal, no-favor placement" - favor usage
    // is a deliberate choice a smarter policy opts into, not something to brute-force
    // enumerate here (Reroll in particular has no fixed outcome to enumerate against).
    public static class LegalDrafts
    {
        public static IReadOnlyList<DraftChoice> EnumerateSimple(GameState state)
        {
            if (state.CurrentPhase is not RoundPhase phase)
            {
                return Array.Empty<DraftChoice>();
            }

            int player = phase.PickOrder[phase.PickOrderIndex];
            var board = state.Players[player].Board;
            var choices = new List<DraftChoice>();

            void AddCandidates(DieSource source, int index, Die die)
            {
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        var placement = new Placement(r, c, die);
                        if (Legality.IsLegalPlacement(board, placement).IsLegal)
                        {
                            choices.Add(new DraftChoice(source, index, r, c));
                        }
                    }
                }
            }

            for (int i = 0; i < phase.Pool.Count; i++)
            {
                AddCandidates(DieSource.Pool, i, phase.Pool[i]);
            }

            foreach (var entry in state.Firmament)
            {
                AddCandidates(DieSource.Firmament, entry.Id, entry.Die);
            }

            return choices;
        }

        public static Die ResolveDie(GameState state, DraftChoice choice)
        {
            if (choice.Source == DieSource.Pool)
            {
                return ((RoundPhase)state.CurrentPhase!).Pool[choice.Index];
            }

            foreach (var entry in state.Firmament)
            {
                if (entry.Id == choice.Index)
                {
                    return entry.Die;
                }
            }

            throw new InvalidOperationException("Firmament id not found.");
        }
    }
}
