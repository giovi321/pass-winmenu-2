using System.Threading.Tasks;
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

		[Fact]
		public void GetPassphrase_WhenDisabled_ReturnsNull()
		{
			var (provider, keyStore) = Build(new BiometricConfig { Enabled = false, Mode = BiometricUnlockMode.EveryPassword });

			provider.GetPassphrase().ShouldBeNull();
			keyStore.SignCount.ShouldBe(0);
		}

		[Fact]
		public void GetPassphrase_OncePerSession_ReturnsNull()
		{
			// Once-per-session is handled by the startup preset job, not the loopback provider.
			var (provider, _) = Build(new BiometricConfig { Enabled = true, Mode = BiometricUnlockMode.OncePerSession });

			provider.GetPassphrase().ShouldBeNull();
		}

		[Fact]
		public void GetPassphrase_EveryPassword_UnlocksOnEveryCall()
		{
			var (provider, keyStore) = Build(new BiometricConfig { Enabled = true, Mode = BiometricUnlockMode.EveryPassword });

			new string(provider.GetPassphrase()).ShouldBe("s3cret");
			new string(provider.GetPassphrase()).ShouldBe("s3cret");

			keyStore.SignCount.ShouldBe(2);
		}

		[Fact]
		public void GetPassphrase_Cache_ReusesCachedPassphraseWithinWindow()
		{
			var (provider, keyStore) = Build(new BiometricConfig { Enabled = true, Mode = BiometricUnlockMode.Cache, CacheSeconds = 3600 });

			new string(provider.GetPassphrase()).ShouldBe("s3cret");
			new string(provider.GetPassphrase()).ShouldBe("s3cret");

			// Second call served from cache: only one Hello gesture.
			keyStore.SignCount.ShouldBe(1);
		}

		[Fact]
		public void GetPassphrase_NotEnrolled_ReturnsNull()
		{
			var (provider, _) = Build(
				new BiometricConfig { Enabled = true, Mode = BiometricUnlockMode.EveryPassword },
				enrol: false);

			provider.GetPassphrase().ShouldBeNull();
		}
	}
}
