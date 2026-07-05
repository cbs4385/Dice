using NUnit.Framework;
using Quintessence.Game;
using Quintessence.Game.Clash;
using Quintessence.Game.Network;
using Quintessence.UI.Network;

namespace Quintessence.UI.Tests
{
    // Round-trip encode/decode for each of NetworkAction's 5 variants -
    // proves the manual wire format SteamNetworkBridge sends over the wire
    // preserves every field, without needing any Steam client at all. Plain
    // synchronous [Test]s, lives here for the same reason
    // NetworkActionConvergenceTests does (no separate EditMode assembly has
    // access to Quintessence.UI.Network types).
    public class NetworkActionWireFormatTests
    {
        [Test]
        public void Draft_WithAdjustFavor_RoundTrips()
        {
            var original = new NetworkAction.Draft(new DraftChoice(DieSource.Pool, 2, 1, 3, new FavorAction.Adjust(-1)))
            {
                ActingPlayer = 1,
                SequenceNumber = 42,
            };

            var decoded = (NetworkAction.Draft)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.ActingPlayer, Is.EqualTo(1));
            Assert.That(decoded.SequenceNumber, Is.EqualTo(42));
            Assert.That(decoded.Choice.Source, Is.EqualTo(DieSource.Pool));
            Assert.That(decoded.Choice.Index, Is.EqualTo(2));
            Assert.That(decoded.Choice.Row, Is.EqualTo(1));
            Assert.That(decoded.Choice.Col, Is.EqualTo(3));
            Assert.That(decoded.Choice.Favor, Is.InstanceOf<FavorAction.Adjust>());
            Assert.That(((FavorAction.Adjust)decoded.Choice.Favor!).Delta, Is.EqualTo(-1));
        }

        [Test]
        public void Draft_WithNoFavor_RoundTrips()
        {
            var original = new NetworkAction.Draft(new DraftChoice(DieSource.Firmament, 0, 2, 0))
            {
                ActingPlayer = 0,
                SequenceNumber = 0,
            };

            var decoded = (NetworkAction.Draft)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.Choice.Source, Is.EqualTo(DieSource.Firmament));
            Assert.That(decoded.Choice.Favor, Is.Null);
        }

        [Test]
        public void Draft_WithRerollFavor_RoundTrips()
        {
            var original = new NetworkAction.Draft(new DraftChoice(DieSource.Pool, 0, 0, 0, new FavorAction.Reroll()))
            {
                ActingPlayer = 0,
                SequenceNumber = 1,
            };

            var decoded = (NetworkAction.Draft)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.Choice.Favor, Is.InstanceOf<FavorAction.Reroll>());
        }

        [Test]
        public void Draft_WithDefyFavor_RoundTrips()
        {
            var original = new NetworkAction.Draft(new DraftChoice(DieSource.Pool, 0, 0, 0, new FavorAction.Defy()))
            {
                ActingPlayer = 3,
                SequenceNumber = 7,
            };

            var decoded = (NetworkAction.Draft)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.Choice.Favor, Is.InstanceOf<FavorAction.Defy>());
        }

        [Test]
        public void Forfeit_RoundTrips()
        {
            var original = new NetworkAction.Forfeit { ActingPlayer = 2, SequenceNumber = 9 };

            var decoded = NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded, Is.InstanceOf<NetworkAction.Forfeit>());
            Assert.That(decoded.ActingPlayer, Is.EqualTo(2));
            Assert.That(decoded.SequenceNumber, Is.EqualTo(9));
        }

        [Test]
        public void Declare_Scorch_RoundTrips()
        {
            var original = new NetworkAction.Declare(InterventionKind.Scorch, new InterventionParams.Scorch(1, 2, 3, 4))
            {
                ActingPlayer = 0,
                SequenceNumber = 5,
            };

            var decoded = (NetworkAction.Declare)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.Kind, Is.EqualTo(InterventionKind.Scorch));
            var scorch = (InterventionParams.Scorch)decoded.Params;
            Assert.That(scorch.TargetPlayer, Is.EqualTo(1));
            Assert.That(scorch.Row, Is.EqualTo(2));
            Assert.That(scorch.Col, Is.EqualTo(3));
            Assert.That(scorch.Pips, Is.EqualTo(4));
        }

        [Test]
        public void Declare_Riptide_RoundTrips()
        {
            var original = new NetworkAction.Declare(InterventionKind.Riptide, new InterventionParams.Riptide(7, 1, 2)) { ActingPlayer = 1, SequenceNumber = 1 };
            var decoded = (NetworkAction.Declare)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            var riptide = (InterventionParams.Riptide)decoded.Params;
            Assert.That(riptide.FirmamentId, Is.EqualTo(7));
            Assert.That(riptide.Row, Is.EqualTo(1));
            Assert.That(riptide.Col, Is.EqualTo(2));
        }

        [Test]
        public void Declare_Gust_RoundTrips()
        {
            var original = new NetworkAction.Declare(InterventionKind.Gust, new InterventionParams.Gust(3, 0, 1)) { ActingPlayer = 0, SequenceNumber = 2 };
            var decoded = (NetworkAction.Declare)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            var gust = (InterventionParams.Gust)decoded.Params;
            Assert.That(gust.PoolIndex, Is.EqualTo(3));
            Assert.That(gust.Row, Is.EqualTo(0));
            Assert.That(gust.Col, Is.EqualTo(1));
        }

        [Test]
        public void Declare_Petrify_RoundTrips()
        {
            var original = new NetworkAction.Declare(InterventionKind.Petrify, new InterventionParams.Petrify(2, 1, 1)) { ActingPlayer = 0, SequenceNumber = 3 };
            var decoded = (NetworkAction.Declare)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            var petrify = (InterventionParams.Petrify)decoded.Params;
            Assert.That(petrify.TargetPlayer, Is.EqualTo(2));
            Assert.That(petrify.Row, Is.EqualTo(1));
            Assert.That(petrify.Col, Is.EqualTo(1));
        }

        [Test]
        public void Declare_EclipseNullifyBand_RoundTrips()
        {
            var original = new NetworkAction.Declare(InterventionKind.Eclipse, new InterventionParams.EclipseNullifyBand(1, 2, 3)) { ActingPlayer = 0, SequenceNumber = 4 };
            var decoded = (NetworkAction.Declare)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            var eclipse = (InterventionParams.EclipseNullifyBand)decoded.Params;
            Assert.That(eclipse.TargetPlayer, Is.EqualTo(1));
            Assert.That(eclipse.Row, Is.EqualTo(2));
            Assert.That(eclipse.Col, Is.EqualTo(3));
        }

        [Test]
        public void Declare_EclipseCancel_RoundTrips()
        {
            var original = new NetworkAction.Declare(InterventionKind.Eclipse, new InterventionParams.EclipseCancel()) { ActingPlayer = 0, SequenceNumber = 5 };
            var decoded = (NetworkAction.Declare)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            Assert.That(decoded.Params, Is.InstanceOf<InterventionParams.EclipseCancel>());
        }

        [Test]
        public void Ward_RoundTrips()
        {
            var original = new NetworkAction.Ward { ActingPlayer = 1, SequenceNumber = 6 };
            var decoded = NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            Assert.That(decoded, Is.InstanceOf<NetworkAction.Ward>());
            Assert.That(decoded.ActingPlayer, Is.EqualTo(1));
        }

        [Test]
        public void DeclineWard_RoundTrips()
        {
            var original = new NetworkAction.DeclineWard { ActingPlayer = 1, SequenceNumber = 7 };
            var decoded = NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));
            Assert.That(decoded, Is.InstanceOf<NetworkAction.DeclineWard>());
        }

        [Test]
        public void MatchStart_RoundTrips()
        {
            var seats = new[] { SeatControl.LocalHuman, SeatControl.RemoteHuman, SeatControl.Ai };
            var original = new NetworkAction.MatchStart(123456789L, 3, seats, IsClash: true)
            {
                ActingPlayer = 0,
                SequenceNumber = 0,
            };

            var decoded = (NetworkAction.MatchStart)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.Seed, Is.EqualTo(123456789L));
            Assert.That(decoded.PlayerCount, Is.EqualTo(3));
            Assert.That(decoded.IsClash, Is.True);
            Assert.That(decoded.Seats, Is.EqualTo(seats));
        }

        [Test]
        public void MatchStart_NotClash_RoundTrips()
        {
            var seats = new[] { SeatControl.LocalHuman, SeatControl.Ai };
            var original = new NetworkAction.MatchStart(-42L, 2, seats, IsClash: false)
            {
                ActingPlayer = 0,
                SequenceNumber = 0,
            };

            var decoded = (NetworkAction.MatchStart)NetworkActionWireFormat.Decode(NetworkActionWireFormat.Encode(original));

            Assert.That(decoded.Seed, Is.EqualTo(-42L));
            Assert.That(decoded.IsClash, Is.False);
            Assert.That(decoded.Seats, Is.EqualTo(seats));
        }
    }
}
