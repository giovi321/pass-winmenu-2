namespace PassWinmenu.Configuration
{
	public class PasswordGenerationConfig
	{
		/// <summary>Which generator to use: random characters or an XKCD-style passphrase.</summary>
		public PasswordGenerationStyle Style { get; set; } = PasswordGenerationStyle.Random;

		/// <summary>Settings for the XKCD-style passphrase generator (used when <see cref="Style"/> is Xkcd).</summary>
		public XkcdConfig Xkcd { get; set; } = new XkcdConfig();

		public int Length { get; set; } = 20;
		public CharacterGroupConfig[] CharacterGroups { get; set; } =
		{
			new("Symbols", "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~", true),
			new("Numeric", "0123456789", true),
			new("Lowercase", "abcdefghijklmnopqrstuvwxyz", true),
			new("Uppercase", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", true),
			new("Whitespace", " ", false), 
		};
		public string DefaultContent { get; set; } = "Username: \n";
	}
}
