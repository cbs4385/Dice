namespace Quintessence.Game.Clash
{
    // ExpiresRound: the token is cleared once GameState.Round reaches this value
    // (i.e. it blocks the cell for petrifyDurationRounds rounds after being placed).
    public sealed record PetrifyToken(int Player, int Row, int Col, int ExpiresRound);
}
