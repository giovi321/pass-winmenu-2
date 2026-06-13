namespace PassWinmenu.Configuration
{
	public class GpgAgentConfig
	{
		public bool Preload { get; set; } = true;
		public GpgAgentConfigFile Config { get; set; } = new GpgAgentConfigFile();
	}
}
