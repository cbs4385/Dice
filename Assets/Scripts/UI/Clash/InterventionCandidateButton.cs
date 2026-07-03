using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI.Clash
{
    // The Clash equivalent of DieButton, but placeholder-only: a plain text label
    // and a click callback, no shape/animation - "targeting" is picking one of
    // these from a list of already-legal candidates, not clicking cells directly.
    // Final targeting UX/feel is a human-gated call (docs/clash.md SS5), not this
    // scaffold's job.
    public sealed class InterventionCandidateButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        public void Initialize(string description, Action onClicked)
        {
            _label.text = description;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked());
        }
    }
}
