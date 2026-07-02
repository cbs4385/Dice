namespace Quintessence.Engine
{
    public abstract record Cell
    {
        // Private constructor closes the hierarchy to the three nested cases below.
        private Cell()
        {
        }

        public sealed record ElementCell(Element Element) : Cell;

        public sealed record BandCell(Band Band) : Cell;

        public sealed record WildCell : Cell;
    }
}
