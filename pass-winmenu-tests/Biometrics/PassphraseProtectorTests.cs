using System.Security.Cryptography;
using PassWinmenu.Biometrics;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Biometrics
{
	public class PassphraseProtectorTests
	{
		private static readonly byte[] Signature = { 9, 8, 7, 6, 5, 4, 3, 2, 1 };

		[Fact]
		public void ProtectThenUnprotect_WithSameSignature_RoundTrips()
		{
			var protector = new PassphraseProtector();

			var blob = protector.Protect(Signature, "correct horse".ToCharArray());
			var recovered = protector.Unprotect(Signature, blob);

			new string(recovered).ShouldBe("correct horse");
		}

		[Fact]
		public void Protect_ProducesDifferentBlobsEachTime_DueToRandomNonce()
		{
			var protector = new PassphraseProtector();

			var first = protector.Protect(Signature, "hunter2".ToCharArray());
			var second = protector.Protect(Signature, "hunter2".ToCharArray());

			first.ShouldNotBe(second);
		}

		[Fact]
		public void Unprotect_WithTamperedBlob_Throws()
		{
			var protector = new PassphraseProtector();
			var blob = protector.Protect(Signature, "hunter2".ToCharArray());

			blob[^1] ^= 0xFF;

			Should.Throw<CryptographicException>(() => protector.Unprotect(Signature, blob));
		}

		[Fact]
		public void Unprotect_WithDifferentSignature_Throws()
		{
			var protector = new PassphraseProtector();
			var blob = protector.Protect(Signature, "hunter2".ToCharArray());

			Should.Throw<CryptographicException>(
				() => protector.Unprotect(new byte[] { 1, 1, 1, 1 }, blob));
		}

		[Fact]
		public void Unprotect_WithTruncatedBlob_Throws()
		{
			var protector = new PassphraseProtector();

			Should.Throw<CryptographicException>(
				() => protector.Unprotect(Signature, new byte[] { 1, 2, 3 }));
		}
	}
}
