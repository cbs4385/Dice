using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // A settings overlay reachable both before and during a match - unlike
    // every other overlay in this project (gated by game state), the open
    // button here is always active, since a player might want to turn on
    // reduced motion mid-session. Wires docs/gdd.md SS7's reduced-motion/
    // screen-shake accessibility toggles to AccessibilitySettings. A
    // deliberately plain, structural placeholder; real settings-screen feel
    // is human-gated.
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Toggle _reducedMotionToggle;
        [SerializeField] private Toggle _screenShakeToggle;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _openButton;

        private void Awake()
        {
            _openButton.onClick.AddListener(Show);
            _closeButton.onClick.AddListener(Close);
            _reducedMotionToggle.onValueChanged.AddListener(OnReducedMotionChanged);
            _screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
        }

        public void Show()
        {
            _reducedMotionToggle.SetIsOnWithoutNotify(AccessibilitySettings.ReducedMotion);
            _screenShakeToggle.SetIsOnWithoutNotify(AccessibilitySettings.ScreenShake);
            _root.SetActive(true);
            UiFocus.Claim(_reducedMotionToggle);
        }

        private void Close()
        {
            _root.SetActive(false);
            UiFocus.Claim(_openButton);
        }

        private void OnReducedMotionChanged(bool value) => AccessibilitySettings.ReducedMotion = value;

        private void OnScreenShakeChanged(bool value) => AccessibilitySettings.ScreenShake = value;
    }
}
