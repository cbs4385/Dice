using System;
using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game
{
    public static class GameSetup
    {
        private static readonly Func<Board>[] BoardFactories =
        {
            BoardLayouts.Ashfall, BoardLayouts.Tidewater, BoardLayouts.Zephyr, BoardLayouts.Bedrock,
        };

        public static GameState NewGame(int playerCount, IRng rng, int startingFavor = 3)
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

            return new GameState(
                Round: 1,
                StartPlayerIndex: 0,
                Players: players,
                Bag: Bag.Default,
                Firmament: new List<FirmamentDie>(),
                Objective: objective,
                CurrentPhase: null,
                NextFirmamentId: 0,
                IsGameOver: false);
        }

        private static int[] Shuffle(int[] values, IRng rng)
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
