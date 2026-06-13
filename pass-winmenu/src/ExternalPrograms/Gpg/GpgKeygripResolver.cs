using System;
using System.Linq;
using PassWinmenu.Configuration;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	/// <summary>
	/// Resolves a secret key's keygrip by parsing <c>gpg --list-secret-keys --with-keygrip
	/// --with-colons</c>. The colon format emits a <c>grp</c> record after each <c>sec</c>/<c>ssb</c>
	/// record; field 11 of the key record holds the per-key capability letters (lowercase
	/// <c>e</c> = encryption). The encryption subkey's keygrip is the one gpg-agent must unlock
	/// to decrypt a password file.
	/// </summary>
	internal class GpgKeygripResolver : IGpgKeygripResolver
	{
		private readonly IGpgTransport gpgTransport;

		public GpgKeygripResolver(IGpgTransport gpgTransport)
		{
			this.gpgTransport = gpgTransport;
		}

		public string? GetEncryptionKeygrip(string? keyId = null)
		{
			var arguments = "--list-secret-keys --with-keygrip";
			if (!string.IsNullOrEmpty(keyId))
			{
				// Defence in depth: a key id should never contain quotes or control characters.
				// (Quote() below already prevents argument injection regardless.)
				if (keyId.Any(c => c == '"' || char.IsControl(c)))
				{
					throw new ConfigurationException("The configured gpg key-id contains invalid characters.");
				}

				arguments += $" {GpgArguments.Quote(keyId)}";
			}

			var result = gpgTransport.CallGpg(arguments);
			return ParseEncryptionKeygrip(result.RawStdout);
		}

		internal static string? ParseEncryptionKeygrip(string colonOutput)
		{
			string? capabilities = null;
			string? fallbackKeygrip = null;

			foreach (var rawLine in colonOutput.Split('\n'))
			{
				// Tolerate CRLF: split on '\n' can leave a trailing '\r' on each field.
				var fields = rawLine.TrimEnd('\r').Split(':');
				switch (fields[0])
				{
					case "sec":
					case "ssb":
						// Field 11 (index 11) holds the capability flags for this (sub)key.
						capabilities = fields.Length > 11 ? fields[11] : string.Empty;
						break;
					case "grp":
						// Field 9 (index 9) holds the keygrip.
						var keygrip = fields.Length > 9 ? fields[9] : string.Empty;
						if (keygrip.Length == 0)
						{
							break;
						}

						// Case-sensitive: lowercase 'e' is this key's encryption capability
						// (uppercase letters describe the whole key, not this subkey).
						if (capabilities != null && capabilities.Contains('e', StringComparison.Ordinal))
						{
							return keygrip;
						}

						fallbackKeygrip ??= keygrip;
						break;
				}
			}

			return fallbackKeygrip;
		}
	}
}
