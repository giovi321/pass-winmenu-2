using System; // brings in WindowsRuntimeSystemExtensions, the awaiters for IAsyncOperation/IAsyncAction
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
		var result = await WindowsHelloPrompt.RunAsync(
			"Setting up Windows Hello for Pass Winmenu 2…",
			_ => KeyCredentialManager.RequestCreateAsync(
				credentialName,
				KeyCredentialCreationOption.ReplaceExisting).AsTask());

		if (result.Status != KeyCredentialStatus.Success)
		{
			throw new BiometricException(
				$"Could not create a Windows Hello credential ({result.Status}).",
				result.Status);
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
		var signResult = await WindowsHelloPrompt.RunAsync(
			"Unlock your passwords with Windows Hello…",
			ct => opened.Credential.RequestSignAsync(buffer).AsTask(ct));

		if (signResult.Status != KeyCredentialStatus.Success)
		{
			throw new BiometricException(
				$"Windows Hello authentication failed ({signResult.Status}).",
				signResult.Status);
		}

		CryptographicBuffer.CopyToByteArray(signResult.Result, out var signature);
		return signature;
	}

	public async Task DeleteAsync(string credentialName)
	{
		await KeyCredentialManager.DeleteAsync(credentialName);
	}
}
