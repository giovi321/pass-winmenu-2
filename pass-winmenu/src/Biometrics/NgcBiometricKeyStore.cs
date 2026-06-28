using System;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PassWinmenu.Configuration;
using Windows.Security.Credentials;

#nullable enable
namespace PassWinmenu.Biometrics;

/// <summary>
/// <see cref="IBiometricKeyStore"/> backed by the Windows Hello "Microsoft Passport Key Storage
/// Provider" (NGC) via the NCrypt/CNG API. Unlike the WinRT <c>KeyCredentialManager</c>, NCrypt lets
/// us set <c>NCRYPT_WINDOW_HANDLE_PROPERTY</c> so the Hello gesture prompt is parented to our window
/// and therefore focused — essential for a tray app triggered by a global hotkey, where the prompt
/// otherwise opens unfocused and ignores the fingerprint sensor.
/// </summary>
/// <remarks>
/// The TPM-backed RSA key never leaves the TPM. At enrolment we generate a random secret, encrypt it
/// with the key (no gesture) and store the ciphertext next to the config; unlocking decrypts that
/// ciphertext, which requires a Hello gesture (<c>NgcCacheType = AUTH_MANDATORY</c> +
/// <c>PinCacheIsGestureRequired</c>). RSA decryption is deterministic, so <see cref="SignAsync"/>
/// returns the same secret every time and the existing AES-GCM passphrase protector keeps working
/// unchanged. The recovered secret is the only key-derivation input, so the wrapped secret on disk is
/// useless without a successful Windows Hello gesture on this machine's TPM.
/// </remarks>
internal sealed class NgcBiometricKeyStore : IBiometricKeyStore
{
	private const string SecretFileName = "biometric.hellokey";
	private const int SecretLength = 32;

	private readonly IFileSystem fileSystem;
	private readonly string secretPath;

	public NgcBiometricKeyStore(IFileSystem fileSystem, ConfigurationFile configurationFile)
	{
		this.fileSystem = fileSystem;

		var directory = fileSystem.Path.GetDirectoryName(configurationFile.Path);
		secretPath = string.IsNullOrEmpty(directory)
			? SecretFileName
			: fileSystem.Path.Combine(directory, SecretFileName);
	}

	public async Task<bool> IsAvailableAsync()
	{
		return await KeyCredentialManager.IsSupportedAsync();
	}

	public Task<bool> ExistsAsync(string credentialName)
	{
		if (!fileSystem.File.Exists(secretPath))
		{
			return Task.FromResult(false);
		}

		if (NativeMethods.NCryptOpenStorageProvider(out var provider, NativeMethods.MS_NGC_KEY_STORAGE_PROVIDER, 0) < 0)
		{
			return Task.FromResult(false);
		}

		using (provider)
		{
			if (NativeMethods.NCryptOpenKey(provider, out var key, NgcKeyName(credentialName), 0, CngKeyOpenOptions.Silent) < 0)
			{
				return Task.FromResult(false);
			}

			key.Dispose();
			return Task.FromResult(true);
		}
	}

	public Task CreateAsync(string credentialName)
	{
		using var prompt = WindowsHelloPrompt.Show("Setting up Windows Hello for Pass Winmenu 2…");

		CreatePersistedKey(credentialName, prompt.Handle);

		// Wrap a fresh random secret with the new key and store the ciphertext. Encryption needs no
		// gesture; only the later decryption (unlock) does.
		var secret = RandomNumberGenerator.GetBytes(SecretLength);
		try
		{
			var ciphertext = Encrypt(credentialName, secret);
			fileSystem.File.WriteAllBytes(secretPath, ciphertext);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(secret);
		}

		return Task.CompletedTask;
	}

	public Task<byte[]> SignAsync(string credentialName, byte[] challenge)
	{
		if (!fileSystem.File.Exists(secretPath))
		{
			throw new BiometricException("Windows Hello unlock is not set up.", KeyCredentialStatus.NotFound);
		}

		var ciphertext = fileSystem.File.ReadAllBytes(secretPath);

		using var prompt = WindowsHelloPrompt.Show("Unlock your passwords with Windows Hello…");
		var secret = Decrypt(credentialName, ciphertext, prompt.Handle);
		return Task.FromResult(secret);
	}

	public Task DeleteAsync(string credentialName)
	{
		TryDeleteKey(credentialName);

		if (fileSystem.File.Exists(secretPath))
		{
			fileSystem.File.Delete(secretPath);
		}

		return Task.CompletedTask;
	}

	/// <summary>Deletes the named NGC key if it exists. Best-effort: ignores all failures.</summary>
	private static void TryDeleteKey(string credentialName)
	{
		if (NativeMethods.NCryptOpenStorageProvider(out var provider, NativeMethods.MS_NGC_KEY_STORAGE_PROVIDER, 0) < 0)
		{
			return;
		}

		using (provider)
		{
			if (NativeMethods.NCryptOpenKey(provider, out var key, NgcKeyName(credentialName), 0, CngKeyOpenOptions.Silent) < 0)
			{
				return;
			}

			// NCryptDeleteKey frees the handle on success, so stop the SafeHandle from freeing it again.
			if (NativeMethods.NCryptDeleteKey(key, 0) >= 0)
			{
				key.SetHandleAsInvalid();
			}
			else
			{
				key.Dispose();
			}
		}
	}

	private static void CreatePersistedKey(string credentialName, IntPtr parentWindow)
	{
		// Remove any existing key with this name first — including one left by the previous
		// KeyCredentialManager enrolment — then create a fresh key. Overwriting in place
		// (NCRYPT_OVERWRITE_KEY_FLAG) fails with NTE_INVALID_PARAMETER on the Passport provider.
		TryDeleteKey(credentialName);

		Check(NativeMethods.NCryptOpenStorageProvider(out var provider, NativeMethods.MS_NGC_KEY_STORAGE_PROVIDER, 0), "open the Windows Hello provider");
		using (provider)
		{
			Check(
				NativeMethods.NCryptCreatePersistedKey(provider, out var key, NativeMethods.BCRYPT_RSA_ALGORITHM, NgcKeyName(credentialName), 0, CngKeyCreationOptions.None),
				"create the Windows Hello key");
			using (key)
			{
				var length = BitConverter.GetBytes(2048);
				Check(NativeMethods.NCryptSetProperty(key, NativeMethods.NCRYPT_LENGTH_PROPERTY, length, length.Length, CngPropertyOptions.None), "set the key length");

				var usage = BitConverter.GetBytes(NativeMethods.NCRYPT_ALLOW_DECRYPT_FLAG);
				Check(NativeMethods.NCryptSetProperty(key, NativeMethods.NCRYPT_KEY_USAGE_PROPERTY, usage, usage.Length, CngPropertyOptions.None), "set the key usage");

				// Force a Hello gesture on every use of the key (instead of caching the unlock).
				var cacheType = BitConverter.GetBytes(NativeMethods.NGC_CACHE_TYPE_AUTH_MANDATORY);
				if (NativeMethods.NCryptSetProperty(key, NativeMethods.NCRYPT_NGC_CACHE_TYPE_PROPERTY, cacheType, cacheType.Length, CngPropertyOptions.None) < 0)
				{
					Check(
						NativeMethods.NCryptSetProperty(key, NativeMethods.NCRYPT_NGC_CACHE_TYPE_PROPERTY_DEPRECATED, cacheType, cacheType.Length, CngPropertyOptions.None),
						"set the gesture requirement");
				}

				SetWindowHandle(key, parentWindow);

				Check(NativeMethods.NCryptFinalizeKey(key, 0), "finalise the Windows Hello key");
			}
		}
	}

	private static byte[] Encrypt(string credentialName, byte[] data)
	{
		Check(NativeMethods.NCryptOpenStorageProvider(out var provider, NativeMethods.MS_NGC_KEY_STORAGE_PROVIDER, 0), "open the Windows Hello provider");
		using (provider)
		{
			// Silent: wrapping the secret needs no gesture, only the later unlock does.
			Check(NativeMethods.NCryptOpenKey(provider, out var key, NgcKeyName(credentialName), 0, CngKeyOpenOptions.Silent), "open the Windows Hello key");
			using (key)
			{
				Check(NativeMethods.NCryptEncrypt(key, data, data.Length, IntPtr.Zero, null, 0, out var size, NativeMethods.NCRYPT_PAD_PKCS1_FLAG), "measure the wrapped secret");

				var output = new byte[size];
				Check(NativeMethods.NCryptEncrypt(key, data, data.Length, IntPtr.Zero, output, output.Length, out size, NativeMethods.NCRYPT_PAD_PKCS1_FLAG), "wrap the secret");

				if (size != output.Length)
				{
					Array.Resize(ref output, size);
				}

				return output;
			}
		}
	}

	private static byte[] Decrypt(string credentialName, byte[] ciphertext, IntPtr parentWindow)
	{
		Check(NativeMethods.NCryptOpenStorageProvider(out var provider, NativeMethods.MS_NGC_KEY_STORAGE_PROVIDER, 0), "open the Windows Hello provider");
		using (provider)
		{
			Check(NativeMethods.NCryptOpenKey(provider, out var key, NgcKeyName(credentialName), 0, CngKeyOpenOptions.None), "open the Windows Hello key");
			using (key)
			{
				SetWindowHandle(key, parentWindow);

				var gestureRequired = BitConverter.GetBytes(1);
				Check(
					NativeMethods.NCryptSetProperty(key, NativeMethods.NCRYPT_PIN_CACHE_IS_GESTURE_REQUIRED_PROPERTY, gestureRequired, gestureRequired.Length, CngPropertyOptions.None),
					"require a Windows Hello gesture");

				// One call only: the gesture prompt fires here, so a size-probe call would prompt twice.
				// RSA plaintext is always smaller than the ciphertext, so the ciphertext length is a
				// safe buffer size.
				var output = new byte[ciphertext.Length];
				Check(
					NativeMethods.NCryptDecrypt(key, ciphertext, ciphertext.Length, IntPtr.Zero, output, output.Length, out var size, NativeMethods.NCRYPT_PAD_PKCS1_FLAG),
					"unlock with Windows Hello");

				if (size != output.Length)
				{
					Array.Resize(ref output, size);
				}

				return output;
			}
		}
	}

	private static void SetWindowHandle(SafeNCryptKeyHandle key, IntPtr parentWindow)
	{
		if (parentWindow == IntPtr.Zero)
		{
			return;
		}

		var handle = IntPtr.Size == 8
			? BitConverter.GetBytes(parentWindow.ToInt64())
			: BitConverter.GetBytes(parentWindow.ToInt32());

		// Best-effort: parenting the prompt to our window is what focuses it, but some configurations
		// reject the property — don't fail the unlock over it.
		NativeMethods.NCryptSetProperty(key, NativeMethods.NCRYPT_WINDOW_HANDLE_PROPERTY, handle, handle.Length, CngPropertyOptions.None);
	}

	/// <summary>
	/// Builds the NGC key name in the form the Passport provider requires:
	/// <c>{userSID}//{domain}/{subdomain}/{name}</c>. A plain name is rejected with
	/// <c>NTE_INVALID_PARAMETER</c>. (Same scheme as KeePassWinHello.)
	/// </summary>
	private static string NgcKeyName(string credentialName)
	{
		using var identity = WindowsIdentity.GetCurrent();
		var sid = identity.User?.Value
			?? throw new BiometricException("Could not determine the current Windows user account.");

		const string domain = "PassWinmenu";
		const string subDomain = "";
		return $"{sid}//{domain}/{subDomain}/{credentialName}";
	}

	private static void Check(int status, string operation)
	{
		if (status >= 0)
		{
			return;
		}

		throw status switch
		{
			NativeMethods.NTE_USER_CANCELLED or NativeMethods.ERROR_CANCELLED =>
				new BiometricException("Windows Hello was cancelled.", KeyCredentialStatus.UserCanceled),
			NativeMethods.NTE_NO_KEY or NativeMethods.NTE_BAD_KEYSET =>
				new BiometricException("The Windows Hello credential was not found.", KeyCredentialStatus.NotFound),
			_ => new BiometricException($"Windows Hello failed while trying to {operation} (0x{status:X8})."),
		};
	}

	private static class NativeMethods
	{
		public const string MS_NGC_KEY_STORAGE_PROVIDER = "Microsoft Passport Key Storage Provider";
		public const string BCRYPT_RSA_ALGORITHM = "RSA";
		public const string NCRYPT_LENGTH_PROPERTY = "Length";
		public const string NCRYPT_KEY_USAGE_PROPERTY = "Key Usage";
		public const string NCRYPT_NGC_CACHE_TYPE_PROPERTY = "NgcCacheType";
		public const string NCRYPT_NGC_CACHE_TYPE_PROPERTY_DEPRECATED = "NgcCacheTypeProperty";
		public const string NCRYPT_WINDOW_HANDLE_PROPERTY = "HWND Handle";
		public const string NCRYPT_PIN_CACHE_IS_GESTURE_REQUIRED_PROPERTY = "PinCacheIsGestureRequired";

		public const int NCRYPT_ALLOW_DECRYPT_FLAG = 0x00000001;
		public const int NGC_CACHE_TYPE_AUTH_MANDATORY = 0x00000001;
		public const int NCRYPT_PAD_PKCS1_FLAG = 0x00000002;

		public const int NTE_USER_CANCELLED = unchecked((int)0x80090036);
		public const int NTE_NO_KEY = unchecked((int)0x8009000D);
		public const int NTE_BAD_KEYSET = unchecked((int)0x80090016);
		public const int ERROR_CANCELLED = unchecked((int)0x800704C7);

		[DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
		public static extern int NCryptOpenStorageProvider(out SafeNCryptProviderHandle phProvider, string pszProviderName, int dwFlags);

		[DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
		public static extern int NCryptOpenKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszKeyName, int dwLegacyKeySpec, CngKeyOpenOptions dwFlags);

		[DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
		public static extern int NCryptCreatePersistedKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszAlgId, string pszKeyName, int dwLegacyKeySpec, CngKeyCreationOptions dwFlags);

		[DllImport("ncrypt.dll")]
		public static extern int NCryptFinalizeKey(SafeNCryptKeyHandle hKey, int dwFlags);

		[DllImport("ncrypt.dll")]
		public static extern int NCryptDeleteKey(SafeNCryptKeyHandle hKey, int dwFlags);

		[DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
		public static extern int NCryptSetProperty(SafeNCryptHandle hObject, string pszProperty, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput, int cbInput, CngPropertyOptions dwFlags);

		[DllImport("ncrypt.dll")]
		public static extern int NCryptEncrypt(SafeNCryptKeyHandle hKey, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput, int cbInput, IntPtr pPaddingInfo, [Out, MarshalAs(UnmanagedType.LPArray)] byte[]? pbOutput, int cbOutput, out int pcbResult, int dwFlags);

		[DllImport("ncrypt.dll")]
		public static extern int NCryptDecrypt(SafeNCryptKeyHandle hKey, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput, int cbInput, IntPtr pPaddingInfo, [Out, MarshalAs(UnmanagedType.LPArray)] byte[]? pbOutput, int cbOutput, out int pcbResult, int dwFlags);
	}
}
