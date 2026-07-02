using System.Collections.Generic;
using UnityEngine;
using Quintessence.Engine;

namespace Quintessence.UI
{
    // Distinct silhouette per element (not just color) is a GDD SS7 accessibility
    // requirement, not just aesthetics. Uses the common tabletop-dice convention:
    // triangle=d4, square=d6, diamond=d8, pentagon=d12, higher-order polygon=d20.
    // Placeholder art (flat procedural polygons) - final material/style per element
    // (GDD SS6/SS12.5) is a separate, still-open, human-gated decision.
    internal static class PlatonicShapeSprites
    {
        private const int TextureSize = 128;

        private static readonly Dictionary<Element, Sprite> Cache = new();

        public static Sprite For(Element element)
        {
            if (Cache.TryGetValue(element, out var cached))
            {
                return cached;
            }

            var sprite = CreatePolygonSprite(SidesFor(element), RotationDegreesFor(element));
            Cache[element] = sprite;
            return sprite;
        }

        private static int SidesFor(Element element) => element switch
        {
            Element.Fire => 3,
            Element.Earth => 4,
            Element.Air => 4,
            Element.Aether => 5,
            Element.Water => 10,
            _ => 6,
        };

        // Air's diamond is a square rotated 45 degrees, visually distinct from Earth's upright square.
        private static float RotationDegreesFor(Element element) => element == Element.Earth ? 45f : -90f;

        private static Sprite CreatePolygonSprite(int sides, float rotationDegrees)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var center = new Vector2(TextureSize / 2f, TextureSize / 2f);
            float radius = (TextureSize / 2f) - 4f;
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            var pixels = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = IsInsideRegularPolygon(point, center, radius, sides, rotation);
                    pixels[(y * TextureSize) + x] = inside ? Color.white : new Color(0f, 0f, 0f, 0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f));
        }

        // Standard point-in-regular-polygon test: compare the point's distance from
        // center against the polygon's radius at that angle (the inradius/circumradius
        // interpolation for a regular polygon face).
        private static bool IsInsideRegularPolygon(Vector2 point, Vector2 center, float circumradius, int sides, float rotation)
        {
            Vector2 delta = point - center;
            if (delta.magnitude > circumradius)
            {
                return false;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) - rotation;
            float segment = (2f * Mathf.PI) / sides;
            float theta = Mathf.Repeat(angle, segment) - (segment / 2f);
            float edgeDistance = circumradius * Mathf.Cos(segment / 2f) / Mathf.Cos(theta);
            return delta.magnitude <= edgeDistance;
        }
    }
}
