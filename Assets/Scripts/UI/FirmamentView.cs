using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Quintessence.Engine;
using Quintessence.Game;
using Quintessence.UI.DiceRoll;

namespace Quintessence.UI
{
    public sealed class FirmamentView : MonoBehaviour
    {
        // Where in FirmamentArena's world space the die instances sit -
        // arbitrary since this mini-arena is isolated from every other
        // camera's view (its own dedicated FirmamentDice3D layer), not a
        // shared world coordinate.
        private const float DiePlaneHeight = 0f;

        [SerializeField] private GameSessionController _controller;
        [SerializeField] private DieButton _dieButtonPrefab;
        [SerializeField] private Transform _container;

        // The persistent 3D display behind the (now invisible) click-target
        // buttons above - see docs/progress.md's entry on this: Pool/board
        // stay flat icons, only the Firmament tray shows real die models.
        [SerializeField] private DiceRollDie _diePrefab;
        [SerializeField] private Transform _dieContainer;
        [SerializeField] private Camera _arenaCamera;

        // The RawImage's own RectTransform showing _arenaCamera's output -
        // NOT full-screen (unlike DiceRollController's roll overlay), so
        // ArenaProjection needs this rect explicitly to map a screen point
        // to the right viewport fraction.
        [SerializeField] private RectTransform _arenaViewport;

        private readonly List<DieButton> _spawned = new();
        private readonly List<DiceRollDie> _spawnedDice = new();

        // Which FirmamentDie.Id each pooled DiceRollDie currently displays -
        // Configure() rebuilds a mesh and destroys/recreates label
        // GameObjects, so it's only called when a slot's identity actually
        // changes (new entry, or an earlier entry's removal shifted indices),
        // not on every Render() pass.
        private readonly List<int> _spawnedDiceEntryId = new();

        private readonly Dictionary<Element, Material> _materialCache = new();

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
            _arenaCamera.enabled = firmament.Count > 0;

            for (int i = 0; i < firmament.Count; i++)
            {
                DieButton button = i < _spawned.Count ? _spawned[i] : Spawn();
                var entry = firmament[i];
                button.gameObject.SetActive(true);
                button.Initialize(
                    entry.Die.Element,
                    entry.Die.Face,
                    DieColors.ForElement(entry.Die.Element),
                    () => _controller.ArmDie(DieSource.Firmament, entry.Id, entry.Die));
                button.SetChromeVisible(false);
            }

            for (int i = firmament.Count; i < _spawned.Count; i++)
            {
                _spawned[i].gameObject.SetActive(false);
            }

            // HorizontalLayoutGroup only repositions its children at the next
            // scheduled layout pass (end of frame), not the instant a new
            // button is spawned/activated above - reading a just-spawned
            // button's RectTransform.position before forcing this rebuild
            // returned every entry's stale (pre-layout) position, found live
            // as every 3D die landing on top of each other at the same spot.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_container);

            Rect viewport = ArenaProjection.GetScreenRect(_arenaViewport);

            for (int i = 0; i < firmament.Count; i++)
            {
                DieButton button = _spawned[i];
                var entry = firmament[i];
                bool selected = _controller.IsHumanTurn
                    && _controller.ArmedSource == DieSource.Firmament
                    && _controller.ArmedIndex == entry.Id;

                DiceRollDie die3D = i < _spawnedDice.Count ? _spawnedDice[i] : Spawn3D();
                if (i >= _spawnedDiceEntryId.Count)
                {
                    _spawnedDiceEntryId.Add(-1);
                }

                if (_spawnedDiceEntryId[i] != entry.Id)
                {
                    var mesh = PlatonicSolidMeshFactory.Build(Sides.Of(entry.Die.Element));
                    die3D.Configure(mesh, MaterialFor(entry.Die.Element));
                    die3D.SetKinematic(true);
                    die3D.transform.rotation = die3D.RotationForFace(entry.Die.Face);
                    _spawnedDiceEntryId[i] = entry.Id;
                }

                die3D.gameObject.SetActive(true);
                die3D.transform.position = ArenaProjection.ScreenToWorldOnPlane(_arenaCamera, button.transform.position, DiePlaneHeight, viewport);
                die3D.SetHighlighted(selected);
            }

            for (int i = firmament.Count; i < _spawnedDice.Count; i++)
            {
                _spawnedDice[i].gameObject.SetActive(false);
                _spawnedDiceEntryId[i] = -1;
            }

            // Same rule as PoolView - either is an equally valid first
            // keyboard/controller target for an unarmed human turn.
            if (firmament.Count > 0 && _controller.IsHumanTurn && _controller.ArmedDie is null)
            {
                UiFocus.ClaimIfInvalid(_spawned[0].Button);
            }
        }

        private DieButton Spawn()
        {
            var button = Instantiate(_dieButtonPrefab, _container);
            _spawned.Add(button);
            return button;
        }

        private DiceRollDie Spawn3D()
        {
            var die = Instantiate(_diePrefab, _dieContainer);
            // _diePrefab is DiceRollController's own prefab, on the DiceRoll3D
            // layer - FirmamentCamera's culling mask only includes
            // FirmamentDice3D, so an instance left on the wrong layer renders
            // invisibly here (found live: the arena's clear color showed, but
            // no die ever appeared). Set on the die itself and its container -
            // Configure() builds label GameObjects that copy gameObject.layer
            // at that moment (see DiceRollDie's own comment), so this must
            // happen before Configure() is ever called on this instance.
            int firmamentLayer = LayerMask.NameToLayer("FirmamentDice3D");
            die.gameObject.layer = firmamentLayer;
            _spawnedDice.Add(die);
            return die;
        }

        private Material MaterialFor(Element element)
        {
            if (_materialCache.TryGetValue(element, out var cached))
            {
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = DieColors.ForElement(element) };
            _materialCache[element] = material;
            return material;
        }
    }
}
