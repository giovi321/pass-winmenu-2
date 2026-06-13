using System.Diagnostics;
using PassWinmenu.Configuration;

namespace PassWinmenu.Actions
{
	internal class EditConfigurationAction : IAction
	{
		private readonly ConfigurationFile configFile;

		public EditConfigurationAction(ConfigurationFile configFile)
		{
			this.configFile = configFile;
		}

		public void Execute()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "explorer", 
				Arguments = configFile.Path,
			};
			Process.Start(startInfo);
		}

		public HotkeyAction ActionType => HotkeyAction.EditConfiguration;
	}
}
