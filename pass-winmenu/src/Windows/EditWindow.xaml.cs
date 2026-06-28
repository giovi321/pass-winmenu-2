using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PassWinmenu.Configuration;

#nullable enable
namespace PassWinmenu.Windows
{
	internal sealed partial class EditWindow
	{
		private readonly string originalContent;

		public EditWindow(string path, string content, PasswordGenerationConfig options)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			Generator.Initialize(options);

			Title = $"Editing '{path}'";

			originalContent = content.Replace(Environment.NewLine, "\n");

			PasswordContent.Text = content;
			PasswordContent.Focus();
		}

		private void Btn_Replace_Click(object sender, RoutedEventArgs e)
		{
			var content = PasswordContent.Text.Replace(Environment.NewLine, "\n");
			var index = content.IndexOf('\n');
			var password = Generator.GeneratedPassword;
			PasswordContent.Text = index == -1 ? password : password + content.Remove(0, index);
		}

		private void Btn_OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				DialogResult = false;
				Close();
			}
		}

		private void HandlePasswordContentFocus(object sender, RoutedEventArgs e)
		{
			if (PasswordContent.IsFocused)
			{
				PasswordDivider.Stroke = new SolidColorBrush(Color.FromRgb(86, 157, 229));
			}
			else
			{
				PasswordDivider.Stroke = new SolidColorBrush(Color.FromRgb(171, 173, 179));
			}
		}

		private void PasswordContent_TextChanged(object sender, TextChangedEventArgs e)
		{
			Btn_OK.IsEnabled = PasswordContent.Text.Replace(Environment.NewLine, "\n") != originalContent;
		}
	}
}
