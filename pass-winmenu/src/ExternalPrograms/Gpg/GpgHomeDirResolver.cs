using System;
using System.Diagnostics;
using System.Linq;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities.ExtensionMethods;

#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgHomeDirResolver
	{
		private static readonly TimeSpan GpgConfTimeout = TimeSpan.FromSeconds(5);

		private readonly GpgConfig config;
		private readonly GpgInstallation installation;
		private readonly IProcesses processes;

		public GpgHomeDirResolver(GpgConfig config, GpgInstallation installation, IProcesses processes)
		{
			this.config = config;
			this.installation = installation;
			this.processes = processes;
		}

		/// <summary>
		/// Returns the path GPG will use as its home directory.
		/// </summary>
		public GpgHomeDirectory GetHomeDir()
		{
			if (config.GnupghomeOverride == null)
			{
				var defaultHomeDir = GetDefaultHomeDir();
				Log.Send($"Detected GPG home directory: \"{defaultHomeDir}\"");
				return new GpgHomeDirectory(defaultHomeDir);
			}
			else
			{
				Log.Send($"Using override for GPG home directory: \"{config.GnupghomeOverride}\"");
				return new GpgHomeDirectory(config.GnupghomeOverride, true);
			}
		}

		/// <summary>
		/// Returns the default home directory used by GPG when no user-defined home directory is available.
		/// </summary>
		private string GetDefaultHomeDir()
		{
			var psi = new ProcessStartInfo(installation.GpgConfExecutable.FullName, "--list-dirs")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			var gpgConf = processes.Start(psi);

			var lines = gpgConf.StandardOutput.ReadAllLines();
			gpgConf.WaitForExit(GpgConfTimeout);

			const string homeDirKey = "homedir:";

			var homeDirLine = lines.FirstOrDefault(l => l.StartsWith(homeDirKey, StringComparison.OrdinalIgnoreCase));
			if (homeDirLine == null)
			{
				throw new Exception("Could not determine home directory by querying gpgconf.");
			}

			return PercentEscape.UnEscape(homeDirLine[homeDirKey.Length..]);
		}
	}
}
