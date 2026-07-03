using System;
using System.Collections.Generic;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game
{
    public static class GameSetup
    {
        private static readonly Func<Board>[] BoardFactories =
        {
            BoardLayouts.Ashfall, BoardLayouts.Tidewater, BoardLayouts.Zephyr, BoardLayouts.Bedrock,
        };

        // clashConfig defaults to null: every existing caller is unaffected and
        // State.Clash stays null, exactly like every other mode (see AGENTS.md /
        // docs/clash.md SS0 - non-Clash modes must stay byte-identical).
        public static GameState NewGame(int playerCount, IRng rng, int startingFavor = 3, ClashConfig? clashConfig = null)
        {
            if (playerCount < 2 || playerCount > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "Quintessence supports 2-4 players.");
            }

            var boardOrder = Shuffle(new[] { 0, 1, 2, 3 }, rng);
            var elementOrder = Shuffle(new[] { 0, 1, 2, 3, 4 }, rng);
            var objective = (PublicObjective)rng.NextInt(6);

            var players = new List<PlayerState>(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                var board = BoardFactories[boardOrder[i]]();
                var privateElement = Elements.All[elementOrder[i]];
                players.Add(new PlayerState(board, startingFavor, privateElement));
            }

            ClashState? clashState = clashConfig is null ? null : ClashSetup.Deal(playerCount, clashConfig, rng);

            return new GameState(
                Round: 1,
                StartPlayerIndex: 0,
                Players: players,
                Bag: Bag.Default,
                Firmament: new List<FirmamentDie>(),
                Objective: objective,
                CurrentPhase: null,
                NextFirmamentId: 0,
                IsGameOver: false,
                Clash: clashState);
        }

        internal static int[] Shuffle(int[] values, IRng rng)
        {
            var array = (int[])values.Clone();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }

            return array;
        }
    }
}
