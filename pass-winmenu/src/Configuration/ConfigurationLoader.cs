using System;
using System.Diagnostics;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using YamlDotNet.Core;

namespace PassWinmenu.Configuration;

internal class ConfigurationLoader
{
	public static Option<ConfigManager> Load(IDialogService dialogService, string configPath)
	{
		LoadResult result;
		try
		{
			result = ConfigManager.Load(configPath, allowCreate: true);
		}
		catch (Exception e) when (e.InnerException != null)
		{
			if (e is YamlException)
			{
				dialogService.ShowErrorWindow(
					$"The configuration file could not be loaded: {e.Message}\n\n"
					+ $"{e.InnerException.GetType().Name}: {e.InnerException.Message}",
					"Unable to load configuration file.");
			}
			else
			{
				dialogService.ShowErrorWindow(
					$"The configuration file could not be loaded. An unhandled exception occurred.\n"
					+ $"{e.InnerException.GetType().Name}: {e.InnerException.Message}",
					"Unable to load configuration file.");
			}

			return default;
		}
		catch (SemanticErrorException e)
		{
			dialogService.ShowErrorWindow(
				$"The configuration file could not be loaded, a YAML error was encountered.\n"
				+ $"{e.GetType().Name}: {e.Message}\n\n"
				+ $"File location: {configPath}",
				"Unable to load configuration file.");
			return default;
		}
		catch (YamlException e)
		{
			dialogService.ShowErrorWindow(
				$"The configuration file could not be loaded. An unhandled exception occurred.\n{e.GetType().Name}: {e.Message}",
				"Unable to load configuration file.");
			return default;
		}

		switch (result)
		{
			case LoadResult.NewFileCreated:
				var open = dialogService.ShowYesNoWindow(
					"A new configuration file has been generated. Please modify it according to your preferences and restart the application.\n\n"
					+ "Would you like to open it now?",
					"New configuration file created");
				if (open)
				{
					Process.Start("explorer", configPath);
				}

				return default;
			case LoadResult.NeedsUpgrade:
				var backedUpFile = ConfigManager.Backup(configPath);
				var openBoth = dialogService.ShowYesNoWindow(
					"The current configuration file is out of date. A new configuration file has been created, and the old file has been backed up.\n"
					+ "Please edit the new configuration file according to your preferences and restart the application.\n\n"
					+ "Would you like to open both files now?",
					"Configuration file out of date");
				if (openBoth)
				{
					Process.Start("explorer", configPath);
					Process.Start(backedUpFile);
				}

				return default;
			case LoadResult.Success success:
				return Option.Some(success.ConfigManager);
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
