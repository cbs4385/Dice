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
            _controller.RoundStarted += OnRoundStarted;
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
            _controller.RoundStarted -= OnRoundStarted;
        }

        // Plays the roll animation exactly once per round-start, for the dice that
        // were just drawn - not on every subsequent re-render (e.g. after a draft).
        private void OnRoundStarted(IReadOnlyList<Die> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                DieButton button = i < _spawned.Count ? _spawned[i] : Spawn();
                int index = i;
                Die die = pool[i];
                button.gameObject.SetActive(true);
                button.PlayRollAnimation(
                    die.Element,
                    die.Face,
                    DieColors.ForElement(die.Element),
                    GameSessionController.RollAnimationSeconds,
                    () => _controller.ArmDie(DieSource.Pool, index, die));
            }

            for (int i = pool.Count; i < _spawned.Count; i++)
            {
                _spawned[i].gameObject.SetActive(false);
            }
        }

        private void Render()
        {
            if (_controller.State is null)
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
