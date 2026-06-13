using System.IO;
using System.IO.Abstractions;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgInstallationFinder
	{
		private readonly IFileSystem fileSystem;
		private readonly IExecutablePathResolver executablePathResolver;
		private readonly IDirectoryInfo gpgDefaultInstallDir;

		public const string GpgExeName = "gpg.exe";
		public const string GpgAgentExeName = "gpg-agent.exe";
		public const string GpgConnectAgentExeName = "gpg-connect-agent.exe";
		public const string GpgConfExeName = "gpgconf.exe";
		public const string GpgPresetPassphraseExeName = "gpg-preset-passphrase.exe";


		public GpgInstallationFinder(IFileSystem fileSystem, IExecutablePathResolver executablePathResolver)
		{
			this.fileSystem = fileSystem;
			this.executablePathResolver = executablePathResolver;

			gpgDefaultInstallDir = fileSystem.DirectoryInfo.New(@"C:\Program Files (x86)\gnupg\bin");
		}

		/// <summary>
		/// Tries to find the GPG installation directory from the given path.
		/// </summary>
		/// <param name="gpgPathSpec">Path to the GPG executable. When set to null,
		/// the default location will be used.</param>
		public GpgInstallation FindGpgInstallation(string? gpgPathSpec = null)
		{
			Log.Send("Attempting to detect the GPG installation directory");
			if (string.IsNullOrEmpty(gpgPathSpec))
			{
				Log.Send("No GPG executable path set, assuming GPG to be in its default installation directory.");
				return new GpgInstallation
				(
					gpgDefaultInstallDir,
					ChildOf(gpgDefaultInstallDir, GpgExeName),
					ChildOf(gpgDefaultInstallDir, GpgAgentExeName),
					ChildOf(gpgDefaultInstallDir, GpgConnectAgentExeName),
					ChildOf(gpgDefaultInstallDir, GpgConfExeName),
					ChildOf(gpgDefaultInstallDir, GpgPresetPassphraseExeName)
				);
			}

			return ResolveFromPath(gpgPathSpec!);
		}

		private GpgInstallation ResolveFromPath(string gpgPathSpec)
		{
			string resolved;
			try
			{
				resolved = executablePathResolver.Resolve(gpgPathSpec);
			}
			catch (ExecutableNotFoundException)
			{
				throw new GpgError($"Gpg executable not found. Please verify that '{gpgPathSpec}' points to a valid GPG executable.");
			}
			var executable = fileSystem.FileInfo.New(resolved);

			Log.Send("GPG executable found at the configured path. Assuming installation dir to be " + executable.Directory);

			return new GpgInstallation
			(
				executable.Directory ?? throw new GpgError($"Unable to determine GPG installation directory from executable path '{resolved}' (resolved from '{gpgPathSpec}')"),
				executable,
				ChildOf(executable.Directory, GpgAgentExeName),
				ChildOf(executable.Directory, GpgConnectAgentExeName),
				ChildOf(executable.Directory, GpgConfExeName),
				ChildOf(executable.Directory, GpgPresetPassphraseExeName)
			);
		}

		private IFileInfo ChildOf(IFileSystemInfo parent, string childName)
		{
			var fullPath = Path.Combine(parent.FullName, childName);
			return fileSystem.FileInfo.New(fullPath);
		}
	}

	internal class GpgInstallation
	{
		public IDirectoryInfo InstallDirectory { get; set; }
		public IFileInfo GpgExecutable { get; set; }
		public IFileInfo GpgAgentExecutable { get; set; }
		public IFileInfo GpgConnectAgentExecutable { get; set; }
		public IFileInfo GpgConfExecutable { get; set; }
		public IFileInfo GpgPresetPassphraseExecutable { get; set; }

		public GpgInstallation(IDirectoryInfo installDirectory, IFileInfo gpgExecutable, IFileInfo gpgAgentExecutable, IFileInfo gpgConnectAgentExecutable, IFileInfo gpgConfExecutable, IFileInfo gpgPresetPassphraseExecutable)
		{
			InstallDirectory = installDirectory;
			GpgExecutable = gpgExecutable;
			GpgAgentExecutable = gpgAgentExecutable;
			GpgConnectAgentExecutable = gpgConnectAgentExecutable;
			GpgConfExecutable = gpgConfExecutable;
			GpgPresetPassphraseExecutable = gpgPresetPassphraseExecutable;
		}
	}
}
