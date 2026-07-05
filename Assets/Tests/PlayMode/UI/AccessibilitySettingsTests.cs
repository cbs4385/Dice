using NUnit.Framework;

namespace Quintessence.UI.Tests
{
    // AccessibilitySettings is backed by real PlayerPrefs (persists to disk
    // on the actual machine) - every test saves and restores the real value
    // around its own assertions so it can't leak into or pollute the actual
    // saved preference, or another test run after it.
    public class AccessibilitySettingsTests
    {
        [Test]
        public void ReducedMotion_RoundTrips()
        {
            bool original = AccessibilitySettings.ReducedMotion;
            try
            {
                AccessibilitySettings.ReducedMotion = true;
                Assert.That(AccessibilitySettings.ReducedMotion, Is.True);

                AccessibilitySettings.ReducedMotion = false;
                Assert.That(AccessibilitySettings.ReducedMotion, Is.False);
            }
            finally
            {
                AccessibilitySettings.ReducedMotion = original;
            }
        }

        [Test]
        public void ScreenShake_RoundTrips()
        {
            bool original = AccessibilitySettings.ScreenShake;
            try
            {
                AccessibilitySettings.ScreenShake = false;
                Assert.That(AccessibilitySettings.ScreenShake, Is.False);

                AccessibilitySettings.ScreenShake = true;
                Assert.That(AccessibilitySettings.ScreenShake, Is.True);
            }
            finally
            {
                AccessibilitySettings.ScreenShake = original;
            }
        }

        [Test]
        public void Changed_FiresOnEitherSetter()
        {
            bool originalMotion = AccessibilitySettings.ReducedMotion;
            bool originalShake = AccessibilitySettings.ScreenShake;
            int fireCount = 0;
            void Handler() => fireCount++;

            AccessibilitySettings.Changed += Handler;
            try
            {
                AccessibilitySettings.ReducedMotion = !originalMotion;
                AccessibilitySettings.ScreenShake = !originalShake;

                Assert.That(fireCount, Is.EqualTo(2));
            }
            finally
            {
                AccessibilitySettings.Changed -= Handler;
                AccessibilitySettings.ReducedMotion = originalMotion;
                AccessibilitySettings.ScreenShake = originalShake;
            }
        }
    }
}
