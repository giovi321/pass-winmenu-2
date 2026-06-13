using System;
using System.Windows;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// Interaction logic for LogViewer.xaml
	/// </summary>
	public partial class LogViewer : Window
	{
		public LogViewer(string title, string logText)
		{
			InitializeComponent();
			Title = title;
			LogTextBox.Text = logText;
			LogTextBox.SelectionStart = 0;
			LogTextBox.SelectionLength = logText?.Length ?? throw new ArgumentNullException(nameof(logText));
		}

		public void AddMessage(string message)
		{
			Dispatcher.Invoke(() =>
			{
				LogTextBox.Text += $"{message}\n";
				LogTextBox.Select(LogTextBox.Text.Length - 1, 0);
			});
		}
	}
}
