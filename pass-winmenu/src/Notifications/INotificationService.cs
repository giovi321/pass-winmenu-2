using System;
using PassWinmenu.WinApi;

namespace PassWinmenu.Notifications
{
	/// <summary>
	/// Presents notifications to the user.
	/// Unlike <see cref="IDialogService"/>, notifications are informational and must not require user interaction.
	/// If a notification needs to be seen or handled, use <see cref="IDialogService"/> instead.
	/// </summary>
	internal interface INotificationService : IDisposable
	{
		void Raise(string message, Severity level);
	}
}
