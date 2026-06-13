using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PassWinmenu.Biometrics;
using PassWinmenu.Configuration;

#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	/// <summary>
	/// Simple wrapper over GPG.
	/// </summary>
	internal class GPG : ICryptoService, ISignService
	{
		private readonly IGpgTransport gpgTransport;
		private readonly IGpgResultVerifier gpgResultVerifier;
		private readonly AdditionalOptionsConfig additionalOptions;
		private readonly IPassphraseProvider? passphraseProvider;

		public GPG(IGpgTransport gpgTransport, IGpgResultVerifier gpgResultVerifier, GpgConfig gpgConfig, IPassphraseProvider? passphraseProvider = null)
		{
			this.gpgTransport = gpgTransport;
			this.gpgResultVerifier = gpgResultVerifier;
			this.passphraseProvider = passphraseProvider;
			additionalOptions = gpgConfig.AdditionalOptions;
		}

		/// <summary>
		/// Decrypt a file with GPG.
		/// </summary>
		/// <param name="file">The path to the file to be decrypted.</param>
		/// <returns>The contents of the decrypted file.</returns>
		/// <exception cref="GpgException">Thrown when decryption fails.</exception>
		public string Decrypt(string file)
		{
			// When a biometric passphrase provider supplies a passphrase, feed it to GPG via
			// loopback pinentry (passphrase on stdin / fd 0) instead of prompting. Otherwise
			// decrypt normally and let gpg-agent/pinentry (or a startup preset) handle it.
			var passphrase = passphraseProvider?.GetPassphrase();
			if (passphrase != null)
			{
				// Pass the passphrase as raw bytes on stdin (fd 0) so it never becomes an
				// un-zeroable string. The file is passed by path, so stdin is free for it.
				var passphraseBytes = ToStdinBytes(passphrase);
				try
				{
					var result = CallGpg(
						$"--pinentry-mode loopback --passphrase-fd 0 --decrypt {GpgArguments.Quote(file)}",
						operationArguments: additionalOptions.Decrypt,
						secretStdin: passphraseBytes);
					gpgResultVerifier.VerifyDecryption(result);
					return result.RawStdout;
				}
				catch (BadPassphraseException)
				{
					// The stored passphrase was rejected (e.g. the GPG passphrase changed). Discard
					// it (prompting re-enrolment) and fall back to a normal pinentry prompt.
					passphraseProvider!.Invalidate();
				}
				finally
				{
					Array.Clear(passphrase, 0, passphrase.Length);
					Array.Clear(passphraseBytes, 0, passphraseBytes.Length);
				}
			}

			var normalResult = CallGpg($"--decrypt {GpgArguments.Quote(file)}", null, additionalOptions.Decrypt);
			gpgResultVerifier.VerifyDecryption(normalResult);
			return normalResult.RawStdout;
		}

		/// <summary>
		/// Encrypt a string with GPG.
		/// </summary>
		/// <param name="data">The text to be encrypted.</param>
		/// <param name="outputFile">The path to the output file.</param>
		/// <param name="recipients">An array of GPG ids for which the file should be encrypted.</param>
		/// <exception cref="GpgException">Thrown when encryption fails.</exception>
		public void Encrypt(string data, string outputFile, bool allowOverwrite, params string[] recipients)
		{
			if (recipients == null)
			{
				recipients = Array.Empty<string>();
			}

			var recipientList = string.Join(" ", recipients.Select(r => $"--recipient {GpgArguments.Quote(r)}"));
			var overwrite = allowOverwrite ? "--yes " : "";

			var result = CallGpg($"{overwrite}--output {GpgArguments.Quote(outputFile)} --encrypt {recipientList}", data, additionalOptions.Encrypt);
			gpgResultVerifier.VerifyEncryption(result);
		}

		private void ListSecretKeys()
		{
			var result = CallGpg("--list-secret-keys");
			if (result.RawStdout.Length == 0)
			{
				throw new GpgError("No private keys found. Pass-winmenu will not be able to decrypt your passwords.");
			}
			// At some point in the future we might have a use for this data,
			// But for now, all we really use this method for is to ensure the GPG agent is started.
			//Log.Send("Secret key IDs: ");
			//Log.Send(result.Stdout);
		}

		public void StartAgent()
		{
			// Looking up a private key will start the GPG agent.
			ListSecretKeys();
		}

		public string GetVersion()
		{
			var output = CallGpg("--version");
			output.EnsureNonZeroExitCode();
			return output.StdoutMessages.First();
		}

		public string[] Sign(string message, string keyId)
		{
			var result = CallGpg($"--detach-sign --local-user {GpgArguments.Quote(keyId)} --armor", message, additionalOptions.Sign);
			return result.StdoutMessages;
		}

		public List<string> GetRecipients(string file)
		{
			var keys = CallGpg($"--list-only {GpgArguments.Quote(file)}");
			var recipients = keys.StatusMessages
				.Where(m => m.StatusCode == GpgStatusCode.ENC_TO)
				.Select(m => m.Message.Split(' ')[0]);

			return recipients.ToList();
		}

		public string? FindShortKeyId(string target)
		{
			var result = CallGpg($"--list-keys {GpgArguments.Quote(target)}");

			return result.StdoutMessages
				.Where(m => m.StartsWith("pub", StringComparison.Ordinal) || m.StartsWith("sub", StringComparison.Ordinal))
				.Select(l => l.Split(':'))
				.Where(l => l[11].Contains("e"))
				.Select(l => l[4])
				.FirstOrDefault();
		}

		private GpgResult CallGpg(string arguments, string? input = null, IDictionary<string, string>? operationArguments = null, byte[]? secretStdin = null)
		{
			var allOptions = (additionalOptions.Always ?? new Dictionary<string, string>())
				.Concat(operationArguments ?? new Dictionary<string, string>());
			if (allOptions.Any())
			{
				arguments = $"{string.Join(" ", allOptions.Select(FormatPair))} {arguments}";
			}

			return secretStdin != null
				? gpgTransport.CallGpgWithSecret(arguments, secretStdin)
				: gpgTransport.CallGpg(arguments, input);
		}

		private static string FormatPair(KeyValuePair<string, string> pair)
		{
			return "--" + (string.IsNullOrEmpty(pair.Value) ? pair.Key : $"{pair.Key} {GpgArguments.Quote(pair.Value)}");
		}

		/// <summary>
		/// Encodes a passphrase as the bytes gpg expects on stdin for <c>--passphrase-fd</c>
		/// (UTF-8, newline-terminated). The returned array is the caller's to clear.
		/// </summary>
		private static byte[] ToStdinBytes(char[] passphrase)
		{
			var encoded = Encoding.UTF8.GetBytes(passphrase);
			try
			{
				var bytes = new byte[encoded.Length + 1];
				Array.Copy(encoded, bytes, encoded.Length);
				bytes[encoded.Length] = (byte)'\n';
				return bytes;
			}
			finally
			{
				Array.Clear(encoded, 0, encoded.Length);
			}
		}
	}
}
