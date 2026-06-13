using System.Threading.Tasks;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Abstraction over a Windows Hello (TPM-backed) key credential. This is the only
/// seam that touches the WinRT <c>KeyCredentialManager</c> APIs, so the rest of the
/// biometric code can be unit-tested with a fake.
/// </summary>
/// <remarks>
/// The security model relies on <see cref="SignAsync"/> being deterministic: Windows
/// Hello credentials are RSA keys and produce RSASSA-PKCS1-v1_5 signatures, so signing
/// the same challenge always yields the same bytes. We derive an encryption key from
/// that signature, which means the protected secret can only be recovered after a
/// successful Windows Hello gesture.
/// </remarks>
internal interface IBiometricKeyStore
{
	/// <summary>
	/// Whether Windows Hello is supported and configured on this device.
	/// </summary>
	Task<bool> IsAvailableAsync();

	/// <summary>
	/// Whether a credential with the given name already exists.
	/// </summary>
	Task<bool> ExistsAsync(string credentialName);

	/// <summary>
	/// Creates (or replaces) the named credential. May prompt the user for a Windows
	/// Hello gesture. Throws <see cref="BiometricException"/> on failure.
	/// </summary>
	Task CreateAsync(string credentialName);

	/// <summary>
	/// Signs <paramref name="challenge"/> with the named credential, prompting the user
	/// for a Windows Hello gesture. The returned signature is deterministic for a given
	/// challenge and credential. Throws <see cref="BiometricException"/> on failure.
	/// </summary>
	Task<byte[]> SignAsync(string credentialName, byte[] challenge);

	/// <summary>
	/// Deletes the named credential, if it exists.
	/// </summary>
	Task DeleteAsync(string credentialName);
}
