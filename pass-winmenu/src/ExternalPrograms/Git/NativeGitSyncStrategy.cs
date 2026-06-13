using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using LibGit2Sharp;
using PassWinmenu.Configuration;

namespace PassWinmenu.ExternalPrograms
{
	internal class NativeGitSyncStrategy : IGitSyncStrategy
	{
		private readonly string repositoryPath;
		private readonly GitConfig gitConfig;
		private readonly TimeSpan gitCallTimeout = TimeSpan.FromSeconds(5);

		public NativeGitSyncStrategy(string repositoryPath, GitConfig gitConfig)
		{
			this.repositoryPath = repositoryPath;
			this.gitConfig = gitConfig;
		}

		public void Fetch(Branch branch)
		{
			CallGit("fetch " + branch.RemoteName);
		}

		/// <summary>
		/// Pushes changes to remote.
		/// </summary>
		public void Push()
		{
			CallGit("push");
		}
		private void CallGit(string arguments)
		{
			var argList = new List<string>
			{
				// May be required in certain cases?
				//"--non-interactive"
			};

			var psi = new ProcessStartInfo
			{
				FileName = gitConfig.GitPath,
				WorkingDirectory = repositoryPath,
				Arguments = $"{arguments} {string.Join(" ", argList)}",
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			if (!string.IsNullOrEmpty(gitConfig.SshPath))
			{
				// Remove is a no-op if the variable is not set.
				psi.EnvironmentVariables.Remove("GIT_SSH");
				psi.EnvironmentVariables.Add("GIT_SSH", gitConfig.SshPath);
			}
			Process gitProc;
			try
			{
				gitProc = Process.Start(psi) ?? throw new GitException("Failed to start Git process");
			}
			catch (Win32Exception e)
			{
				throw new GitException("Git failed to start. " + e.Message, e);
			}

			gitProc.WaitForExit((int)gitCallTimeout.TotalMilliseconds);
			var error = gitProc.StandardError.ReadToEnd();
			if (gitProc.ExitCode != 0)
			{
				throw new GitException($"Git exited with code {gitProc.ExitCode}", error);
			}
		}
	}
}
