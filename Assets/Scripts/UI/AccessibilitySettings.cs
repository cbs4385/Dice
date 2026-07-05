using System;
using UnityEngine;

namespace Quintessence.UI
{
    // Cross-cutting UI-layer preference (same category as the existing
    // static DieColors/PlatonicShapeSprites helpers, not game logic) - so
    // PlayerPrefs here doesn't touch the Engine/Game purity boundary
    // (AGENTS.md's noEngineReferences guard only applies to those two
    // assemblies). Backs docs/gdd.md SS7's "reduced-motion and screen-shake
    // toggles" accessibility requirement.
    public static class AccessibilitySettings
    {
        private const string ReducedMotionKey = "Accessibility.ReducedMotion";
        private const string ScreenShakeKey = "Accessibility.ScreenShake";

        public static event Action Changed;

        public static bool ReducedMotion
        {
            get => PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(ReducedMotionKey, value ? 1 : 0);
                Changed?.Invoke();
            }
        }

        // Persisted for a future shake effect to check - no shake effect
        // exists in the codebase yet, so this toggle has nothing to actually
        // change today. Not disclosed as "working" beyond persisting.
        public static bool ScreenShake
        {
            get => PlayerPrefs.GetInt(ScreenShakeKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(ScreenShakeKey, value ? 1 : 0);
                Changed?.Invoke();
            }
        }
    }
}
