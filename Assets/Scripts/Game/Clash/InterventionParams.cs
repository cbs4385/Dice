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

        public sealed record Riptide(int FirmamentId) : InterventionParams;

        public sealed record Gust(int PoolIndex) : InterventionParams;

        public sealed record Petrify(int TargetPlayer, int Row, int Col) : InterventionParams;

        public sealed record EclipseNullifyBand(int TargetPlayer, int Row, int Col) : InterventionParams;

        public sealed record EclipseCancel : InterventionParams;
    }
}
