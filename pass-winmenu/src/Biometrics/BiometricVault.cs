using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PassWinmenu.Configuration;
using Windows.Security.Credentials;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Default <see cref="IBiometricVault"/>: ties together the Windows Hello key store, the
/// passphrase protector, and the blob store.
/// </summary>
internal sealed class BiometricVault : IBiometricVault
{
	// A fixed, non-secret value signed by the Windows Hello key. Only the (deterministic)
	// signature is used as key-derivation input; the challenge itself need not be secret.
	private static readonly byte[] Challenge = Encoding.ASCII.GetBytes("pass-winmenu-hello-v1");

	private readonly IBiometricKeyStore keyStore;
	private readonly IPassphraseProtector protector;
	private readonly IBiometricBlobStore blobStore;
	private readonly BiometricConfig config;

	public BiometricVault(
		IBiometricKeyStore keyStore,
		IPassphraseProtector protector,
		IBiometricBlobStore blobStore,
		BiometricConfig config)
	{
		this.keyStore = keyStore;
		this.protector = protector;
		this.blobStore = blobStore;
		this.config = config;
	}

	public Task<bool> IsAvailableAsync() => keyStore.IsAvailableAsync();

	public bool IsEnrolled => blobStore.Exists();

	public async Task EnrollAsync(char[] passphrase)
	{
		await keyStore.CreateAsync(config.CredentialName);
		var signature = await keyStore.SignAsync(config.CredentialName, Challenge);
		try
		{
			var blob = protector.Protect(signature, passphrase);
			blobStore.Write(blob);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(signature);
		}
	}

	public async Task<UnlockResult> TryUnlockAsync()
	{
		if (!IsEnrolled)
		{
			return UnlockResult.Of(UnlockOutcome.NotEnrolled);
		}

		if (!await keyStore.IsAvailableAsync())
		{
			return UnlockResult.Of(UnlockOutcome.Unavailable);
		}

		byte[] blob;
		try
		{
			blob = blobStore.Read();
		}
		catch (CryptographicException e)
		{
			// The stored blob can no longer be unwrapped (DPAPI keys changed, different user, or a
			// pre-DPAPI blob) — force re-enrolment.
			return UnlockResult.Of(UnlockOutcome.KeyInvalidated, e.Message);
		}

		byte[] signature;
		try
		{
			signature = await keyStore.SignAsync(config.CredentialName, Challenge);
		}
		catch (BiometricException e) when (e.Status is KeyCredentialStatus.UserCanceled or KeyCredentialStatus.UserPrefersPassword)
		{
			return UnlockResult.Of(UnlockOutcome.Cancelled);
		}
		catch (BiometricException e) when (e.Status is KeyCredentialStatus.NotFound)
		{
			// The credential was removed/invalidated (e.g. Windows Hello was reset).
			return UnlockResult.Of(UnlockOutcome.KeyInvalidated, e.Message);
		}
		catch (BiometricException e)
		{
			return UnlockResult.Of(UnlockOutcome.Failed, e.Message);
		}

		try
		{
			var passphrase = protector.Unprotect(signature, blob);
			return UnlockResult.Succeeded(passphrase);
		}
		catch (CryptographicException e)
		{
			// Tag mismatch => the Hello signature changed (key reset) or the blob is corrupt.
			return UnlockResult.Of(UnlockOutcome.KeyInvalidated, e.Message);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(signature);
		}
	}

	public void ClearEnrollment()
	{
		// Deleting the blob is enough to force re-enrolment. The Hello credential itself is
		// harmless to leave (re-enrolment replaces it).
		blobStore.Delete();
	}
}
