using System;
using Windows.Security.Credentials;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Thrown when a Windows Hello / biometric operation fails. Carries the underlying
/// <see cref="KeyCredentialStatus"/> when one is available, so callers can distinguish
/// e.g. a user cancellation from an invalidated credential.
/// </summary>
internal sealed class BiometricException : Exception
{
	public KeyCredentialStatus? Status { get; }

	public BiometricException(string message, KeyCredentialStatus? status = null)
		: base(message)
	{
		Status = status;
	}
}
