using System.Text;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	/// <summary>
	/// Helpers for safely building GPG command-line argument strings.
	/// </summary>
	internal static class GpgArguments
	{
		/// <summary>
		/// Quotes a single argument value so it survives Windows CommandLineToArgvW parsing as one
		/// token, regardless of embedded spaces, double quotes or backslashes. This prevents
		/// argument injection when an operand (file path, recipient, key id, keygrip, home dir or a
		/// configured option value) contains crafted characters. The algorithm matches the one used
		/// internally by .NET's ProcessStartInfo.ArgumentList.
		/// </summary>
		public static string Quote(string argument)
		{
			var sb = new StringBuilder();
			sb.Append('"');

			var i = 0;
			while (i < argument.Length)
			{
				var c = argument[i++];
				if (c == '\\')
				{
					var backslashes = 1;
					while (i < argument.Length && argument[i] == '\\')
					{
						i++;
						backslashes++;
					}

					if (i == argument.Length)
					{
						// Backslashes immediately before the closing quote must be doubled.
						sb.Append('\\', backslashes * 2);
					}
					else if (argument[i] == '"')
					{
						// Backslashes before a literal quote are doubled, plus one to escape the quote.
						sb.Append('\\', backslashes * 2 + 1);
						sb.Append('"');
						i++;
					}
					else
					{
						// Backslashes not followed by a quote are literal.
						sb.Append('\\', backslashes);
					}
				}
				else if (c == '"')
				{
					sb.Append('\\');
					sb.Append('"');
				}
				else
				{
					sb.Append(c);
				}
			}

			sb.Append('"');
			return sb.ToString();
		}
	}
}
