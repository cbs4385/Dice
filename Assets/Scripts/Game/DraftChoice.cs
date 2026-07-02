namespace Quintessence.Game
{
    public enum DieSource
    {
        Pool,
        Firmament,
    }

    // Row/Col are given rather than a full Placement because, for Reroll, the final
    // face (and therefore the die that ends up on the board) is only known after the
    // reducer resolves it against the rng - the caller cannot supply it up front.
    public sealed record DraftChoice(DieSource Source, int Index, int Row, int Col, FavorAction? Favor = null);

    public abstract record FavorAction
    {
        private FavorAction()
        {
        }

        public sealed record Adjust(int Delta) : FavorAction;

        public sealed record Reroll : FavorAction;

        public sealed record Defy : FavorAction;
    }
}
