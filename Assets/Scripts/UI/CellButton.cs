using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    public sealed class CellButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;

        public int Row { get; private set; }

        public int Col { get; private set; }

        public void Initialize(int row, int col, Action<int, int> onClicked)
        {
            Row = row;
            Col = col;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked(Row, Col));
        }

        public void SetLabel(string text) => _label.text = text;

        public void SetInteractable(bool interactable) => _button.interactable = interactable;

        public void SetColor(Color color) => _background.color = color;
    }
}
