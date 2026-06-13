using System;
using PassWinmenu.Utilities.ExtensionMethods;
using PassWinmenu.WinApi;
using YamlDotNet.Serialization;

# nullable enable
namespace PassWinmenu.Configuration
{
	public class GitConfig
	{
		public bool UseGit { get; set; } = true;

		[YamlIgnore]
		public SyncMode SyncMode => (SyncMode)Enum.Parse(typeof(SyncMode), SyncModeString.ToPascalCase(), true);
		[YamlMember(Alias = "sync-mode")]
		public string SyncModeString { get; set; } = "auto";

		private string gitPath = "git";
		public string GitPath
		{
			get => gitPath;
			set
			{
				if (value == null)
				{
					gitPath = "git";
				}
				else
				{
					var expanded = Environment.ExpandEnvironmentVariables(value);
					gitPath = PathUtilities.NormaliseDirectory(expanded);
				}
			}
		}

		public string? SshPath { get; set; } = null;
		public bool AutoFetch { get; set; } = true;
		public double AutoFetchInterval { get; set; } = 3600;
		[YamlIgnore]
		public TimeSpan AutoFetchIntervalTimeSpan => TimeSpan.FromSeconds(AutoFetchInterval);

	}
}
