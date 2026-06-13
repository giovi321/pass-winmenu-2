using System.Threading.Tasks;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Biometrics
{
	public class BiometricVaultTests
	{
		private static BiometricVault BuildVault(
			out FakeBiometricKeyStore keyStore,
			out InMemoryBlobStore blobStore)
		{
			keyStore = new FakeBiometricKeyStore();
			blobStore = new InMemoryBlobStore();
			return new BiometricVault(
				keyStore,
				new PassphraseProtector(),
				blobStore,
				new BiometricConfig());
		}

		[Fact]
		public async Task TryUnlock_WhenNotEnrolled_ReturnsNotEnrolled()
		{
			var vault = BuildVault(out _, out _);

			var result = await vault.TryUnlockAsync();

			result.Outcome.ShouldBe(UnlockOutcome.NotEnrolled);
		}

		[Fact]
		public async Task EnrollThenUnlock_ReturnsTheSamePassphrase()
		{
			var vault = BuildVault(out _, out _);

			await vault.EnrollAsync("s3cret".ToCharArray());
			var result = await vault.TryUnlockAsync();

			result.Outcome.ShouldBe(UnlockOutcome.Success);
			new string(result.Passphrase).ShouldBe("s3cret");
		}

		[Fact]
		public async Task Enroll_MarksVaultAsEnrolled()
		{
			var vault = BuildVault(out _, out _);

			vault.IsEnrolled.ShouldBeFalse();
			await vault.EnrollAsync("s3cret".ToCharArray());
			vault.IsEnrolled.ShouldBeTrue();
		}

		[Fact]
		public async Task TryUnlock_WhenUnavailable_ReturnsUnavailable()
		{
			var vault = BuildVault(out var keyStore, out _);
			await vault.EnrollAsync("s3cret".ToCharArray());

			keyStore.Available = false;
			var result = await vault.TryUnlockAsync();

			result.Outcome.ShouldBe(UnlockOutcome.Unavailable);
		}

		[Fact]
		public async Task TryUnlock_WhenHelloKeyChanged_ReturnsKeyInvalidated()
		{
			var vault = BuildVault(out var keyStore, out _);
			await vault.EnrollAsync("s3cret".ToCharArray());

			// Simulate the Windows Hello key being reset: signatures no longer match.
			keyStore.SignatureSeed = new byte[] { 42, 42, 42, 42 };
			var result = await vault.TryUnlockAsync();

			result.Outcome.ShouldBe(UnlockOutcome.KeyInvalidated);
		}

		[Fact]
		public async Task ClearEnrollment_RemovesTheStoredBlob()
		{
			var vault = BuildVault(out _, out _);
			await vault.EnrollAsync("s3cret".ToCharArray());

			vault.ClearEnrollment();

			vault.IsEnrolled.ShouldBeFalse();
			(await vault.TryUnlockAsync()).Outcome.ShouldBe(UnlockOutcome.NotEnrolled);
		}
	}
}
