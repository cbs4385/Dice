using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI
{
    // Placeholder colors only - final art direction (GDD SS12.5) is an open,
    // human-gated decision. Distinct hue per element is a functional stand-in for
    // the eventual distinct-silhouette accessibility requirement (GDD SS7), not a
    // claim that color-only encoding is the shipped design.
    internal static class DieColors
    {
        public static Color ForElement(Element element) => element switch
        {
            Element.Fire => new Color(0.80f, 0.30f, 0.20f),
            Element.Earth => new Color(0.40f, 0.60f, 0.30f),
            Element.Air => new Color(0.70f, 0.80f, 0.90f),
            Element.Aether => new Color(0.50f, 0.30f, 0.70f),
            Element.Water => new Color(0.20f, 0.50f, 0.80f),
            _ => Color.gray,
        };
    }
}
