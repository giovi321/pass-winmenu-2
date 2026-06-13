using System;
using System.Collections.Generic;
using System.Linq;
using McSherry.SemanticVersioning;
using PassWinmenu.Configuration;
using PassWinmenu.UpdateChecking.Chocolatey;
using PassWinmenu.UpdateChecking.Dummy;
using PassWinmenu.UpdateChecking.GitHub;
using PassWinmenu.WinApi;

namespace PassWinmenu.UpdateChecking
{
	internal static class UpdateCheckerFactory
	{
		public static UpdateChecker CreateUpdateChecker(UpdateCheckingConfig updateCfg, IUpdateTracker updateTracker)
		{
			IUpdateSource updateSource = updateCfg.UpdateSource switch
			{
				UpdateSource.GitHub => new GitHubUpdateSource(),
				UpdateSource.Chocolatey => new ChocolateyUpdateSource(),
				UpdateSource.Dummy => new DummyUpdateSource
				{
					Versions = new List<ProgramVersion>
					{
						new ProgramVersion {VersionNumber = new SemanticVersion(10, 0, 0), Important = true,},
						new ProgramVersion
						{
							VersionNumber = SemanticVersion.Parse("v11.0-pre1", ParseMode.Lenient),
							IsPrerelease = true,
						},
					}
				},
				_ => throw new ArgumentOutOfRangeException(null, "Invalid update provider.")
			};

			var versionString = Program.Version.Split('-').First();

			var updateChecker = new UpdateChecker(
				updateSource,
				SemanticVersion.Parse(versionString, ParseMode.Lenient),
				updateCfg.AllowPrereleases,
				updateCfg.CheckIntervalTimeSpan,
				updateCfg.InitialDelayTimeSpan);

			updateChecker.UpdateAvailable += (sender, args) =>
			{
				updateTracker.HandleUpdateAvailable(args);
			};

			return updateChecker;
		}
	}
}
