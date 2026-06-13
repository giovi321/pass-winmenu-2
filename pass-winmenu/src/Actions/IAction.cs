using PassWinmenu.Configuration;

namespace PassWinmenu.Actions
{
	internal interface IAction
	{
		void Execute();
		HotkeyAction ActionType { get; }
	}
}
