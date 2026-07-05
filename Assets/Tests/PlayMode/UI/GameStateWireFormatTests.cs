using System.Linq;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game;
using Quintessence.Game.Clash;
using Quintessence.Game.Network;
using Quintessence.UI.SaveGame;

namespace Quintessence.UI.Tests
{
    // Round-trips representative GameState shapes through GameStateWireFormat.
    // Compared field-by-field/cell-by-cell rather than via record Equals - this
    // project already has one real bug on record equality (Bag's Dictionary
    // property silently doing reference equality), so hand-rolled deep
    // comparison is the established, trusted pattern for this kind of check.
    public class GameStateWireFormatTests
    {
        [Test]
        public void RoundTrips_NonClashMidGame()
        {
            var rng = Rng.Create(111);
            var state = GameSetup.NewGame(2, rng);
            state = GameReducer.StartRound(state, rng);
            state = GameReducer.ApplyDraft(state, LegalDrafts.EnumerateSimple(state).First(), rng);

            var seats = new[] { SeatControl.LocalHuman, SeatControl.Ai };
            byte[] bytes = GameStateWireFormat.Encode(rng.ExportState(), seats, state);
            var (rngState, decodedSeats, decoded) = GameStateWireFormat.Decode(bytes);

            Assert.That(rngState, Is.EqualTo(rng.ExportState()));
            Assert.That(decodedSeats, Is.EqualTo(seats));
            AssertStatesEqual(state, decoded);
        }

        [Test]
        public void RoundTrips_ClashMidGameWithPendingInterventionAndTokens()
        {
            var rng = Rng.Create(222);
            var state = GameSetup.NewGame(3, rng, clashConfig: ClashConfig.Default);
            state = GameReducer.StartRound(state, rng);

            // Force a Petrify token and a pending intervention directly - the
            // wire format's job is to round-trip whatever shape ClashState can
            // hold, not to re-derive a specific one through gameplay.
            state = state with
            {
                Clash = state.Clash! with
                {
                    PetrifyTokens = new[] { new PetrifyToken(Player: 1, Row: 0, Col: 2, ExpiresRound: state.Round + 1) },
                    Pending = new PendingIntervention(
                        Actor: 0,
                        Target: 1,
                        Kind: InterventionKind.Scorch,
                        Params: new InterventionParams.Scorch(TargetPlayer: 1, Row: 1, Col: 1, Pips: 2)),
                    InterventionLog = new[] { new ClashLogEntry(state.Round, 0, 1, InterventionKind.Scorch, ClashLogOutcome.Declared) },
                    NullifiedBandCells = new[] { new NullifiedCell(Player: 2, Row: 2, Col: 3) },
                    Storm = new[] { 2, 0, 1 },
                },
            };

            var seats = new[] { SeatControl.LocalHuman, SeatControl.Ai, SeatControl.Ai };
            byte[] bytes = GameStateWireFormat.Encode(rng.ExportState(), seats, state);
            var (_, _, decoded) = GameStateWireFormat.Decode(bytes);

            AssertStatesEqual(state, decoded);
        }

        [Test]
        public void RoundTrips_GameOverState()
        {
            var rng = Rng.Create(333);
            var state = GameSetup.NewGame(2, rng) with { IsGameOver = true, CurrentPhase = null };

            var seats = new[] { SeatControl.LocalHuman, SeatControl.LocalHuman };
            byte[] bytes = GameStateWireFormat.Encode(rng.ExportState(), seats, state);
            var (_, _, decoded) = GameStateWireFormat.Decode(bytes);

            Assert.That(decoded.IsGameOver, Is.True);
            Assert.That(decoded.CurrentPhase, Is.Null);
            AssertStatesEqual(state, decoded);
        }

        private static void AssertStatesEqual(GameState expected, GameState actual)
        {
            Assert.That(actual.Round, Is.EqualTo(expected.Round));
            Assert.That(actual.StartPlayerIndex, Is.EqualTo(expected.StartPlayerIndex));
            Assert.That(actual.Objective, Is.EqualTo(expected.Objective));
            Assert.That(actual.NextFirmamentId, Is.EqualTo(expected.NextFirmamentId));
            Assert.That(actual.IsGameOver, Is.EqualTo(expected.IsGameOver));

            Assert.That(actual.Players.Count, Is.EqualTo(expected.Players.Count));
            for (int p = 0; p < expected.Players.Count; p++)
            {
                Assert.That(actual.Players[p].FavorRemaining, Is.EqualTo(expected.Players[p].FavorRemaining), $"player {p} favor");
                Assert.That(actual.Players[p].PrivateElement, Is.EqualTo(expected.Players[p].PrivateElement), $"player {p} element");
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        Assert.That(actual.Players[p].Board.CellAt(r, c), Is.EqualTo(expected.Players[p].Board.CellAt(r, c)), $"player {p} cell ({r},{c})");
                        Assert.That(actual.Players[p].Board.DieAt(r, c), Is.EqualTo(expected.Players[p].Board.DieAt(r, c)), $"player {p} die ({r},{c})");
                    }
                }
            }

            foreach (var element in Elements.All)
            {
                int expectedCount = expected.Bag.Remaining.TryGetValue(element, out var e) ? e : 0;
                int actualCount = actual.Bag.Remaining.TryGetValue(element, out var a) ? a : 0;
                Assert.That(actualCount, Is.EqualTo(expectedCount), $"bag {element}");
            }

            Assert.That(actual.Firmament.Count, Is.EqualTo(expected.Firmament.Count));
            for (int i = 0; i < expected.Firmament.Count; i++)
            {
                Assert.That(actual.Firmament[i].Id, Is.EqualTo(expected.Firmament[i].Id));
                Assert.That(actual.Firmament[i].Die, Is.EqualTo(expected.Firmament[i].Die));
            }

            Assert.That(actual.CurrentPhase is null, Is.EqualTo(expected.CurrentPhase is null));
            if (expected.CurrentPhase is not null && actual.CurrentPhase is not null)
            {
                Assert.That(actual.CurrentPhase.PickNumber, Is.EqualTo(expected.CurrentPhase.PickNumber));
                Assert.That(actual.CurrentPhase.PickOrder, Is.EqualTo(expected.CurrentPhase.PickOrder));
                Assert.That(actual.CurrentPhase.PickOrderIndex, Is.EqualTo(expected.CurrentPhase.PickOrderIndex));
                Assert.That(actual.CurrentPhase.Pool, Is.EqualTo(expected.CurrentPhase.Pool));
            }

            Assert.That(actual.Clash is null, Is.EqualTo(expected.Clash is null));
            if (expected.Clash is not null && actual.Clash is not null)
            {
                Assert.That(actual.Clash.Config, Is.EqualTo(expected.Clash.Config));
                Assert.That(actual.Clash.Storm, Is.EqualTo(expected.Clash.Storm));
                Assert.That(actual.Clash.InterventionsAvailable, Is.EqualTo(expected.Clash.InterventionsAvailable));
                Assert.That(actual.Clash.PetrifyTokens, Is.EqualTo(expected.Clash.PetrifyTokens));
                Assert.That(actual.Clash.Pending, Is.EqualTo(expected.Clash.Pending));
                Assert.That(actual.Clash.InterventionLog, Is.EqualTo(expected.Clash.InterventionLog));
                Assert.That(actual.Clash.NullifiedBandCells, Is.EqualTo(expected.Clash.NullifiedBandCells));
            }
        }
    }
}
