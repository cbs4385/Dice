using NUnit.Framework;
using UnityEngine;
using Quintessence.UI.Network;

namespace Quintessence.UI.Tests
{
    // Proves SteamService's "fail gracefully, never crash" contract really
    // holds, rather than just asserting it in a comment - the expected state
    // on this CI runner (confirmed Linux, no Steam installed) and possibly
    // this local machine too. Only asserts "does not throw" - whether
    // TryInit returns true or false depends on whether Steam happens to be
    // running locally, which this test deliberately doesn't assume either way.
    public class SteamServiceTests
    {
        [Test]
        public void TryInit_NeverThrows_RegardlessOfSteamAvailability()
        {
            var go = new GameObject("SteamServiceTest");
            try
            {
                var service = go.AddComponent<SteamService>();
                Assert.DoesNotThrow(() => service.TryInit(SteamService.PlaceholderAppId));
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        [Test]
        public void TryInit_CalledTwice_SecondCallIsANoOpAndStillDoesNotThrow()
        {
            var go = new GameObject("SteamServiceTest");
            try
            {
                var service = go.AddComponent<SteamService>();
                service.TryInit(SteamService.PlaceholderAppId);
                bool secondResult = false;
                Assert.DoesNotThrow(() => secondResult = service.TryInit(SteamService.PlaceholderAppId));
                Assert.That(secondResult, Is.EqualTo(service.IsInitialized));
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
