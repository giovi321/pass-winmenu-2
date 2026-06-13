using System;
using Autofac;
using Autofac.Core;
using PassWinmenu.Configuration;
using PassWinmenu.Jobs;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu
{
	internal static class Program
	{
		public static string Version => EmbeddedResources.Version;

		public const string LastConfigVersion = "1.7";
		public const string EncryptedFileExtension = ".gpg";
		public const string PlaintextFileExtension = ".txt";

		public static IDisposable? Start(ConfigManager configManager)
		{
			IContainer? container = null;
			try
			{
				container = Setup.InitialiseDesktop(configManager);
				container.Resolve<UpdateGpgAgentConfig>().Run();
				container.Resolve<AssignHotkeys>().Run();
				container.Resolve<StartUpdateChecker>().Run();
				container.Resolve<PreloadGpgAgent>().Run();
				container.Resolve<BiometricUnlockJob>().Run();
				container.Resolve<StartRemoteUpdateChecker>().Run();
				container.Resolve<EnableConfigReloading>().Run();
			}
			catch (Exception e)
			{
				Log.EnableFileLogging();
				Log.Send("Could not start pass-winmenu: An exception occurred.", LogLevel.Error);
				Log.ReportException(e);

				if (e is DependencyResolutionException {InnerException: not null} de)
				{
					e = de.InnerException;
				}

				var errorMessage = $"pass-winmenu failed to start ({e.GetType().Name}: {e.Message})";
				new GraphicalDialogService().ShowErrorWindow(errorMessage);
				container?.Dispose();
				App.Exit();
			}

			return container;
		}
	}
}
