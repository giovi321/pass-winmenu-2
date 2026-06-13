using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;

namespace PassWinmenu.Windows
{
	internal sealed partial class PasswordWindow
	{
		private readonly PasswordGenerationConfig passwordGenerationConfig;
		private readonly PasswordGenerator passwordGenerator;

		public PasswordWindow(string filename, PasswordGenerationConfig passwordGenerationConfig)
		{
			this.passwordGenerationConfig = passwordGenerationConfig;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			passwordGenerator = new PasswordGenerator(passwordGenerationConfig);

			// The per-character-group checkboxes only apply to the random generator.
			if (passwordGenerationConfig.Style == PasswordGenerationStyle.Random)
			{
				CreateCheckboxes();
			}

			Title = "Add new password";

			AddDefaultMetadata(filename);
			RegeneratePassword();
			Password.Focus();
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

		private void AddDefaultMetadata(string filename)
		{
			var now = DateTime.Now;
			var extraContent = passwordGenerationConfig.DefaultContent
				.Replace("$filename", filename)
				.Replace("$date", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
				.Replace("$time", now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
			ExtraContent.Text = extraContent;
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
	}
}
