namespace Quintessence.Game.Clash
{
    // Closed hierarchy, same pattern as Quintessence.Engine.Cell / Quintessence.Game.FavorAction.
    // Riptide has no target player: the Firmament is a single shared pool (per the
    // rulebook and the already-tested engine), not per-player as docs/clash.md SS2.2
    // assumes - reinterpreted as "steal one die from the shared Firmament" per the
    // supervisor's explicit direction (see docs/progress.md).
    public abstract record InterventionParams
    {
        private InterventionParams()
        {
        }

        public sealed record Scorch(int TargetPlayer, int Row, int Col, int Pips) : InterventionParams;

        // Shared-pool reinterpretation (see docs/progress.md): "steal" means the
        // actor immediately drafts the chosen Firmament die onto their own board,
        // out of turn - the same shape as Gust, sourcing the Firmament instead of
        // the pool, rather than a bank-to-bank transfer that no longer makes sense
        // once there is only one Firmament.
        public sealed record Riptide(int FirmamentId, int Row, int Col) : InterventionParams;

        public sealed record Gust(int PoolIndex, int Row, int Col) : InterventionParams;

        public sealed record Petrify(int TargetPlayer, int Row, int Col) : InterventionParams;

        public sealed record EclipseNullifyBand(int TargetPlayer, int Row, int Col) : InterventionParams;

        public sealed record EclipseCancel : InterventionParams;
    }
}
