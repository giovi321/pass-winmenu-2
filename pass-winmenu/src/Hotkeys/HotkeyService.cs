using System;
using System.Collections.Generic;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.WinApi;

namespace PassWinmenu.Hotkeys
{
	internal class HotkeyService : IDisposable
	{
		private readonly List<IDisposable> registrations = new();
		private readonly IHotkeyRegistrar registrar;

		/// <summary>
		/// Create a new hotkey manager.
		/// </summary>
		public HotkeyService(IHotkeyRegistrar registrar)
		{
			this.registrar = registrar;
		}

		public void AssignHotkeys(IEnumerable<HotkeyConfig> config, ActionDispatcher actionDispatcher)
		{
			foreach (var hotkey in config)
			{
				var keys = KeyCombination.Parse(hotkey.Hotkey);
				try
				{
					switch (hotkey.Action)
					{
						case HotkeyAction.DecryptPassword:
							AddHotKey(keys, () => actionDispatcher.DecryptPassword(hotkey.Options.CopyToClipboard, hotkey.Options.TypeUsername, hotkey.Options.TypePassword));
							break;
						case HotkeyAction.GenerateTotpCode:
							AddHotKey(keys, () => actionDispatcher.GenerateTotpCode(hotkey.Options.CopyToClipboard, hotkey.Options.TypeTotpCode));
							break;
						case HotkeyAction.PasswordField:
							AddHotKey(keys, () => actionDispatcher.DecryptPasswordField(hotkey.Options.CopyToClipboard, hotkey.Options.Type, hotkey.Options.FieldName));
							break;
						case HotkeyAction.DecryptMetadata:
							AddHotKey(keys, () => actionDispatcher.DecryptMetadata(hotkey.Options.CopyToClipboard, hotkey.Options.Type));
							break;
						case HotkeyAction.AddPassword:
						case HotkeyAction.EditPassword:
						case HotkeyAction.ShowDebugInfo:
						case HotkeyAction.CheckForUpdates:
						case HotkeyAction.GitPull:
						case HotkeyAction.GitPush:
						case HotkeyAction.OpenShell:
						case HotkeyAction.ShowPassword:
						case HotkeyAction.EnrollBiometrics:
						case HotkeyAction.ShowAbout:
							AddHotKey(keys, () => actionDispatcher.Dispatch(hotkey.Action));
							break;
						case HotkeyAction.SelectNext:
						case HotkeyAction.SelectPrevious:
						case HotkeyAction.SelectFirst:
						case HotkeyAction.SelectLast:
						default:
							throw new HotkeyException("Invalid hotkey action");
					}
				}
				catch (HotkeyException e) when (e.InnerException?.HResult == HResult.HotkeyAlreadyRegistered)
				{
					throw new HotkeyException($"The hotkey \"{hotkey.Hotkey}\" is already registered by another application.");
				}
			}
		}

		public void Dispose()
		{
			foreach (var reg in registrations)
			{
				reg.Dispose();
			}
		}

		/// <summary>
		/// Register a new hotkey with Windows.
		/// </summary>
		/// <param name="keys">A KeyCombination object representing the keys to be pressed.</param>
		/// <param name="action">The action to be executed when the hotkey is pressed.</param>
		private void AddHotKey(KeyCombination keys, Action action)
		{
			var reg = registrar.Register(keys.ModifierKeys, keys.Key, (sender, args) =>
			{
				action.Invoke();
			});
			registrations.Add(reg);
		}
	}
}
