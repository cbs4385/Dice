using System;
using System.Collections.Generic;
using System.Linq;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game
{
    public static class GameReducer
    {
        public const int TotalRounds = 6;

        private static readonly IDraftOrderStrategy DraftOrder = new SnakeDraftOrderStrategy();

        public static GameState StartRound(GameState state, IRng rng)
        {
            if (state.IsGameOver)
            {
                throw new InvalidOperationException("Cannot start a round after the game is over.");
            }

            if (state.CurrentPhase is not null)
            {
                throw new InvalidOperationException("Cannot start a round while a phase is in progress.");
            }

            int playerCount = state.Players.Count;
            int poolSize = (2 * playerCount) + 1;
            var (pool, bag) = BagOps.DrawRoll(state.Bag, rng, poolSize);
            var pickOrder = DraftOrder.PickOrder(state.StartPlayerIndex, playerCount, pickNumber: 1);

            return state with
            {
                Bag = bag,
                CurrentPhase = new RoundPhase(1, pickOrder, 0, pool),
            };
        }

        public static int CurrentPlayer(GameState state)
        {
            var phase = RequirePhase(state);
            return phase.PickOrder[phase.PickOrderIndex];
        }

        public static GameState ApplyDraft(GameState state, DraftChoice choice, IRng rng)
        {
            var phase = RequirePhase(state);
            int player = phase.PickOrder[phase.PickOrderIndex];
            var playerState = state.Players[player];

            if (choice.Favor is not null && playerState.FavorRemaining <= 0)
            {
                throw new InvalidOperationException("No favor tokens remaining.");
            }

            Die draftedDie;
            IReadOnlyList<Die> newPool = phase.Pool;
            IReadOnlyList<FirmamentDie> newFirmament = state.Firmament;

            if (choice.Source == DieSource.Pool)
            {
                if (choice.Index < 0 || choice.Index >= phase.Pool.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(choice), choice.Index, "Pool index out of range.");
                }

                draftedDie = phase.Pool[choice.Index];
                var remainingPool = phase.Pool.ToList();
                remainingPool.RemoveAt(choice.Index);
                newPool = remainingPool;
            }
            else
            {
                var entry = state.Firmament.FirstOrDefault(f => f.Id == choice.Index);
                if (entry is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(choice), choice.Index, "Firmament id not found.");
                }

                draftedDie = entry.Die;
                newFirmament = state.Firmament.Where(f => f.Id != choice.Index).ToList();
            }

            Die finalDie = choice.Favor switch
            {
                FavorAction.Adjust adjust => Favor.Adjust(draftedDie, adjust.Delta),
                FavorAction.Reroll => Favor.Reroll(draftedDie, rng),
                _ => draftedDie,
            };

            var placement = new Placement(choice.Row, choice.Col, finalDie);
            bool defy = choice.Favor is FavorAction.Defy;
            var legality = Legality.IsLegalPlacement(playerState.Board, placement, defyElementAdjacency: defy);
            if (!legality.IsLegal)
            {
                throw new InvalidOperationException($"Illegal placement: {legality.Reason}");
            }

            int favorSpent = choice.Favor is not null ? 1 : 0;

            var updatedPlayers = state.Players.ToList();
            updatedPlayers[player] = playerState with
            {
                Board = playerState.Board.WithPlacement(placement),
                FavorRemaining = playerState.FavorRemaining - favorSpent,
            };

            // Gated Clash touch-point: no-op whenever state.Clash is null, so this
            // can never affect non-Clash play (see ClashRegressionTests).
            var clashState = state.Clash;
            if (clashState is not null
                && playerState.Board.CellAt(choice.Row, choice.Col) is Cell.BandCell bandCell
                && Bands.Of(finalDie.Face) == bandCell.Band)
            {
                clashState = ClashReducer.ChargeStormOnAttune(clashState, player);
            }

            var updatedState = state with
            {
                Players = updatedPlayers,
                Firmament = newFirmament,
                CurrentPhase = phase with { Pool = newPool },
                Clash = clashState,
            };

            return AdvanceTurn(updatedState);
        }

        public static GameState ApplyForfeit(GameState state)
        {
            RequirePhase(state);
            return AdvanceTurn(state);
        }

        private static GameState AdvanceTurn(GameState state)
        {
            var phase = RequirePhase(state);
            int nextIndex = phase.PickOrderIndex + 1;

            if (nextIndex < phase.PickOrder.Count)
            {
                return state with { CurrentPhase = phase with { PickOrderIndex = nextIndex } };
            }

            if (phase.PickNumber == 1)
            {
                var pickOrder2 = DraftOrder.PickOrder(state.StartPlayerIndex, state.Players.Count, pickNumber: 2);
                return state with { CurrentPhase = new RoundPhase(2, pickOrder2, 0, phase.Pool) };
            }

            // Round complete: any dice left in the pool move to the Firmament.
            int nextFirmamentId = state.NextFirmamentId;
            var firmament = state.Firmament.ToList();
            foreach (var die in phase.Pool)
            {
                firmament.Add(new FirmamentDie(nextFirmamentId, die));
                nextFirmamentId++;
            }

            int nextRound = state.Round + 1;
            bool gameOver = nextRound > TotalRounds;

            return state with
            {
                Round = gameOver ? state.Round : nextRound,
                StartPlayerIndex = (state.StartPlayerIndex + 1) % state.Players.Count,
                Firmament = firmament,
                NextFirmamentId = nextFirmamentId,
                CurrentPhase = null,
                IsGameOver = gameOver,
            };
        }

        private static RoundPhase RequirePhase(GameState state)
        {
            if (state.CurrentPhase is not RoundPhase phase)
            {
                throw new InvalidOperationException("No phase in progress; call StartRound first.");
            }

            return phase;
        }
    }
}
