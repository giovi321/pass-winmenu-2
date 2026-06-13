using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

#nullable enable
namespace PassWinmenu.Configuration
{
	public class ConfigManager : IDisposable
	{
		public ConfigurationFile ConfigurationFile { get; private set; }
		
		private FileSystemWatcher? watcher;

		private ConfigManager(ConfigurationFile configurationFile)
		{
			ConfigurationFile = configurationFile;
		}

		public void EnableAutoReloading()
		{
			var directory = Path.GetDirectoryName(ConfigurationFile.Path);
			if (string.IsNullOrWhiteSpace(directory))
			{
				directory = Directory.GetCurrentDirectory();
			}
			watcher = new FileSystemWatcher(directory)
			{
				IncludeSubdirectories = false,
				EnableRaisingEvents = true
			};
			watcher.Changed += (sender, args) =>
			{
				// Wait a moment to allow the writing process to close the file.
				// This doesn't have to be exact, we can just cancel the reload if the file is still in use.
				Thread.Sleep(500);
				Log.Send($"Configuration file changed (change type: {args.ChangeType}), attempting reload.");

				// Reloading the configuration file involves creating UI resources
				// (Brush/Thickness), which needs to be done on the main thread.
				Application.Current.Dispatcher.Invoke(Reload);
			};
			
			Log.Send("Config reloading enabled");
		}
		
		public static LoadResult Load(string path, bool allowCreate)
		{
			if (!File.Exists(path))
			{
				if (!allowCreate)
				{
					return new LoadResult.NotFound();
				}
				
				try
				{
					using var defaultConfig = EmbeddedResources.DefaultConfig;
					using var configFile = File.Create(path);
					defaultConfig.CopyTo(configFile);
				}
				catch (Exception e) when (e is FileNotFoundException or FileLoadException or IOException)
				{
					throw new Exception("A new configuration file could not be created", e);
				}

				return new LoadResult.NewFileCreated();
			}

			using (var reader = File.OpenText(path))
			{
				var versionCheck = ConfigurationDeserialiser.Deserialise<Dictionary<string, object>?>(reader);
				if (versionCheck == null || !versionCheck.ContainsKey("config-version"))
				{
					return new LoadResult.NeedsUpgrade();
				}
				if (versionCheck["config-version"] as string != Program.LastConfigVersion)
				{
					return new LoadResult.NeedsUpgrade();
				}
			}

			using (var reader = File.OpenText(path))
			{
				var config = ConfigurationDeserialiser.Deserialise<Config>(reader);
				var configurationFile = new ConfigurationFile(path, config);
				return new LoadResult.Success(new ConfigManager(configurationFile));
			}
		}

		private void Reload()
		{
			try
			{
				using var reader = File.OpenText(ConfigurationFile.Path);
				ConfigurationFile = ConfigurationFile with
				{
					Config = ConfigurationDeserialiser.Deserialise<Config>(reader),
				};
				Log.Send("Configuration file reloaded successfully.");

			}
			catch (Exception e)
			{
				Log.Send($"Could not reload configuration file. An exception occurred.");
				Log.ReportException(e);
				// No need to do anything, we can simply continue using the old configuration.
			}
		}

		public static string Backup(string originalFile)
		{
			var extension = Path.GetExtension(originalFile);
			var name = Path.GetFileNameWithoutExtension(originalFile);
			var directory = Path.GetDirectoryName(originalFile);

			// Find an unused name to which we can rename the old configuration file.
			var root = string.IsNullOrEmpty(directory) ? name : Path.Combine(directory, name);
			var newFileName = $"{root}-backup{extension}";
			var counter = 2;
			while (File.Exists(newFileName))
			{
				newFileName =$"{root}-backup-{counter++}{extension}";
			}

			File.Move(originalFile, newFileName);

			using var defaultConfig = EmbeddedResources.DefaultConfig;
			using var configFile = File.Create(originalFile);
			
			defaultConfig.CopyTo(configFile);
			
			return newFileName;
		}

		public void Dispose()
		{
			watcher?.Dispose();
		}
	}
}
