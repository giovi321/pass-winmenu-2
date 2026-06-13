using System;
using System.Text.RegularExpressions;
using OtpNet;
using PassWinmenu.Utilities;

#nullable enable
namespace PassWinmenu.PasswordManagement
{
	internal class TotpGenerator
	{
		private static readonly Regex OtpSecretRegex = new Regex("secret=([a-zA-Z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static Option<string> GenerateTotpCode(KeyedPasswordFile passwordFile, DateTime timestamp)
		{
			string? secretKey = null;
			foreach (var k in passwordFile.Keys)
			{
				// TOTP: Xxxx4
				if (k.Key.ToUpperInvariant() == "TOTP")
				{
					secretKey = k.Value;
				}

				// otpauth://totp/account?secret=FxxxX&digits=6
				if (k.Key.ToUpperInvariant() == "OTPAUTH" || k.Key.ToUpperInvariant() == "OTP-AUTH")
				{
					var match = OtpSecretRegex.Match(k.Value);
					if (match.Success) {
						secretKey = match.Groups[1].Value;
					}
				}
			}

			if (secretKey == null)
			{
				return Option.None<string>();
			}

			var secretKeyBytes = Base32Encoding.ToBytes(secretKey);
			var totp = new Totp(secretKeyBytes);
			var totpCode = totp.ComputeTotp(timestamp);

			return Option.Some(totpCode);
		}
	}
}
