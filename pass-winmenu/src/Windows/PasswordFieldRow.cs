using System.ComponentModel;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// One row in the <see cref="PasswordDetailsWindow"/>: a field name, its value, and (for secret
	/// fields such as the password) a reveal toggle that masks the value until the user opts in.
	/// </summary>
	internal sealed class PasswordFieldRow : INotifyPropertyChanged
	{
		private const string Mask = "••••••••";

		private bool revealed;

		public PasswordFieldRow(string name, string value, bool isSecret)
		{
			Name = name;
			Value = value;
			IsSecret = isSecret;
		}

		public string Name { get; }

		/// <summary>The real value, copied to the clipboard on click.</summary>
		public string Value { get; }

		public bool IsSecret { get; }

		public bool IsRevealed
		{
			get => revealed;
			set
			{
				if (revealed == value)
				{
					return;
				}

				revealed = value;
				OnPropertyChanged(nameof(IsRevealed));
				OnPropertyChanged(nameof(Display));
			}
		}

		/// <summary>What is shown on screen: masked for secret fields until revealed.</summary>
		public string Display => IsSecret && !revealed ? Mask : Value;

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
