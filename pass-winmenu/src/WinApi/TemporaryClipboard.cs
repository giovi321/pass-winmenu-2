using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities;

#nullable enable
namespace PassWinmenu.WinApi
{
	public class TemporaryClipboard
	{
		private readonly InterfaceConfig config;

		public TemporaryClipboard(InterfaceConfig config)
		{
			this.config = config;
		}
		
		/// <summary>
		/// Copies a string to the clipboard. If it still exists on the clipboard after the amount of time
		/// specified in <paramref name="timeout"/>, it will be removed again.
		/// </summary>
		/// <param name="text">The text to add to the clipboard.</param>
		/// <returns>The time until the placed text will be cleared.</returns>
		public TimeSpan Place(string text)
		{
			Helpers.AssertOnUiThread();

			var clipboardBackup = MakeClipboardBackup();

			// Place the text in a DataObject that tells Windows 10/11 not to include it
			// in Clipboard History (Win+V) or upload it to the Cloud Clipboard.
			var dataObject = new DataObject();
			dataObject.SetData(DataFormats.Text, text);
			dataObject.SetData("CanIncludeInClipboardHistory", 0);
			dataObject.SetData("CanUploadToCloudClipboard", 0);
			Clipboard.SetDataObject(dataObject, true);

			var timeout = TimeSpan.FromSeconds(config.ClipboardTimeout);
			Task.Delay(timeout).ContinueWith(_ => PlaceInternal(text, clipboardBackup), TaskScheduler.Default);

			// If the application exits before the timeout elapses, clear the clipboard on exit.
			// Stale handlers from earlier Place calls are harmless: PlaceInternal only acts
			// if the clipboard still contains the text it placed.
			if (Application.Current != null)
			{
				Application.Current.Exit += (_, _) => PlaceInternal(text, clipboardBackup);
			}

			return timeout;
		}

		public string? GetText()
		{
			return Clipboard.ContainsText() ? Clipboard.GetText() : null;
		}

		private void PlaceInternal(string text, Dictionary<string, object> clipboardBackup)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				try
				{
					// Only reset the clipboard to its previous contents if it still contains the text we copied to it.
					if (!Clipboard.ContainsText() || Clipboard.GetText() != text)
					{
						return;
					}

					Clipboard.Clear();

					if (!config.RestoreClipboard)
					{
						return;
					}

					// Create a new DataObject into which we can restore our data.
					var dataObject = new DataObject();
					Log.Send($"Restoring previous clipboard contents:");
					foreach (var pair in clipboardBackup)
					{
						Log.Send($" - {pair.Key}");
						dataObject.SetData(pair.Key, pair.Value);
					}

					// Now place it on the clipboard.
					Clipboard.SetDataObject(dataObject, true);
				}
				catch (Exception e)
				{
					Log.Send($"Failed to restore previous clipboard contents: An exception occurred ({e.GetType().Name}: {e.Message})", LogLevel.Error);
				}
			});
		}

		/// <summary>
		/// Backs up the current clipboard data to a dictionary mapping data formats to contents.
		/// </summary>
		private static Dictionary<string, object> MakeClipboardBackup()
		{
			var clipboardBackup = new Dictionary<string, object>();
			var dataObject = Clipboard.GetDataObject();
			if (dataObject == null)
			{
				return clipboardBackup;
			}
			Log.Send("Creating clipboard backup.");
			var formats = dataObject.GetFormats(false);
			Log.Send($" - Formats: {string.Join(", ", formats)}");
			foreach (var format in formats)
			{
				try
				{
					clipboardBackup[format] = dataObject.GetData(format, false);
				}
				catch (Exception e)
				{
					Log.Send($"Couldn't store format \"{format}\": {e.GetType().Name} ({e.Message})", LogLevel.Warning);
				}
			}
			return clipboardBackup;
		}
	}
}
