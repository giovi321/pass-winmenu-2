using System;
using System.Diagnostics;
using System.Text;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	/// <summary>
	/// Seeds gpg-agent's passphrase cache via <c>gpg-preset-passphrase --preset &lt;keygrip&gt;</c>.
	/// The passphrase is written to the tool's stdin as raw UTF-8 bytes (verified: literal
	/// passphrase, not hex). It is never placed on the command line.
	/// </summary>
	internal class GpgAgentPresetService : IGpgAgentPresetService
	{
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

		private readonly GpgInstallation installation;
		private readonly GpgHomeDirectory homeDirectory;
		private readonly IProcesses processes;

		public GpgAgentPresetService(GpgInstallation installation, GpgHomeDirectory homeDirectory, IProcesses processes)
		{
			this.installation = installation;
			this.homeDirectory = homeDirectory;
			this.processes = processes;
		}

		public PresetResult Preset(string keygrip, char[] passphrase)
		{
			var psi = new ProcessStartInfo
			{
				FileName = installation.GpgPresetPassphraseExecutable.FullName,
				Arguments = $"--preset {keygrip}",
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};

			// gpg-preset-passphrase locates the agent via GNUPGHOME, so we must point it at the
			// same home directory gpg uses when the home dir is overridden.
			if (homeDirectory.IsOverride)
			{
				psi.EnvironmentVariables["GNUPGHOME"] = homeDirectory.Path;
			}

			IProcess process;
			try
			{
				process = processes.Start(psi);
			}
			catch (Exception e)
			{
				Log.Send($"Could not run gpg-preset-passphrase: {e.GetType().Name}: {e.Message}", LogLevel.Warning);
				return PresetResult.Failed;
			}

			var bytes = Encoding.UTF8.GetBytes(passphrase);
			try
			{
				process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
				process.StandardInput.BaseStream.Flush();
				process.StandardInput.Close();
			}
			finally
			{
				Array.Clear(bytes, 0, bytes.Length);
			}

			var stderr = process.StandardError.ReadToEnd();
			process.WaitForExit(Timeout);

			if (process.ExitCode == 0)
			{
				return PresetResult.Success;
			}

			Log.Send($"gpg-preset-passphrase failed (exit {process.ExitCode}): {stderr.Trim()}", LogLevel.Warning);

			// "Not supported" is what gpg-agent returns when allow-preset-passphrase is missing.
			if (stderr.Contains("Not supported", StringComparison.OrdinalIgnoreCase)
				|| stderr.Contains("not allowed", StringComparison.OrdinalIgnoreCase))
			{
				return PresetResult.NotAllowed;
			}

			return PresetResult.Failed;
		}
	}
}
