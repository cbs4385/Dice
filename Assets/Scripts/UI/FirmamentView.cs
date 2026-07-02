using System.Collections.Generic;
using UnityEngine;
using Quintessence.Game;

namespace Quintessence.UI
{
    public sealed class FirmamentView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private DieButton _dieButtonPrefab;
        [SerializeField] private Transform _container;

        private readonly List<DieButton> _spawned = new();

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
        }

        private void Render()
        {
            if (_controller.State is null)
            {
                return;
            }

            IReadOnlyList<FirmamentDie> firmament = _controller.State.Firmament;

            for (int i = 0; i < firmament.Count; i++)
            {
                DieButton button = i < _spawned.Count ? _spawned[i] : Spawn();
                var entry = firmament[i];
                bool selected = _controller.IsHumanTurn
                    && _controller.ArmedSource == DieSource.Firmament
                    && _controller.ArmedIndex == entry.Id;

                button.gameObject.SetActive(true);
                button.Initialize(
                    $"{entry.Die.Element}\n{entry.Die.Face}",
                    DieColors.ForElement(entry.Die.Element),
                    () => _controller.ArmDie(DieSource.Firmament, entry.Id, entry.Die));
                button.SetSelected(selected);
            }

            for (int i = firmament.Count; i < _spawned.Count; i++)
            {
                _spawned[i].gameObject.SetActive(false);
            }
        }

        private DieButton Spawn()
        {
            var button = Instantiate(_dieButtonPrefab, _container);
            _spawned.Add(button);
            return button;
        }
    }
}
