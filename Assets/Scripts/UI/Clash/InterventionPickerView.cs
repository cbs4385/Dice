using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Quintessence.Game;
using Quintessence.Game.Clash;

namespace Quintessence.UI.Clash
{
    // "Targeting" is picking from a list of already-legal candidates
    // (ClashLegalMoves.EnumerateDeclarations), not clicking cells directly - a
    // deliberately plain, structural placeholder. Real targeting UX (clicking a
    // cell, dragging, per-kind flows) is a human-gated feel decision.
    public sealed class InterventionPickerView : MonoBehaviour
    {
        [SerializeField] private GameSessionController _controller;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _openButton;
        [SerializeField] private GameObject _candidateListRoot;
        [SerializeField] private InterventionCandidateButton _candidatePrefab;
        [SerializeField] private Transform _candidateContainer;

        private readonly List<InterventionCandidateButton> _spawned = new();
        private bool _listOpen;

        private void OnEnable()
        {
            _controller.StateChanged += Render;
            _openButton.onClick.AddListener(ToggleList);
            Render();
        }

        private void OnDisable()
        {
            _controller.StateChanged -= Render;
            _openButton.onClick.RemoveListener(ToggleList);
        }

        private void ToggleList()
        {
            _listOpen = !_listOpen;
            Render();
        }

        private void Render()
        {
            if (_controller.State is null)
            {
                return;
            }

            var state = _controller.State;
            IReadOnlyList<(InterventionKind Kind, InterventionParams Params)> candidates =
                state.Clash is not null && _controller.IsHumanTurn
                    ? ClashLegalMoves.EnumerateDeclarations(state, GameReducer.CurrentPlayer(state))
                    : System.Array.Empty<(InterventionKind, InterventionParams)>();

            bool canDeclare = candidates.Count > 0;
            _root.SetActive(canDeclare);
            if (!canDeclare)
            {
                _listOpen = false;
            }

            _candidateListRoot.SetActive(_listOpen);
            if (!_listOpen)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var button = i < _spawned.Count ? _spawned[i] : Spawn();
                var candidate = candidates[i];
                button.gameObject.SetActive(true);
                button.Initialize(Describe(candidate.Params), () =>
                {
                    _listOpen = false;
                    _controller.DeclareIntervention(candidate.Kind, candidate.Params);
                });
            }

            for (int i = candidates.Count; i < _spawned.Count; i++)
            {
                _spawned[i].gameObject.SetActive(false);
            }
        }

        private InterventionCandidateButton Spawn()
        {
            var button = Instantiate(_candidatePrefab, _candidateContainer);
            _spawned.Add(button);
            return button;
        }

        private static string Describe(InterventionParams parameters) => parameters switch
        {
            InterventionParams.Scorch s => $"Scorch P{s.TargetPlayer + 1}'s die at ({s.Row},{s.Col}) by {s.Pips}",
            InterventionParams.Riptide r => $"Riptide: claim Firmament die #{r.FirmamentId} onto ({r.Row},{r.Col})",
            InterventionParams.Gust g => $"Gust: draft pool die #{g.PoolIndex} onto ({g.Row},{g.Col})",
            InterventionParams.Petrify p => $"Petrify P{p.TargetPlayer + 1}'s cell ({p.Row},{p.Col})",
            InterventionParams.EclipseNullifyBand e => $"Eclipse: nullify P{e.TargetPlayer + 1}'s cell ({e.Row},{e.Col})",
            InterventionParams.EclipseCancel => "Eclipse: cancel",
            _ => "Unknown intervention",
        };
    }
}
