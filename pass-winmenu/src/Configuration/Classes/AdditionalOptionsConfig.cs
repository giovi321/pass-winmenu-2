using System.Collections.Generic;

namespace PassWinmenu.Configuration
{
	public class AdditionalOptionsConfig
	{
		public Dictionary<string, string> Always { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> Encrypt { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> Decrypt { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> Sign { get; set; } = new Dictionary<string, string>();
	}
}
