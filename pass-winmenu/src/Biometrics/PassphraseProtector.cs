using System;
using System.Security.Cryptography;
using System.Text;

namespace PassWinmenu.Biometrics;

/// <summary>
/// AES-256-GCM implementation of <see cref="IPassphraseProtector"/>. The AES key is the
/// SHA-256 of the Windows Hello signature; the blob layout is
/// <c>[12-byte nonce][16-byte tag][ciphertext]</c>.
/// </summary>
internal sealed class PassphraseProtector : IPassphraseProtector
{
	private const int NonceSize = 12; // AES-GCM standard nonce length
	private const int TagSize = 16;   // AES-GCM authentication tag length

	public byte[] Protect(byte[] signature, char[] passphrase)
	{
		var key = DeriveKey(signature);
		var plaintext = Encoding.UTF8.GetBytes(passphrase);
		try
		{
			var nonce = RandomNumberGenerator.GetBytes(NonceSize);
			var ciphertext = new byte[plaintext.Length];
			var tag = new byte[TagSize];

			using (var aes = new AesGcm(key))
			{
				aes.Encrypt(nonce, plaintext, ciphertext, tag);
			}

			var blob = new byte[NonceSize + TagSize + ciphertext.Length];
			Buffer.BlockCopy(nonce, 0, blob, 0, NonceSize);
			Buffer.BlockCopy(tag, 0, blob, NonceSize, TagSize);
			Buffer.BlockCopy(ciphertext, 0, blob, NonceSize + TagSize, ciphertext.Length);
			return blob;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(key);
			CryptographicOperations.ZeroMemory(plaintext);
		}
	}

	public char[] Unprotect(byte[] signature, byte[] blob)
	{
		if (blob.Length < NonceSize + TagSize)
		{
			throw new CryptographicException("The stored biometric blob is too short to be valid.");
		}

		var key = DeriveKey(signature);
		var plaintext = new byte[blob.Length - NonceSize - TagSize];
		try
		{
			var nonce = new byte[NonceSize];
			var tag = new byte[TagSize];
			var ciphertext = new byte[blob.Length - NonceSize - TagSize];
			Buffer.BlockCopy(blob, 0, nonce, 0, NonceSize);
			Buffer.BlockCopy(blob, NonceSize, tag, 0, TagSize);
			Buffer.BlockCopy(blob, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

			using (var aes = new AesGcm(key))
			{
				// Throws AuthenticationTagMismatchException (a CryptographicException) when the
				// key (i.e. the Hello signature) is wrong or the blob was tampered with.
				aes.Decrypt(nonce, ciphertext, tag, plaintext);
			}

			return Encoding.UTF8.GetChars(plaintext);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(key);
			CryptographicOperations.ZeroMemory(plaintext);
		}
	}

	private static byte[] DeriveKey(byte[] signature)
	{
		// SHA-256 yields a 32-byte (AES-256) key from the variable-length Hello signature.
		return SHA256.HashData(signature);
	}
}
