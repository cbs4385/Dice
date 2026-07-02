namespace Quintessence.Engine
{
    public static class BoardLayouts
    {
        public static Board Ashfall() => Board.Empty(new Cell[,]
        {
            { new Cell.ElementCell(Element.Fire), new Cell.BandCell(Band.Low), new Cell.WildCell(), new Cell.BandCell(Band.High) },
            { new Cell.WildCell(), new Cell.ElementCell(Element.Air), new Cell.BandCell(Band.Celestial), new Cell.WildCell() },
            { new Cell.BandCell(Band.Mid), new Cell.WildCell(), new Cell.ElementCell(Element.Earth), new Cell.ElementCell(Element.Water) },
        });

        public static Board Tidewater() => Board.Empty(new Cell[,]
        {
            { new Cell.ElementCell(Element.Water), new Cell.WildCell(), new Cell.BandCell(Band.Mid), new Cell.ElementCell(Element.Aether) },
            { new Cell.BandCell(Band.Celestial), new Cell.ElementCell(Element.Earth), new Cell.WildCell(), new Cell.BandCell(Band.Low) },
            { new Cell.WildCell(), new Cell.BandCell(Band.High), new Cell.ElementCell(Element.Air), new Cell.WildCell() },
        });

        public static Board Zephyr() => Board.Empty(new Cell[,]
        {
            { new Cell.WildCell(), new Cell.ElementCell(Element.Air), new Cell.BandCell(Band.High), new Cell.WildCell() },
            { new Cell.BandCell(Band.Low), new Cell.BandCell(Band.Celestial), new Cell.WildCell(), new Cell.ElementCell(Element.Fire) },
            { new Cell.ElementCell(Element.Aether), new Cell.WildCell(), new Cell.BandCell(Band.Mid), new Cell.ElementCell(Element.Water) },
        });

        public static Board Bedrock() => Board.Empty(new Cell[,]
        {
            { new Cell.BandCell(Band.High), new Cell.ElementCell(Element.Earth), new Cell.WildCell(), new Cell.BandCell(Band.Celestial) },
            { new Cell.WildCell(), new Cell.BandCell(Band.Low), new Cell.ElementCell(Element.Fire), new Cell.WildCell() },
            { new Cell.ElementCell(Element.Aether), new Cell.WildCell(), new Cell.BandCell(Band.Mid), new Cell.ElementCell(Element.Air) },
        });
    }
}
