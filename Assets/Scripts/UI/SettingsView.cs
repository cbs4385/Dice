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
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private Toggle _reducedMotionToggle;
        [SerializeField] private Toggle _screenShakeToggle;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _openButton;

        // M5 DoD: "save/resume works" - hidden whenever no match is in
        // progress (GameSessionController.CanSaveCurrentMatch's own
        // conditions - not exposed publicly, so this checks State directly;
        // SaveAndExitToMenu itself still no-ops safely either way).
        [SerializeField] private Button _saveAndExitButton;

        private void Awake()
        {
            _openButton.onClick.AddListener(Show);
            _closeButton.onClick.AddListener(Close);
            _reducedMotionToggle.onValueChanged.AddListener(OnReducedMotionChanged);
            _screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
            _saveAndExitButton.onClick.AddListener(OnSaveAndExitClicked);
        }

        public void Show()
        {
            _reducedMotionToggle.SetIsOnWithoutNotify(AccessibilitySettings.ReducedMotion);
            _screenShakeToggle.SetIsOnWithoutNotify(AccessibilitySettings.ScreenShake);
            _saveAndExitButton.gameObject.SetActive(_controller.State is not null);
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

        private void OnSaveAndExitClicked()
        {
            _controller.SaveAndExitToMenu();
            Close();
        }
    }
}
