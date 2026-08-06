using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	public class GpgAgentConfigUpdater
	{
		public const string ManagedByPassWinmenuComment = "# This configuration key is automatically managed by pass-winmenu";

		/// <summary>
		/// The gpg-agent config keys pass-winmenu is allowed to manage. Keys from the user's
		/// config file that are not in this set are ignored, so a malicious config cannot make
		/// us write dangerous keys (e.g. <c>pinentry-program</c> or <c>allow-preset-passphrase</c>)
		/// into gpg-agent.conf.
		/// </summary>
		private static readonly HashSet<string> AllowedKeys = new HashSet<string>
		{
			"default-cache-ttl",
			"default-cache-ttl-ssh",
			"max-cache-ttl",
			"max-cache-ttl-ssh",
			"min-passphrase-len",
			"min-passphrase-nonalpha",
			"check-passphrase-pattern",
			"enforce-passphrase-constraints",
		};

		private readonly IGpgAgentConfigReader reader;

		public GpgAgentConfigUpdater(IGpgAgentConfigReader reader)
		{
			this.reader = reader;
		}

		/// <summary>
		/// Update the gpg-agent config file with the given keys.
		/// Keys are validated before anything is written: pairs containing CR/LF characters
		/// or starting with '#' are rejected, and keys outside <see cref="AllowedKeys"/> are
		/// ignored, so user-supplied config cannot inject extra lines or dangerous settings.
		/// </summary>
		public void UpdateAgentConfig(Dictionary<string, string> keys)
		{
			string[] lines;
			try
			{
				lines = reader.ReadConfigLines();
			}
			catch (Exception e)
			{
				Log.Send("Could not read agent config file. Updating it will not be possible.", LogLevel.Warning);
				Log.ReportException(e);
				return;
			}

			var validatedKeys = keys.Where(IsValidConfigPair).ToDictionary(pair => pair.Key, pair => pair.Value);

			var newLines = UpdateAgentConfigKeyCollection(lines, validatedKeys.ToList()).ToArray();

			if (lines.SequenceEqual(newLines))
			{
				Log.Send("GPG agent config file already contains the correct settings; it'll be left untouched.");
				return;
			}

			Log.Send($"Modifying GPG agent config file ({string.Join(", ", validatedKeys.Keys)})");
			try
			{
				reader.WriteConfigLines(newLines);
			}
			catch (Exception e)
			{
				Log.Send("Could not update agent config file.", LogLevel.Warning);
				Log.ReportException(e);
			}
		}

		/// <summary>
		/// Removes the given pass-winmenu-managed config keys (and their managed-by comment) from the
		/// gpg-agent config file. Returns true if the file was changed. Used to undo settings such as
		/// <c>allow-preset-passphrase</c> when the biometric feature is disabled, so we do not
		/// permanently weaken the user's gpg-agent.
		/// </summary>
		public bool RemoveManagedKeys(IEnumerable<string> keys)
		{
			string[] lines;
			try
			{
				lines = reader.ReadConfigLines();
			}
			catch (Exception e)
			{
				Log.Send("Could not read agent config file; cannot remove managed keys.", LogLevel.Warning);
				Log.ReportException(e);
				return false;
			}

			var keySet = new HashSet<string>(keys);
			var keyRegex = new Regex(@"^\s*([^#\s][^\s]*)");
			var newLines = new List<string>();
			var removed = false;

			foreach (var line in lines)
			{
				var match = keyRegex.Match(line);
				if (match.Success && keySet.Contains(match.Groups[1].Value))
				{
					removed = true;
					// Also drop the managed-by comment we wrote directly above the key.
					if (newLines.Count > 0 && newLines[^1] == ManagedByPassWinmenuComment)
					{
						newLines.RemoveAt(newLines.Count - 1);
					}

					continue;
				}

				newLines.Add(line);
			}

			if (!removed)
			{
				return false;
			}

			Log.Send($"Removing pass-winmenu-managed GPG agent config keys ({string.Join(", ", keySet)})");
			try
			{
				reader.WriteConfigLines(newLines.ToArray());
				return true;
			}
			catch (Exception e)
			{
				Log.Send("Could not update agent config file.", LogLevel.Warning);
				Log.ReportException(e);
				return false;
			}
		}

		/// <summary>
		/// Checks whether a user-supplied config pair is safe to write to gpg-agent.conf.
		/// Logs a warning and returns false for pairs that must not be written.
		/// </summary>
		private static bool IsValidConfigPair(KeyValuePair<string, string> pair)
		{
			if (pair.Key.IndexOfAny(new[] { '\r', '\n' }) >= 0 || pair.Value?.IndexOfAny(new[] { '\r', '\n' }) >= 0)
			{
				Log.Send($"Ignoring GPG agent config key '{pair.Key}': keys and values may not contain CR or LF characters.", LogLevel.Warning);
				return false;
			}

			if (pair.Key.StartsWith("#") || pair.Value?.StartsWith("#") == true)
			{
				Log.Send($"Ignoring GPG agent config key '{pair.Key}': keys and values may not start with '#'.", LogLevel.Warning);
				return false;
			}

			if (!AllowedKeys.Contains(pair.Key))
			{
				Log.Send($"Ignoring GPG agent config key '{pair.Key}': it is not in the whitelist of keys pass-winmenu is allowed to manage.", LogLevel.Warning);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Iterates over a list of config lines, adding or replacing the given config keys.
		/// </summary>
		private static IEnumerable<string> UpdateAgentConfigKeyCollection(string[] existingLines, List<KeyValuePair<string, string>> pairsToUpdate)
		{
			var configKeyRegex = new Regex(@"^(\s*([^#^\s][^\s]*)\s+)(.*)$");
			for (var i = 0; i < existingLines.Length; i++)
			{
				var line = existingLines[i];
				var match = configKeyRegex.Match(line);
				if (!match.Success)
				{
					// This line does not look like a config key, best not touch it.
					yield return line;
					continue;
				}

				// Line looks like a a configuration pair, let's check if we want to do something with it.
				var key = match.Groups[2].Value;
				var value = match.Groups[3].Value;
				var pairToSet = pairsToUpdate.FirstOrDefault(k => k.Key == key);
				if (pairToSet.Key == null)
				{
					// We don't recognise the key in this pair, so no need to change it.
					yield return line;
					continue;
				}

				// This pair may need its value updated, so remove it.
				pairsToUpdate.RemoveAll(k => k.Key == key);

				if (pairToSet.Value == value)
				{
					// The value is already correct, no need to do anything.
					yield return line;
					continue;
				}

				// Insert a comment explaining that we're managing this key,
				// unless such a comment already exists.
				if (i == 0 || existingLines[i - 1] != ManagedByPassWinmenuComment)
				{
					yield return ManagedByPassWinmenuComment;
				}

				// Now return the updated key-value pair.
				yield return $"{pairToSet.Key} {pairToSet.Value}";
			}

			while (pairsToUpdate.Any())
			{
				// Looks like some of the keys we need to set aren't in the config file yet, so let's add them.
				var next = pairsToUpdate[0];
				pairsToUpdate.RemoveAt(0);
				yield return ManagedByPassWinmenuComment;
				yield return $"{next.Key} {next.Value}";
			}
		}
	}
}
