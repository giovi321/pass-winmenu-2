using Moq;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenu.Notifications;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Biometrics
{
	public class BiometricPassphraseProviderTests
	{
		// Builds a provider over a real vault (FakeBiometricKeyStore + in-memory blob) so the
		// success/disabled/not-enrolled paths exercise the actual unlock flow.
		private static (BiometricPassphraseProvider provider, FakeBiometricKeyStore keyStore) Build(
			BiometricConfig config,
			bool enrol = true)
		{
			var keyStore = new FakeBiometricKeyStore();
			var vault = new BiometricVault(keyStore, new PassphraseProtector(), new InMemoryBlobStore(), config);
			if (enrol)
			{
				vault.EnrollAsync("s3cret".ToCharArray()).GetAwaiter().GetResult();
			}

			// Enrolment performs one signature; only count gestures made during GetPassphrase.
			keyStore.ResetSignCount();

			var provider = new BiometricPassphraseProvider(config, vault, Mock.Of<INotificationService>());
			return (provider, keyStore);
		}

		// Builds a provider over a mocked vault so a specific UnlockOutcome can be injected.
		private static BiometricPassphraseProvider BuildWithVault(
			IBiometricVault vault,
			bool enabled = true,
			INotificationService? notifications = null)
		{
			return new BiometricPassphraseProvider(
				new BiometricConfig { Enabled = enabled },
				vault,
				notifications ?? Mock.Of<INotificationService>());
		}

		[Fact]
		public void IsEnabled_ReflectsConfig()
		{
			Build(new BiometricConfig { Enabled = true }).provider.IsEnabled.ShouldBeTrue();
			Build(new BiometricConfig { Enabled = false }).provider.IsEnabled.ShouldBeFalse();
		}

		[Fact]
		public void GetPassphrase_WhenDisabled_ReturnsUnavailableWithoutGesture()
		{
			var (provider, keyStore) = Build(new BiometricConfig { Enabled = false });

			provider.GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Unavailable);
			keyStore.SignCount.ShouldBe(0);
		}

		[Fact]
		public void GetPassphrase_Success_ReturnsProvidedPassphrase()
		{
			var (provider, keyStore) = Build(new BiometricConfig { Enabled = true });

			var result = provider.GetPassphrase();

			result.Outcome.ShouldBe(PassphraseOutcome.Provided);
			new string(result.Passphrase).ShouldBe("s3cret");
			keyStore.SignCount.ShouldBe(1);
		}

		[Fact]
		public void GetPassphrase_UnlocksWithAFreshGestureOnEveryCall()
		{
			// The provider keeps no cache of its own (gpg-agent's cache is the only one), so each
			// call prompts for a fresh gesture.
			var (provider, keyStore) = Build(new BiometricConfig { Enabled = true });

			provider.GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Provided);
			provider.GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Provided);

			keyStore.SignCount.ShouldBe(2);
		}

		[Fact]
		public void GetPassphrase_NotEnrolled_ReturnsUnavailable()
		{
			var (provider, _) = Build(new BiometricConfig { Enabled = true }, enrol: false);

			provider.GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Unavailable);
		}

		[Fact]
		public void GetPassphrase_UserCancelled_ReturnsDeclined()
		{
			var vault = new Mock<IBiometricVault>();
			vault.Setup(v => v.TryUnlockAsync()).ReturnsAsync(UnlockResult.Of(UnlockOutcome.Cancelled));

			BuildWithVault(vault.Object).GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Declined);
		}

		[Fact]
		public void GetPassphrase_Failed_ReturnsUnavailableAndNotifies()
		{
			var vault = new Mock<IBiometricVault>();
			vault.Setup(v => v.TryUnlockAsync()).ReturnsAsync(UnlockResult.Of(UnlockOutcome.Failed, "boom"));
			var notifications = new Mock<INotificationService>();

			BuildWithVault(vault.Object, notifications: notifications.Object)
				.GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Unavailable);

			notifications.Verify(n => n.Raise(It.IsAny<string>(), Severity.Error), Times.Once);
		}

		[Fact]
		public void GetPassphrase_KeyInvalidated_ClearsEnrollmentAndReturnsUnavailable()
		{
			var vault = new Mock<IBiometricVault>();
			vault.Setup(v => v.TryUnlockAsync()).ReturnsAsync(UnlockResult.Of(UnlockOutcome.KeyInvalidated, "reset"));

			BuildWithVault(vault.Object).GetPassphrase().Outcome.ShouldBe(PassphraseOutcome.Unavailable);

			vault.Verify(v => v.ClearEnrollment(), Times.Once);
		}

		[Fact]
		public void Invalidate_ClearsEnrollment()
		{
			var vault = new Mock<IBiometricVault>();

			BuildWithVault(vault.Object).Invalidate();

			vault.Verify(v => v.ClearEnrollment(), Times.Once);
		}
	}
}
