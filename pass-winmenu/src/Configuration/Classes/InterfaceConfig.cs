using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace PassWinmenu.Configuration
{
	public class InterfaceConfig
	{
		public bool FollowCursor { get; set; } = true;
		public string DirectorySeparator { get; set; } = "/";
		public double ClipboardTimeout { get; set; } = 30;
		public bool RestoreClipboard { get; set; } = true;

		[YamlMember(Alias = "hotkeys")]
		public HotkeyConfig[] UnfilteredHotkeys { get; set; } =
		{
			new HotkeyConfig
			{
				Hotkey = "tab",
				Action = HotkeyAction.SelectNext,
			},
			new HotkeyConfig
			{
				Hotkey = "shift tab",
				Action = HotkeyAction.SelectPrevious,
			}
		};

		[YamlIgnore]
		public IEnumerable<HotkeyConfig> Hotkeys => UnfilteredHotkeys
			?.Where(h => h != null && h.Hotkey != null && h.Options != null)
			?? Enumerable.Empty<HotkeyConfig>();

		public PasswordEditorConfig PasswordEditor { get; set; } = new PasswordEditorConfig();
		public StyleConfig Style { get; set; } = new StyleConfig();
	}
}
