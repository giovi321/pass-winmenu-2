using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;

#nullable enable
namespace PassWinmenu.Windows
{
	internal sealed partial class EditWindow
	{
		private readonly PasswordGenerator passwordGenerator;
		private readonly string originalContent;

		public EditWindow(string path, string content, PasswordGenerationConfig options)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			passwordGenerator = new PasswordGenerator(options);
			CreateCheckboxes();

			Title = $"Editing '{path}'";

			originalContent = content.Replace(Environment.NewLine, "\n");

			PasswordContent.Text = content;
			PasswordContent.Focus();
		}

		private void CreateCheckboxes()
		{
			var colCount = 3;
			var index = 0;
			foreach (var charGroup in passwordGenerator.Options.CharacterGroups)
			{
				var x = index % colCount;
				var y = index / colCount;

				var cbx = new CheckBox
				{
					Name = charGroup.Name,
					Content = charGroup.Name,
					Margin = new Thickness(x * 100, y * 20, 0, 0),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					IsChecked = charGroup.Enabled,
				};
				cbx.Unchecked += HandleCheckedChanged;
				cbx.Checked += HandleCheckedChanged;
				CharacterGroups.Children.Add(cbx);

				index++;
			}
		}

		private void RegeneratePassword()
		{
			Password.Text = passwordGenerator.GeneratePassword();
			Password.CaretIndex = Password.Text?.Length ?? 0;
		}

		private void Btn_Generate_Click(object sender, RoutedEventArgs e)
		{
			Password.Text = passwordGenerator.GeneratePassword();
		}

		private void Btn_Replace_Click(object sender, RoutedEventArgs e)
		{
			var content = PasswordContent.Text.Replace(Environment.NewLine, "\n");
			var index = content.IndexOf('\n');
			PasswordContent.Text = index == -1 ? Password.Text : Password.Text + content.Remove(0, index);
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

		private void HandleCheckedChanged(object sender, RoutedEventArgs e)
		{
			var checkbox = (CheckBox)sender;
			passwordGenerator.Options.CharacterGroups.First(c => c.Name == checkbox.Name).Enabled = checkbox.IsChecked ?? false;

			RegeneratePassword();
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
