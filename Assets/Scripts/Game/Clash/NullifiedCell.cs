namespace Quintessence.Game.Clash
{
    // Eclipse's nullify-a-band-cell effect (docs/clash.md SS2.2): the cell still
    // holds its die and still counts for adjacency/objectives, it just contributes
    // zero band-cell points at final scoring - see ClashScoring.
    public sealed record NullifiedCell(int Player, int Row, int Col);
}
