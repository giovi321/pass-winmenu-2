using System.Windows;

namespace PassWinmenu.WinApi;

/// <summary>
/// Perform interaction with the user, either through graphical pop-up windows or through the commandline UI.
/// </summary>
public interface IDialogService
{
	void ShowErrorWindow(string message, string title = "An error occurred.");

	bool ShowYesNoWindow(string message, string title, MessageBoxImage image = MessageBoxImage.None);
}
