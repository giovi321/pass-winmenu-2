using System;

namespace PassWinmenu.Configuration
{
	/// <summary>
	/// Configuration for unlocking GPG-encrypted passwords with Windows Hello (fingerprint/PIN/face).
	/// Disabled by default; when enabled the user enrols once and their GPG passphrase is stored
	/// encrypted under a TPM-backed Windows Hello key. How often Windows Hello is requested is
	/// governed solely by gpg-agent's own passphrase cache (its <c>default-cache-ttl</c> /
	/// <c>max-cache-ttl</c>): pass-winmenu probes that cache and only prompts for Windows Hello on a
	/// miss, failing the decryption closed if the gesture is declined.
	/// </summary>
	public class BiometricConfig
	{
		public bool Enabled { get; set; } = false;

		/// <summary>Name of the Windows Hello credential pass-winmenu creates.</summary>
		public string CredentialName { get; set; } = "pass-winmenu-gpg";

		/// <summary>
		/// Optional id of the GPG key whose passphrase is being unlocked. When null, the
		/// encryption-capable secret key is auto-detected.
		/// </summary>
		public string? KeyId { get; set; } = null;

		[Obsolete("The biometric unlock 'mode' was removed; the unlock cadence is now governed " +
			"solely by gpg-agent's passphrase cache. This property is retained only so older " +
			"configuration files still deserialise successfully.", true)]
		public string? Mode { get; set; }

		[Obsolete("The biometric 'cache-seconds' setting was removed; the unlock cadence is now " +
			"governed solely by gpg-agent's passphrase cache. This property is retained only so " +
			"older configuration files still deserialise successfully.", true)]
		public int CacheSeconds { get; set; }
	}
}
