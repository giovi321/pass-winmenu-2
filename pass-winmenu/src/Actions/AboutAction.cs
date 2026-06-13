using PassWinmenu.Configuration;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	/// <summary>
	/// Shows the About window (product, version, author, and attribution to the original).
	/// </summary>
	internal class AboutAction : IAction
	{
		public HotkeyAction ActionType => HotkeyAction.ShowAbout;

		public void Execute()
		{
			new AboutWindow().ShowDialog();
		}
	}
}
