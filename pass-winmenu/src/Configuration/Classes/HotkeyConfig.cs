#nullable enable
namespace PassWinmenu.Configuration
{
	public class HotkeyConfig
	{
		public string Hotkey { get; set; } = string.Empty;
		public HotkeyAction Action { get; set; } = HotkeyAction.AddPassword;
		public HotkeyOptions Options { get; set; } = new();
	}
}
