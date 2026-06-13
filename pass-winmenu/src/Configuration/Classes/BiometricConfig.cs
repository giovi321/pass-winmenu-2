namespace PassWinmenu.Configuration
{
	/// <summary>
	/// How often the user must authenticate with Windows Hello before passwords can be decrypted.
	/// </summary>
	public enum BiometricUnlockMode
	{
		/// <summary>Authenticate once at startup; gpg-agent caches the passphrase for the session.</summary>
		OncePerSession,

		/// <summary>Authenticate, then a short cache window (see <see cref="BiometricConfig.CacheSeconds"/>).</summary>
		Cache,

		/// <summary>Authenticate with a fresh gesture on every single decryption.</summary>
		EveryPassword,
	}

	/// <summary>
	/// Configuration for unlocking GPG-encrypted passwords with Windows Hello (fingerprint/PIN/face).
	/// Disabled by default; when enabled the user enrols once and their GPG passphrase is stored
	/// encrypted under a TPM-backed Windows Hello key.
	/// </summary>
	public class BiometricConfig
	{
		public bool Enabled { get; set; } = false;

		public BiometricUnlockMode Mode { get; set; } = BiometricUnlockMode.OncePerSession;

		/// <summary>Cache window in seconds; only used when <see cref="Mode"/> is <see cref="BiometricUnlockMode.Cache"/>.</summary>
		public int CacheSeconds { get; set; } = 600;

		/// <summary>Name of the Windows Hello credential pass-winmenu creates.</summary>
		public string CredentialName { get; set; } = "pass-winmenu-gpg";

		/// <summary>
		/// Optional key id whose keygrip should be preset into gpg-agent. When null, the
		/// encryption-capable secret key is auto-detected.
		/// </summary>
		public string? KeyId { get; set; } = null;
	}
}
