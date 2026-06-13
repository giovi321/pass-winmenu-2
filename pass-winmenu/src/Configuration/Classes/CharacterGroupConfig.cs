using System;
using System.Collections.Generic;
using PassWinmenu.Utilities.ExtensionMethods;

#nullable enable
namespace PassWinmenu.Configuration
{
	public class CharacterGroupConfig
	{
		public string Name { get; set; } = "No name set";
		public string Characters { get; set; } = string.Empty;
		public HashSet<int> CharacterSet => new(Characters.ToCodePoints());
		public bool Enabled { get; set; }

		public CharacterGroupConfig()
		{
		}

		public CharacterGroupConfig(string name, string characters, bool enabled)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Characters = characters ?? throw new ArgumentNullException(nameof(characters));
			Enabled = enabled;
		}
	}
}
