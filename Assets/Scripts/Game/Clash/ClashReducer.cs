using System.Collections.Generic;

namespace Quintessence.Game.Clash
{
    public static class ClashReducer
    {
        // Called from GameReducer.ApplyDraft only when state.Clash is not null and
        // the placement attuned a band cell - a small, explicitly-gated touch-point
        // on the core reducer, never a branch inside shared placement logic.
        public static ClashState ChargeStormOnAttune(ClashState clash, int player)
        {
            var storm = new List<int>(clash.Storm);
            storm[player] = System.Math.Min(storm[player] + clash.Config.StormPerAttune, clash.Config.StormCap);
            return clash with { Storm = storm };
        }
    }
}
