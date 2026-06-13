using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class StdErrResult
	{
		public readonly List<StatusMessage> StatusMessages;
		public readonly List<string> StdErrMessages;

		public StdErrResult(List<StatusMessage> statusMessages, List<string> stdErrMessages)
		{
			StatusMessages = statusMessages;
			StdErrMessages = stdErrMessages;
		}
	}

	internal class GpgTransport : IGpgTransport
	{
		private const string StatusMarker = "[GNUPG:] ";

		private readonly TimeSpan gpgCallTimeout = TimeSpan.FromSeconds(5);
		private readonly GpgHomeDirectory homeDirectory;
		private readonly GpgInstallation installation;
		private readonly IProcesses processes;

		public GpgTransport(GpgHomeDirectory homeDirectory, GpgInstallation installation, IProcesses processes)
		{
			this.homeDirectory = homeDirectory;
			this.installation = installation;
			this.processes = processes;
		}

		public GpgResult CallGpg(string arguments, string? input = null)
		{
			return Execute(arguments, input, null);
		}

		public GpgResult CallGpgWithSecret(string arguments, byte[] secretStdin)
		{
			return Execute(arguments, null, secretStdin);
		}

		private GpgResult Execute(string arguments, string? input, byte[]? secretStdin)
		{
			var gpgProc = CreateGpgProcess(arguments, input, secretStdin);

			var stdErrTask = Task.Run(() => ReadStdErr(gpgProc));
			var stdOutTask = Task.Run(() => ReadStdout(gpgProc));

			Task.WaitAll(stdErrTask, stdOutTask);
			gpgProc.WaitForExit(gpgCallTimeout);

			var output = stdOutTask.Result;
			var status = stdErrTask.Result;

			return new GpgResult(gpgProc.ExitCode, output, status.StatusMessages, status.StdErrMessages);
		}

		private string ReadStdout(IProcess gpgProc)
		{
			// We can use the standard UTF-8 encoding here, as it should be able to handle input without BOM.
			using var reader = new StreamReader(gpgProc.StandardOutput.BaseStream, Encoding.UTF8);
			return reader.ReadToEnd();
		}

		private StdErrResult ReadStdErr(IProcess gpgProc)
		{
			string? stderrLine;
			var stderrMessages = new List<string>();
			var statusMessages = new List<StatusMessage>();

			while ((stderrLine = gpgProc.StandardError.ReadLine()) != null)
			{
				Log.Send($"[GPG]: {stderrLine}");
				if (stderrLine.StartsWith(StatusMarker, StringComparison.Ordinal))
				{
					// This line is a status line, so extract status information from it.
					var statusLine = stderrLine.Substring(StatusMarker.Length);
					var spaceIndex = statusLine.IndexOf(" ", StringComparison.Ordinal);
					if (spaceIndex == -1)
					{
						statusMessages.Add(new StatusMessage(statusLine, null));
					}
					else
					{
						var statusLabel = statusLine.Substring(0, spaceIndex);
						// Length+1 because the space after the status label should be skipped.
						var statusMessage = statusLine.Substring(statusLabel.Length + 1);
						statusMessages.Add(new StatusMessage(statusLabel, statusMessage));
					}
				}
				else
				{
					stderrMessages.Add(stderrLine);
				}
			}

			return new StdErrResult(statusMessages, stderrMessages);
		}

		/// <summary>
		/// Spawns a GPG process.
		/// </summary>
		private IProcess CreateGpgProcess(string arguments, string? input = null, byte[]? secretStdin = null)
		{
			Log.Send($"Calling GPG with \"{arguments}\"");
			// Only redirect stdin if we're going to send anything to it.
			var psi = CreateGpgProcessStartInfo(arguments, input != null || secretStdin != null);

			var gpgProc = processes.Start(psi);
			if (secretStdin != null)
			{
				// Write the secret (e.g. the passphrase) as raw bytes so we never create an
				// un-zeroable string copy. The caller owns and clears the byte array.
				gpgProc.StandardInput.BaseStream.Write(secretStdin, 0, secretStdin.Length);
				gpgProc.StandardInput.BaseStream.Flush();
				gpgProc.StandardInput.Close();
			}
			else if (input != null)
			{
				// Explicitly define the encoding to not send a BOM, to ensure other platforms can handle our output.
				using var writer = new StreamWriter(gpgProc.StandardInput.BaseStream, new UTF8Encoding(false));
				writer.Write(input);
			}
			return gpgProc;
		}

		/// <summary>
		/// Generates a ProcessStartInfo object that can be used to spawn a GPG process.
		/// </summary>
		private ProcessStartInfo CreateGpgProcessStartInfo(string arguments, bool redirectStdin)
		{
			// Maybe use --display-charset utf-8?
			var argList = new List<string>
			{
				"--batch", // Ensure GPG does not ask for input or user action
				"--no-tty", // Let GPG know we're not a TTY
				"--status-fd 2", // Write status messages to stderr
				"--with-colons", // Use colon notation for displaying keys
				"--exit-on-status-write-error", //  Exit if status messages cannot be written
			};
			if (homeDirectory.IsOverride)
			{
				argList.Add($"--homedir {GpgArguments.Quote(homeDirectory.Path)}");
			}

			var psi = new ProcessStartInfo
			{
				FileName = installation.GpgExecutable.FullName,
				Arguments = $"{string.Join(" ", argList)} {arguments}",
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = redirectStdin,
				CreateNoWindow = true
			};

			// Force the C locale so gpg's human-readable diagnostics (e.g. "Bad passphrase") are
			// emitted in a stable language, which the loopback wrong-passphrase detection relies on.
			psi.EnvironmentVariables["LC_ALL"] = "C";
			psi.EnvironmentVariables["LANG"] = "C";

			return psi;
		}

	}
}
