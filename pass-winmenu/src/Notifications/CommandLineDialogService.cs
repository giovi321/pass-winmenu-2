using System;
using System.Windows;
using PassWinmenu.WinApi;

namespace PassWinmenu.Notifications;

internal class CommandLineDialogService : IDialogService
{
	public void ShowErrorWindow(string message, string title = "An error occurred.")
	{
		Console.Error.WriteLine(title);
		Console.Error.WriteLine(message);
	}

	public bool ShowYesNoWindow(string message, string title, MessageBoxImage image = MessageBoxImage.None)
	{
		Console.Error.WriteLine(title);
		Console.Error.WriteLine(message);
		
		while(true)
		{
			Console.Write("Y/N> ");
			var answer = Console.ReadLine() ?? string.Empty;
			switch (answer.ToLower())
			{
				case "y" or "yes":
					return true;
				case "n" or "no":
					return false;
			}
		}
	}
}
