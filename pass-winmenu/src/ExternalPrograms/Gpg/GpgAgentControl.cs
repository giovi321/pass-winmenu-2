using System;
using System.Diagnostics;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgAgentControl : IGpgAgentControl
	{
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

		private readonly GpgInstallation installation;
		private readonly GpgHomeDirectory homeDirectory;
		private readonly IProcesses processes;

		public GpgAgentControl(GpgInstallation installation, GpgHomeDirectory homeDirectory, IProcesses processes)
		{
			this.installation = installation;
			this.homeDirectory = homeDirectory;
			this.processes = processes;
		}

		public void ReloadAgent()
		{
			// Launch starts the agent if it is not running; reload makes an already-running
			// agent re-read its configuration. Doing both is robust against either state.
			RunGpgConf("--launch gpg-agent");
			RunGpgConf("--reload gpg-agent");
		}

		private void RunGpgConf(string arguments)
		{
			var psi = new ProcessStartInfo
			{
				FileName = installation.GpgConfExecutable.FullName,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
			};

			if (homeDirectory.IsOverride)
			{
				psi.EnvironmentVariables["GNUPGHOME"] = homeDirectory.Path;
			}

			try
			{
				var process = processes.Start(psi);
				process.StandardError.ReadToEnd();
				process.WaitForExit(Timeout);
			}
			catch (Exception e)
			{
				Log.Send($"Could not run gpgconf {arguments}: {e.GetType().Name}: {e.Message}", LogLevel.Warning);
			}
		}
	}
}
