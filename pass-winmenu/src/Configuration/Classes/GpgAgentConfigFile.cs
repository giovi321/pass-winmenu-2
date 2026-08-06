using System.Collections.Generic;

namespace PassWinmenu.Configuration
{
	public class GpgAgentConfigFile
	{
		public bool AllowConfigManagement { get; set; }

		/// <summary>
		/// Key/value pairs to write to gpg-agent.conf when <see cref="AllowConfigManagement"/>
		/// is enabled. Only whitelisted keys are written (see
		/// <c>GpgAgentConfigUpdater.AllowedKeys</c>); keys outside the whitelist, and keys or
		/// values containing CR/LF characters or starting with '#', are ignored with a warning.
		/// </summary>
		public Dictionary<string, string> Keys { get; set; } = new Dictionary<string, string>();
	}
}
