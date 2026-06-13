using System;
using System.IO;
using System.IO.Abstractions;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgAgentConfigReader : IGpgAgentConfigReader
	{
		private const string GpgAgentConfigFileName = "gpg-agent.conf";

		private readonly IFileSystem fileSystem;
		private readonly GpgHomeDirectory homeDirectory;

		public GpgAgentConfigReader(IFileSystem fileSystem, GpgHomeDirectory homeDirectory)
		{
			this.fileSystem = fileSystem;
			this.homeDirectory = homeDirectory;
		}

		public string[] ReadConfigLines()
		{
			var homeDir = GetHomeDir();
			var agentConf = fileSystem.Path.Combine(homeDir, GpgAgentConfigFileName);

			if (fileSystem.File.Exists(agentConf))
			{
				return fileSystem.File.ReadAllLines(agentConf);
			}

			using (fileSystem.File.Create(agentConf))
			{
				return Array.Empty<string>();
			}
		}

		public void WriteConfigLines(string[] lines)
		{
			var homeDir = GetHomeDir();

			var agentConf = fileSystem.Path.Combine(homeDir, GpgAgentConfigFileName);

			fileSystem.File.WriteAllLines(agentConf, lines);
		}

		private string GetHomeDir()
		{
			if (fileSystem.Directory.Exists(homeDirectory.Path))
			{
				return homeDirectory.Path;
			}

			throw new DirectoryNotFoundException("GPG Homedir does not exist");
		}
	}
}
