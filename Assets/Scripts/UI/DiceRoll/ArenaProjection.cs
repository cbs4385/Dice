using UnityEngine;

namespace Quintessence.UI.DiceRoll
{
    // Maps a UI element's on-screen world position into world space on a
    // horizontal plane, via an orthographic camera whose RenderTexture
    // overlay covers the full screen 1:1 - a screen point maps to a viewport
    // fraction *of the screen*, and from there to a world point via the
    // camera. Deliberately NOT camera.ScreenToViewportPoint: that divides by
    // the camera's own pixelWidth/pixelHeight (the RenderTexture's
    // resolution), not the actual screen/canvas size the UI position was
    // measured in - a real bug found live in DiceRollController before this
    // was extracted here (dice flew to screen center instead of the tray).
    public static class ArenaProjection
    {
        // Full-screen case (DiceRollController's own use: its RawImage overlay
        // covers the whole screen 1:1, so a screen point maps to a viewport
        // fraction of the entire screen).
        public static Vector3 ScreenToWorldOnPlane(Camera camera, Vector3 worldPointOnUi, float planeHeight) =>
            ScreenToWorldOnPlane(camera, worldPointOnUi, planeHeight, new Rect(0f, 0f, Screen.width, Screen.height));

        // General case: screenViewport is the on-screen pixel rect the
        // destination RawImage actually occupies - needed whenever that
        // RawImage is NOT full-screen (e.g. FirmamentView's small tray
        // strip), since a screen point must map to a viewport fraction *of
        // that rect*, not of the whole screen, to land in the right spot.
        public static Vector3 ScreenToWorldOnPlane(Camera camera, Vector3 worldPointOnUi, float planeHeight, Rect screenViewport)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPointOnUi);
            float u = (screenPos.x - screenViewport.x) / screenViewport.width;
            float v = (screenPos.y - screenViewport.y) / screenViewport.height;
            float depth = Mathf.Abs(camera.transform.position.y - planeHeight);
            Vector3 worldPos = camera.ViewportToWorldPoint(new Vector3(u, v, depth));
            return new Vector3(worldPos.x, planeHeight, worldPos.z);
        }

        // The on-screen pixel rect a RectTransform currently occupies -
        // FirmamentView uses this to get its RawImage's actual screen rect
        // for the general overload above.
        public static Rect GetScreenRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector3 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
    }
}
