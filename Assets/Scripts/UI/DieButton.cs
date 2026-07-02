using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Quintessence.Engine;

namespace Quintessence.UI
{
    public sealed class DieButton : MonoBehaviour
    {
        private static readonly Color SelectedColor = Color.yellow;

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;

        private Color _baseColor;
        private Coroutine _rollCoroutine;

        public void Initialize(Element element, int face, Color color, Action onClicked)
        {
            if (_rollCoroutine is not null)
            {
                StopCoroutine(_rollCoroutine);
                _rollCoroutine = null;
            }

            ApplyVisual(element, face, color);
            Rewire(onClicked);
        }

        // Purely cosmetic: the true face was already determined by the (already
        // tested, deterministic) reducer before this is called. Cycling through
        // plausible-looking random faces here has no effect on game state - it is
        // a placeholder for the "feel" of a rolling die (GDD SS6), not a finished
        // animation; timing/drama is a human-gated call, not this method's job.
        public void PlayRollAnimation(Element element, int finalFace, Color color, float duration, Action onClicked)
        {
            if (_rollCoroutine is not null)
            {
                StopCoroutine(_rollCoroutine);
            }

            _rollCoroutine = StartCoroutine(RollThenSettle(element, finalFace, color, duration, onClicked));
        }

        public void SetSelected(bool selected) => _background.color = selected ? SelectedColor : _baseColor;

        private IEnumerator RollThenSettle(Element element, int finalFace, Color color, float duration, Action onClicked)
        {
            _background.sprite = PlatonicShapeSprites.For(element);
            _background.color = color;
            _baseColor = color;
            _button.interactable = false;

            var cosmeticRng = new System.Random();
            int maxFace = Sides.Of(element);
            float elapsed = 0f;
            const float tickSeconds = 0.06f;

            while (elapsed < duration)
            {
                _label.text = $"{element}\n{cosmeticRng.Next(1, maxFace + 1)}";
                yield return new WaitForSeconds(tickSeconds);
                elapsed += tickSeconds;
            }

            ApplyVisual(element, finalFace, color);
            Rewire(onClicked);
            _rollCoroutine = null;
        }

        private void ApplyVisual(Element element, int face, Color color)
        {
            _label.text = $"{element}\n{face}";
            _baseColor = color;
            _background.sprite = PlatonicShapeSprites.For(element);
            _background.color = color;
        }

        private void Rewire(Action onClicked)
        {
            _button.interactable = true;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked());
        }
    }
}
