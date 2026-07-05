using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // Keyboard/controller focus management (M4 DoD: "full keyboard +
    // controller input"). Every UGUI Selectable already responds to Move
    // (arrow keys/d-pad/stick) and Submit (Enter/gamepad South) for free via
    // InputSystemUIInputModule - what's missing is *initial* focus, since
    // arrow-key navigation has nothing to move from until something is
    // selected. ClaimIfInvalid is the one rule every view uses: never steal
    // focus from a still-valid selection, so a player mid-navigation never
    // gets yanked around by an unrelated StateChanged firing.
    public static class UiFocus
    {
        public static bool IsSelectionValid()
        {
            GameObject current = EventSystem.current?.currentSelectedGameObject;
            if (current == null || !current.activeInHierarchy)
            {
                return false;
            }

            var selectable = current.GetComponent<Selectable>();
            return selectable == null || selectable.IsInteractable();
        }

        public static void ClaimIfInvalid(Selectable target)
        {
            if (IsSelectionValid() || target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            EventSystem.current?.SetSelectedGameObject(target.gameObject);
        }

        // Unconditional - for an explicit screen transition (e.g. Show()
        // opening a new panel), not a passive Render() recheck. The current
        // selection can still look "valid" by IsSelectionValid's own rules at
        // that exact moment (e.g. the previous screen's button, not yet
        // deactivated because *that* deactivation is driven by a separate
        // view's own StateChanged subscription, which hasn't fired yet) even
        // though it's no longer the one the player should land on.
        public static void Claim(Selectable target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            EventSystem.current?.SetSelectedGameObject(target.gameObject);
        }
    }
}
