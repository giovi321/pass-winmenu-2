using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PassWinmenu.Configuration;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// Shows every field of a decrypted password file (the password plus each metadata key) with its
	/// value alongside it. Clicking a value copies it to the clipboard; the password is masked until
	/// the user reveals it with the eye toggle.
	/// </summary>
	internal sealed partial class PasswordDetailsWindow
	{
		private readonly Action<PasswordFieldRow> copyField;

		public PasswordDetailsWindow(string title, IReadOnlyList<PasswordFieldRow> rows, Action<PasswordFieldRow> copyField, InterfaceConfig interfaceConfig)
		{
			this.copyField = copyField;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			ApplyStyle(interfaceConfig.Style);

			Title = title;
			HeaderText.Text = title;
			FieldList.ItemsSource = rows;
		}

		/// <summary>
		/// Applies the same theming the selection menu uses, so this window matches it visually.
		/// </summary>
		private void ApplyStyle(StyleConfig style)
		{
			Background = style.BackgroundColour;
			BorderBrush = style.BorderColour;
			BorderThickness = style.BorderWidth;
			FontFamily = new FontFamily(style.FontFamily);
			FontSize = style.FontSize;
			Foreground = style.Options.TextColour;

			// Brushes referenced by the XAML via DynamicResource.
			Resources["FieldForeground"] = style.Options.TextColour;
			Resources["FieldHoverBackground"] = style.Selection.BackgroundColour;
			Resources["FieldHoverForeground"] = style.Selection.TextColour;
			Resources["HintForeground"] = style.SearchHint.TextColour;
		}

		private void CopyField_Click(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement { Tag: PasswordFieldRow row })
			{
				copyField(row);
			}
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
			}
		}
	}
}
