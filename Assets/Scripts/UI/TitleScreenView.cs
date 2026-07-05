using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    // The very first screen shown at launch - Play hands off to
    // ModeSelectView.Show(); Quit exits the application (Editor-safe, so
    // it's actually testable live via UnityMCP, not just in a real build).
    // A deliberately plain, structural placeholder; real title-screen feel
    // (art, branding) is human-gated.
    public sealed class TitleScreenView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private ModeSelectView _modeSelectView;

        // Tracks whether this screen has been dismissed (Play clicked) - only
        // re-armed on the specific "was in a match, now isn't" edge
        // (SaveAndExitToMenu), not on every render where State merely
        // happens to be null - State stays null through the *entire*
        // pre-match flow (title, mode-select, player-setup), the same
        // problem ModeSelectView's own old auto-show logic had.
        private bool _dismissed;
        private bool _wasInMatch;

        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnEnable()
        {
            _controller.StateChanged += OnStateChanged;
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged()
        {
            bool inMatch = _controller.State is not null;
            if (_wasInMatch && !inMatch)
            {
                _dismissed = false;
            }

            _wasInMatch = inMatch;
            Render();
        }

        private void Render()
        {
            bool visible = !_wasInMatch && !_dismissed;
            _root.SetActive(visible);

            if (visible)
            {
                UiFocus.ClaimIfInvalid(_playButton);
            }
        }

        private void OnPlayClicked()
        {
            _dismissed = true;
            _root.SetActive(false);
            _modeSelectView.Show();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
