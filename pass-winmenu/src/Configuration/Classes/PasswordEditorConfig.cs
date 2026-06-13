using System;
using System.IO;
using PassWinmenu.WinApi;

namespace PassWinmenu.Configuration
{
	public class PasswordEditorConfig
	{
		public bool UseBuiltin { get; set; } = true;

		private string temporaryFileDirectory = Environment.ExpandEnvironmentVariables(@"%temp%");
		public string TemporaryFileDirectory
		{
			get => temporaryFileDirectory;
			set
			{
				var expanded = Environment.ExpandEnvironmentVariables(value);
				temporaryFileDirectory = Path.GetFullPath(PathUtilities.NormaliseDirectory(expanded));
			}
		}
	}
}
