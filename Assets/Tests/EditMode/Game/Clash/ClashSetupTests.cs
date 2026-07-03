using System.Linq;
using NUnit.Framework;
using Quintessence.Engine;
using Quintessence.Game.Clash;

namespace Quintessence.Game.Tests.Clash
{
    public class ClashSetupTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Deal_CreatesExpectedShape(int playerCount)
        {
            var config = ClashConfig.Default;
            var clash = ClashSetup.Deal(playerCount, config, Rng.Create(1));

            Assert.That(clash.Storm, Has.Count.EqualTo(playerCount));
            Assert.That(clash.Storm, Is.All.EqualTo(0));
            Assert.That(clash.InterventionsAvailable, Has.Count.EqualTo(config.InterventionsPerMatch));
            Assert.That(clash.InterventionsAvailable.Distinct().Count(), Is.EqualTo(config.InterventionsPerMatch), "no duplicate kinds dealt");
            Assert.That(clash.PetrifyTokens, Is.Empty);
            Assert.That(clash.Pending, Is.Null);
            Assert.That(clash.InterventionLog, Is.Empty);
        }

        [Test]
        public void Deal_SameSeed_IsDeterministic()
        {
            var a = ClashSetup.Deal(3, ClashConfig.Default, Rng.Create(42));
            var b = ClashSetup.Deal(3, ClashConfig.Default, Rng.Create(42));

            Assert.That(b.InterventionsAvailable, Is.EqualTo(a.InterventionsAvailable));
        }

        [Test]
        public void NewGame_WithClashConfig_PopulatesClashState()
        {
            var state = GameSetup.NewGame(2, Rng.Create(1), clashConfig: ClashConfig.Default);

            Assert.That(state.Clash, Is.Not.Null);
            Assert.That(state.Clash!.Storm, Has.Count.EqualTo(2));
        }
    }
}
