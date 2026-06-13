using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

#nullable enable
namespace PassWinmenu.Configuration
{
	public class Config
	{
		public PasswordStoreConfig PasswordStore { get; set; } = new PasswordStoreConfig();
		public GitConfig Git { get; set; } = new GitConfig();
		public GpgConfig Gpg { get; set; } = new GpgConfig();
		[Obsolete("This key is no longer used, but it is still present to allow older" +
			"configuration files to be deserialised successfully.", true)]
		public object Output { get; set; } = new object();

		[YamlMember(Alias = "hotkeys")]
		public HotkeyConfig[]? UnfilteredHotkeys { get; set; } =
		{
			new HotkeyConfig
			{
				Hotkey = "ctrl alt p",
				Action = HotkeyAction.DecryptPassword,
				Options = new HotkeyOptions
				{
					CopyToClipboard = true
				}
			},
			new HotkeyConfig
			{
				Hotkey = "ctrl alt shift p",
				Action = HotkeyAction.DecryptPassword,
				Options = new HotkeyOptions
				{
					CopyToClipboard = true,
					TypeUsername = true,
					TypePassword = true
				}
			}
		};

		[YamlIgnore]
		public IEnumerable<HotkeyConfig> Hotkeys => UnfilteredHotkeys
			?.Where(h => h != null && h.Hotkey != null && h.Options != null)
			?? Enumerable.Empty<HotkeyConfig>();

		public NotificationConfig Notifications { get; set; } = new NotificationConfig();

		public ApplicationConfig Application { get; set; } = new ApplicationConfig();

		public InterfaceConfig Interface { get; set; } = new InterfaceConfig();

		public bool CreateLogFile { get; set; } = false;
		/// <summary>
		/// Determines the current version of the configuration file.
		/// Config file versions run synchronously with pass-winmenu versions,
		/// but not every pass-winmenu update will also bump the configuration file version.
		/// This only happens when there are changes preventing users from re-using an older
		/// configuration file for a newer version of pass-winmenu. In that case, the new
		/// configuration file version will be set to the latest version of pass-winmenu.
		/// If not set, it is assumed to be 1.7.
		/// </summary>
		public string ConfigVersion { get; set; } = "1.7";
	}
}
