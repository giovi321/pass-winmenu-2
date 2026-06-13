using System.Windows;

namespace PassWinmenu.WinApi;

public class GraphicalDialogService : IDialogService
{
	/// <summary>
	/// Shows an error dialog to the user.
	/// </summary>
	/// <param name="message">The error message to be displayed.</param>
	/// <param name="title">The window title of the error dialog.</param>
	/// <remarks>
	/// It might seem a bit inconsistent that some errors are sent as notifications, while others are
	/// displayed in a MessageBox using the ShowErrorWindow method below.
	/// The reasoning for this is explained here.
	/// ShowErrorWindow is used for any error that results from an action initiated by a user and 
	/// prevents that action for being completed successfully, as well as any error that forces the
	/// application to exit.
	/// Any other errors should be sent as notifications, which aren't as intrusive as an error dialog that
	/// forces you to stop doing whatever you were doing and click OK before you're allowed to continue.
	/// </remarks>
	public void ShowErrorWindow(string message, string title = "An error occurred.")
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public bool ShowYesNoWindow(string message, string title, MessageBoxImage image = MessageBoxImage.None)
	{
		return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.None) == MessageBoxResult.Yes;
	}
}
