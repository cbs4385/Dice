namespace Quintessence.Engine
{
    public enum IllegalPlacementReason
    {
        CellOccupied,
        ElementCellMismatch,
        NotConnected,
        ElementAdjacencyViolation,
    }

    public sealed record LegalityResult(bool IsLegal, IllegalPlacementReason? Reason)
    {
        public static readonly LegalityResult Legal = new(true, null);

        public static LegalityResult Illegal(IllegalPlacementReason reason) => new(false, reason);
    }

    public static class Legality
    {
        public static LegalityResult IsLegalPlacement(Board board, Placement placement, bool defyElementAdjacency = false)
        {
            if (board.DieAt(placement.Row, placement.Col) is not null)
            {
                return LegalityResult.Illegal(IllegalPlacementReason.CellOccupied);
            }

            if (board.CellAt(placement.Row, placement.Col) is Cell.ElementCell elementCell
                && elementCell.Element != placement.Die.Element)
            {
                return LegalityResult.Illegal(IllegalPlacementReason.ElementCellMismatch);
            }

            if (board.HasAnyDie() && !board.IsAdjacentToAnyDie(placement.Row, placement.Col))
            {
                return LegalityResult.Illegal(IllegalPlacementReason.NotConnected);
            }

            if (!defyElementAdjacency
                && board.HasOrthogonalNeighborOfElement(placement.Row, placement.Col, placement.Die.Element))
            {
                return LegalityResult.Illegal(IllegalPlacementReason.ElementAdjacencyViolation);
            }

            return LegalityResult.Legal;
        }
    }
}
