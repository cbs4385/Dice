using System;
using System.Collections.Generic;
using UnityEngine;
using Quintessence.Engine;
using Quintessence.Game;

namespace Quintessence.UI
{
    public sealed class PoolView : MonoBehaviour
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
            // DiceRollController owns the pool dice exclusively while its physics
            // roll is playing; rendering the real, clickable buttons here too
            // early was the root cause of a real bug (see docs/progress.md) where
            // the old 2D placeholder animation got cancelled out from under
            // itself by this very method.
            if (_controller.State is null || _controller.IsRollInProgress)
            {
                return;
            }

            IReadOnlyList<Die> pool = _controller.State.CurrentPhase?.Pool ?? Array.Empty<Die>();

            for (int i = 0; i < pool.Count; i++)
            {
                DieButton button = i < _spawned.Count ? _spawned[i] : Spawn();
                int index = i;
                Die die = pool[i];
                bool selected = _controller.IsHumanTurn
                    && _controller.ArmedSource == DieSource.Pool
                    && _controller.ArmedIndex == index;

                button.gameObject.SetActive(true);
                button.Initialize(
                    die.Element,
                    die.Face,
                    DieColors.ForElement(die.Element),
                    () => _controller.ArmDie(DieSource.Pool, index, die));
                button.SetSelected(selected);
            }

            for (int i = pool.Count; i < _spawned.Count; i++)
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
