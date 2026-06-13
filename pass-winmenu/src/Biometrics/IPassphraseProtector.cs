namespace PassWinmenu.Biometrics;

/// <summary>
/// Encrypts and decrypts the GPG passphrase using a key derived from a Windows Hello
/// signature. Pure crypto: it does not touch WinRT or the filesystem, so it is fully
/// unit-testable. The derived key is bound to biometry + TPM because the signature can
/// only be produced after a successful Windows Hello gesture.
/// </summary>
internal interface IPassphraseProtector
{
	/// <summary>
	/// Encrypts <paramref name="passphrase"/> using a key derived from <paramref name="signature"/>.
	/// Returns a self-contained blob (nonce + tag + ciphertext).
	/// </summary>
	byte[] Protect(byte[] signature, char[] passphrase);

	/// <summary>
	/// Decrypts a blob produced by <see cref="Protect"/>. Throws
	/// <see cref="System.Security.Cryptography.CryptographicException"/> when the signature
	/// does not match (e.g. the Windows Hello key changed) or the blob is corrupt.
	/// </summary>
	char[] Unprotect(byte[] signature, byte[] blob);
}
