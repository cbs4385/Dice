using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game.Clash
{
    public static class ClashSetup
    {
        private static readonly InterventionKind[] AllKinds =
        {
            InterventionKind.Scorch, InterventionKind.Riptide, InterventionKind.Gust,
            InterventionKind.Petrify, InterventionKind.Eclipse,
        };

        public static ClashState Deal(int playerCount, ClashConfig config, IRng rng)
        {
            var order = GameSetup.Shuffle(new[] { 0, 1, 2, 3, 4 }, rng);
            var dealt = new List<InterventionKind>(config.InterventionsPerMatch);
            for (int i = 0; i < config.InterventionsPerMatch && i < AllKinds.Length; i++)
            {
                dealt.Add(AllKinds[order[i]]);
            }

            var storm = new List<int>(new int[playerCount]);

            return new ClashState(
                Config: config,
                Storm: storm,
                InterventionsAvailable: dealt,
                PetrifyTokens: new List<PetrifyToken>(),
                Pending: null,
                InterventionLog: new List<ClashLogEntry>(),
                NullifiedBandCells: new List<NullifiedCell>());
        }
    }
}
