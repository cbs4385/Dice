using System;
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

        public Button Button => _button;

        public void Initialize(Element element, int face, Color color, Action onClicked)
        {
            _label.text = $"{element}\n{face}";
            _baseColor = color;
            _background.sprite = PlatonicShapeSprites.For(element);
            _background.color = color;
            _button.interactable = true;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked());
        }

        public void SetSelected(bool selected) => _background.color = selected ? SelectedColor : _baseColor;

        // Turns this button into an invisible click-catcher - its RectTransform
        // and Button still work exactly as before, just with nothing drawn, so
        // a real visual (e.g. a 3D die rendered behind it) can show through
        // instead. Used by FirmamentView once its 3D die display took over
        // the visible presentation.
        public void SetChromeVisible(bool visible)
        {
            _label.text = visible ? _label.text : string.Empty;
            _background.color = visible ? _baseColor : new Color(0f, 0f, 0f, 0f);
        }
    }
}
