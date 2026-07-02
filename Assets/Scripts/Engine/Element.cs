using System;
using System.Collections.Generic;

namespace Quintessence.Engine
{
    public enum Element
    {
        Fire,
        Earth,
        Air,
        Aether,
        Water,
    }

    public static class Sides
    {
        public static int Of(Element element) => element switch
        {
            Element.Fire => 4,
            Element.Earth => 6,
            Element.Air => 8,
            Element.Aether => 12,
            Element.Water => 20,
            _ => throw new ArgumentOutOfRangeException(nameof(element)),
        };
    }

    public static class Elements
    {
        public static readonly IReadOnlyList<Element> All = new[]
        {
            Element.Fire, Element.Earth, Element.Air, Element.Aether, Element.Water,
        };
    }
}
