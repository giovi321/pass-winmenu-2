namespace PassWinmenu.Configuration
{
	/// <summary>Which password generator to use when generating a new password.</summary>
	public enum PasswordGenerationStyle
	{
		/// <summary>Random characters from the configured character groups (the original behaviour).</summary>
		Random,

		/// <summary>An XKCD-style passphrase made of several random dictionary words.</summary>
		Xkcd,
	}

	/// <summary>How each word in an XKCD-style passphrase is capitalised.</summary>
	public enum XkcdCapitalisation
	{
		None,
		First,
		Upper,
		Random,
	}

	/// <summary>
	/// Settings for the XKCD-style ("correct horse battery staple") passphrase generator.
	/// </summary>
	public class XkcdConfig
	{
		/// <summary>Number of words in the passphrase.</summary>
		public int WordCount { get; set; } = 4;

		/// <summary>String placed between words (and before an appended number).</summary>
		public string Separator { get; set; } = "-";

		/// <summary>
		/// When true, a freshly-generated random digit ('0'-'9') wraps every word — one before each
		/// word and one at the end — instead of using the fixed <see cref="Separator"/>, e.g.
		/// "8WORD1balcony9COLONY0". This overrides <see cref="Separator"/> and <see cref="IncludeNumber"/>.
		/// </summary>
		public bool RandomNumberSeparator { get; set; } = false;

		/// <summary>How each word is capitalised.</summary>
		public XkcdCapitalisation Capitalisation { get; set; } = XkcdCapitalisation.First;

		/// <summary>Only use words at least this many characters long.</summary>
		public int MinWordLength { get; set; } = 4;

		/// <summary>Only use words at most this many characters long.</summary>
		public int MaxWordLength { get; set; } = 9;

		/// <summary>Append a random two-digit number (useful for sites that require a digit).</summary>
		public bool IncludeNumber { get; set; } = false;

		/// <summary>
		/// Optional path to a custom word list (one word per line). When null or empty, the built-in
		/// EFF diceware word list is used.
		/// </summary>
		public string? WordListFile { get; set; } = null;
	}
}
