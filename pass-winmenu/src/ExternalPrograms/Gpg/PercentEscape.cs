using System.Globalization;
using System.Text.RegularExpressions;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal static class PercentEscape
	{
		private static readonly Regex PercentEscapeRegex = new Regex("%[0-9a-f]{2}", RegexOptions.Compiled);

		public static string UnEscape(string text)
		{
			return PercentEscapeRegex.Replace(text, match => Replace(match.Value));
		}

		private static string Replace(string hex)
		{
			var hexValue = hex.Substring(1);
			var numericValue = int.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

			return ((char)numericValue).ToString(CultureInfo.InvariantCulture);
		}
	}
}
