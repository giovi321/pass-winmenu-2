using System; // brings in WindowsRuntimeSystemExtensions, the awaiters for IAsyncOperation/IAsyncAction
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;

namespace PassWinmenu.Biometrics;

/// <summary>
/// <see cref="IBiometricKeyStore"/> backed by the Windows Hello
/// <see cref="KeyCredentialManager"/>. The private key lives in the TPM and never
/// leaves it; signing requires a Windows Hello gesture (fingerprint/PIN/face).
/// </summary>
internal sealed class KeyCredentialBiometricKeyStore : IBiometricKeyStore
{
	public async Task<bool> IsAvailableAsync()
	{
		return await KeyCredentialManager.IsSupportedAsync();
	}

	public async Task<bool> ExistsAsync(string credentialName)
	{
		var result = await KeyCredentialManager.OpenAsync(credentialName);
		return result.Status == KeyCredentialStatus.Success;
	}

	public async Task CreateAsync(string credentialName)
	{
		using (WindowsHelloPrompt.Show("Setting up Windows Hello for Pass Winmenu 2…"))
		{
			AllowForegroundForHelloPrompt();
			var result = await KeyCredentialManager.RequestCreateAsync(
				credentialName,
				KeyCredentialCreationOption.ReplaceExisting);

			if (result.Status != KeyCredentialStatus.Success)
			{
				throw new BiometricException(
					$"Could not create a Windows Hello credential ({result.Status}).",
					result.Status);
			}
		}
	}

	public async Task<byte[]> SignAsync(string credentialName, byte[] challenge)
	{
		var opened = await KeyCredentialManager.OpenAsync(credentialName);
		if (opened.Status != KeyCredentialStatus.Success)
		{
			throw new BiometricException(
				$"Could not open the Windows Hello credential ({opened.Status}).",
				opened.Status);
		}

		var buffer = CryptographicBuffer.CreateFromByteArray(challenge);
		using (WindowsHelloPrompt.Show("Unlock your passwords with Windows Hello…"))
		{
			AllowForegroundForHelloPrompt();
			var signResult = await opened.Credential.RequestSignAsync(buffer);
			if (signResult.Status != KeyCredentialStatus.Success)
			{
				throw new BiometricException(
					$"Windows Hello authentication failed ({signResult.Status}).",
					signResult.Status);
			}

			CryptographicBuffer.CopyToByteArray(signResult.Result, out var signature);
			return signature;
		}
	}

	public async Task DeleteAsync(string credentialName)
	{
		await KeyCredentialManager.DeleteAsync(credentialName);
	}

	/// <summary>
	/// The Windows Hello prompt is drawn by a separate broker process. When pass-winmenu
	/// triggers it from a background thread (e.g. during a decrypt), Windows' foreground lock
	/// keeps the broker's window from taking focus, so it appears behind everything. Granting
	/// any process permission to set the foreground window lets the broker bring its prompt to
	/// the front. This only succeeds while we still hold foreground rights (we just handled a
	/// hotkey / showed a window), which is exactly when the prompt is shown.
	/// </summary>
	private static void AllowForegroundForHelloPrompt()
	{
		try
		{
			NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);
		}
		catch (DllNotFoundException)
		{
			// Non-Windows or a stripped environment: focus is best-effort, so ignore.
		}
	}

	private static class NativeMethods
	{
		public const int ASFW_ANY = -1;

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AllowSetForegroundWindow(int dwProcessId);
	}
}
