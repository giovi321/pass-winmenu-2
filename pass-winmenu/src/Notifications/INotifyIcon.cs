using PassWinmenu.Actions;
using PassWinmenu.UpdateChecking;

namespace PassWinmenu.Notifications
{
	internal interface INotifyIcon
	{
		void AddUpdate(ProgramVersion version);
	}
}
