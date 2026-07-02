using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    public sealed class DieButton : MonoBehaviour
    {
        private static readonly Color SelectedColor = Color.yellow;

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;

        private Color _baseColor;

        public void Initialize(string label, Color color, Action onClicked)
        {
            _label.text = label;
            _baseColor = color;
            _background.color = color;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked());
        }

        public void SetSelected(bool selected) => _background.color = selected ? SelectedColor : _baseColor;
    }
}
