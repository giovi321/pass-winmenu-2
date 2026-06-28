namespace PassWinmenu.Configuration
{
	/// <summary>Where extra special characters are inserted into a generated password.</summary>
	public enum SpecialCharacterPlacement
	{
		/// <summary>Append at the end (e.g. "password!").</summary>
		End,

		/// <summary>Prepend at the start (e.g. "!password").</summary>
		Start,

		/// <summary>Insert at random positions within the password.</summary>
		Random,
	}

	/// <summary>
	/// Optional rule for adding special characters to a generated password, to satisfy services with
	/// arbitrary "must contain a special character" policies. Disabled by default; the generate window
	/// exposes a toggle, and these values configure what the toggle does.
	/// </summary>
	public class SpecialCharacterConfig
	{
		/// <summary>Default state of the in-window toggle.</summary>
		public bool Enabled { get; set; } = false;

		/// <summary>Pool of characters to draw from when adding special characters.</summary>
		public string Characters { get; set; } = "!@#$%^&*";

		/// <summary>How many characters to add.</summary>
		public int Count { get; set; } = 1;

		/// <summary>Where the characters are inserted.</summary>
		public SpecialCharacterPlacement Placement { get; set; } = SpecialCharacterPlacement.End;
	}
}
