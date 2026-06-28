using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;

#nullable enable
namespace PassWinmenu.Windows
{
	internal sealed partial class PasswordGeneratorControl
	{
		private PasswordGenerator? passwordGenerator;
		private bool initialised;
		private string? currentBasePassword;

		public PasswordGeneratorControl()
		{
			InitializeComponent();
		}

		/// <summary>The currently generated password (the editable text in the box).</summary>
		public string GeneratedPassword => Password.Text;

		public void FocusPassword() => Password.Focus();

		/// <summary>
		/// Wires the control to a generation config and produces the first password. The config object
		/// is used in memory only; nothing here writes the YAML file.
		/// </summary>
		public void Initialize(PasswordGenerationConfig config)
		{
			passwordGenerator = new PasswordGenerator(config);

			// Length slider: start at the configured length, clamped to the slider range.
			var initialLength = config.Length;
			if (initialLength < LengthSlider.Minimum)
			{
				initialLength = (int)LengthSlider.Minimum;
			}
			else if (initialLength > LengthSlider.Maximum)
			{
				initialLength = (int)LengthSlider.Maximum;
			}

			LengthSlider.Value = initialLength;
			// Changing the length means a different base password, so regenerate.
			LengthSlider.ValueChanged += (_, _) => GenerateNewBase();

			// Special-character toggle: only meaningful when a pool is configured.
			var special = config.SpecialCharacters;
			if (string.IsNullOrEmpty(special.Characters))
			{
				Chk_Special.Visibility = Visibility.Collapsed;
			}
			else
			{
				Chk_Special.Content = $"Add special characters ({special.Characters})";
				Chk_Special.IsChecked = special.Enabled;
				// Toggling only adds/removes the special characters; it must NOT regenerate the password.
				Chk_Special.Checked += (_, _) => RenderPassword();
				Chk_Special.Unchecked += (_, _) => RenderPassword();
			}

			// Character-group checkboxes only apply to the random generator.
			if (config.Style == PasswordGenerationStyle.Random)
			{
				CreateCheckboxes();
			}
			else
			{
				CharacterGroups.Visibility = Visibility.Collapsed;
			}

			initialised = true;
			GenerateNewBase();
		}

		private void CreateCheckboxes()
		{
			const int colCount = 3;
			var index = 0;
			foreach (var charGroup in passwordGenerator!.Options.CharacterGroups)
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

		/// <summary>Generates a fresh base password (length/character-set driven) and renders it.</summary>
		private void GenerateNewBase()
		{
			if (!initialised || passwordGenerator == null)
			{
				return;
			}

			var targetLength = (int)LengthSlider.Value;
			currentBasePassword = passwordGenerator.GenerateBase(targetLength);
			UpdateLengthLabel(targetLength);
			RenderPassword();
		}

		/// <summary>
		/// Renders the current base password with or without the special characters, without changing
		/// the base. Used by the special-character toggle so ticking it does not regenerate.
		/// </summary>
		private void RenderPassword()
		{
			if (passwordGenerator == null)
			{
				return;
			}

			var includeSpecial = Chk_Special.Visibility == Visibility.Visible && Chk_Special.IsChecked == true;

			Password.Text = currentBasePassword == null
				? null
				: includeSpecial
					? passwordGenerator.ApplySpecialCharacters(currentBasePassword)
					: currentBasePassword;
			Password.CaretIndex = Password.Text?.Length ?? 0;
		}

		private void UpdateLengthLabel(int targetLength)
		{
			if (passwordGenerator!.Options.Style == PasswordGenerationStyle.Xkcd)
			{
				var words = passwordGenerator.ComputeXkcdWordCount(targetLength);
				Lbl_Length.Text = $"{targetLength}  (≈{words} words)";
			}
			else
			{
				Lbl_Length.Text = $"{targetLength}";
			}
		}

		private void Btn_Generate_Click(object sender, RoutedEventArgs e) => GenerateNewBase();

		private void HandleCheckedChanged(object sender, RoutedEventArgs e)
		{
			var checkbox = (CheckBox)sender;
			passwordGenerator!.Options.CharacterGroups.First(c => c.Name == checkbox.Name).Enabled =
				checkbox.IsChecked ?? false;

			GenerateNewBase();
		}
	}
}
