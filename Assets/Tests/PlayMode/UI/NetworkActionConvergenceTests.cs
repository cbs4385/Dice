using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game;
using Quintessence.Game.Network;
using Quintessence.UI.Network;

namespace Quintessence.UI.Tests
{
    // Proves the core premise behind "async, same-seed drafting" (docs/gdd.md)
    // before any real transport exists: two independently-seeded IRng
    // instances, each driving their own GameState copy, converge to
    // byte-identical results as long as they only ever apply the same
    // NetworkActions in the same order - exactly what
    // LoopbackNetworkBridge.ActionConfirmed guarantees. Mirrors
    // SelfPlay.PlayRandomGame's own loop shape (Quintessence.Game.Tests.
    // SelfPlayPropertyTests already proves the engine itself is
    // deterministic), but routes every draft/forfeit through the bridge to
    // two independent "clients" instead of mutating one shared state
    // directly - the bridge's action-relay is what's actually under test
    // here, not the engine's own already-proven determinism.
    //
    // Plain synchronous [Test], not [UnityTest] - no MonoBehaviour/scene
    // involved - but lives in this PlayMode assembly since
    // Quintessence.UI.Tests (needed for Quintessence.UI.Network types) has
    // no separate EditMode counterpart.
    public class NetworkActionConvergenceTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TwoIndependentClients_ApplyingConfirmedActions_ConvergeToIdenticalState(int playerCount)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var rngA = Rng.Create(seed);
                var rngB = Rng.Create(seed);
                // Decides which legal draft to send each turn - a stand-in for
                // "the acting seat's own choice," deliberately a separate rng
                // from rngA/rngB so it never perturbs either client's own
                // reducer-internal RNG consumption.
                var decisionRng = Rng.Create(seed + 1_000_000);

                GameState stateA = GameSetup.NewGame(playerCount, rngA);
                GameState stateB = GameSetup.NewGame(playerCount, rngB);

                var bridge = new LoopbackNetworkBridge();
                bridge.ActionConfirmed += action => stateA = Apply(stateA, action, rngA);
                bridge.ActionConfirmed += action => stateB = Apply(stateB, action, rngB);

                while (!stateA.IsGameOver)
                {
                    if (stateA.CurrentPhase is null)
                    {
                        // StartRound isn't itself routed through the bridge in
                        // this slice (see GameSessionController.StartTurn) -
                        // both clients call it directly, still each with their
                        // own rng, and must still agree afterward.
                        stateA = GameReducer.StartRound(stateA, rngA);
                        stateB = GameReducer.StartRound(stateB, rngB);
                        AssertConverged(stateA, stateB, playerCount, seed);
                        continue;
                    }

                    int actingPlayer = GameReducer.CurrentPlayer(stateA);
                    var candidates = LegalDrafts.EnumerateSimple(stateA);
                    NetworkAction action = candidates.Count == 0
                        ? new NetworkAction.Forfeit { ActingPlayer = actingPlayer }
                        : new NetworkAction.Draft(candidates[decisionRng.NextInt(candidates.Count)]) { ActingPlayer = actingPlayer };

                    bridge.SendIntent(action);
                    AssertConverged(stateA, stateB, playerCount, seed);
                }
            }
        }

        private static GameState Apply(GameState state, NetworkAction action, IRng rng) => action switch
        {
            NetworkAction.Draft draft => GameReducer.ApplyDraft(state, draft.Choice, rng),
            NetworkAction.Forfeit => GameReducer.ApplyForfeit(state),
            _ => state,
        };

        private static void AssertConverged(GameState a, GameState b, int playerCount, int seed)
        {
            Assert.That(b.Round, Is.EqualTo(a.Round), $"seed {seed}");
            Assert.That(b.IsGameOver, Is.EqualTo(a.IsGameOver), $"seed {seed}");

            for (int p = 0; p < playerCount; p++)
            {
                Assert.That(b.Players[p].FavorRemaining, Is.EqualTo(a.Players[p].FavorRemaining), $"seed {seed} player {p}");
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        Assert.That(b.Players[p].Board.DieAt(r, c), Is.EqualTo(a.Players[p].Board.DieAt(r, c)), $"seed {seed} player {p} cell ({r},{c})");
                    }
                }
            }
        }
    }
}
