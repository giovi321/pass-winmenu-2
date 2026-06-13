using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.Utilities.ExtensionMethods;

namespace PassWinmenu.Actions
{
	internal class OpenPasswordShellAction : IAction
	{
		private readonly GpgInstallation installation;
		private readonly PasswordStoreConfig passwordStore;
		private readonly GpgHomeDirectory homeDirectory;
		private readonly IFileSystem fileSystem;
		private readonly IProcesses processes;

		public HotkeyAction ActionType => HotkeyAction.OpenShell;

		public OpenPasswordShellAction(GpgInstallation installation, PasswordStoreConfig passwordStore, GpgHomeDirectory homeDirectory, IFileSystem fileSystem, IProcesses processes)
		{
			this.installation = installation;
			this.passwordStore = passwordStore;
			this.homeDirectory = homeDirectory;
			this.fileSystem = fileSystem;
			this.processes = processes;
		}

		public void Execute()
		{
			var powerShell = new ProcessStartInfo
			{
				FileName = "powershell",
				WorkingDirectory = passwordStore.Location,
				UseShellExecute = true,
			};

			var gpgExe = installation.GpgExecutable.FullName;

			string gpgInvocation;
			if (homeDirectory.IsOverride)
			{
				gpgInvocation  = FormatPowerShellArguments(gpgExe, "--homedir", fileSystem.Path.GetFullPath(homeDirectory.Path));
			}
			else
			{
				gpgInvocation = FormatPowerShellArguments(gpgExe);
			}

			powerShell.FormatArguments(
				"-NoExit",
				"-Command",
				$"function gpg() {{ & {gpgInvocation} $args }};"
				+ "echo '\n"
				+ "    ╔══════════════════════════════════════════════════════════╗\n"
				+ "    ║ In this shell, you can run  GPG commands  in your store. ║\n"
				+ "    ║ The ''gpg'' command  has been aliased  to the same version ║\n"
				+ "    ║ of GPG  used by pass-winmenu, and configured to make use ║\n"
				+ "    ║ of the  same  home  directory, so  you can  access  your ║\n"
				+ "    ║ password store GPG keys from here.                       ║\n"
				+ "    ╚══════════════════════════════════════════════════════════╝\n"
				+ "'");
			processes.Start(powerShell);
		}

		private static string FormatPowerShellArguments(params string[] args)
		{
			return string.Join(" ", args.Select(a => $"'{a.Replace("'", "''")}'"));
		}
	}
}
