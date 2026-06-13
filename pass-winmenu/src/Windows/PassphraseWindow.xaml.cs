using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;

namespace PassWinmenu.Windows
{
	/// <summary>
	/// A simple masked passphrase-entry dialog used during Windows Hello enrolment.
	/// </summary>
	internal sealed partial class PassphraseWindow
	{
		/// <summary>The entered passphrase, or null if the dialog was cancelled.</summary>
		public char[]? Result { get; private set; }

		public PassphraseWindow(string? prompt = null)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			if (!string.IsNullOrEmpty(prompt))
			{
				Prompt.Text = prompt;
			}

			PassphraseBox.Focus();
		}

		private void Btn_OK_Click(object sender, RoutedEventArgs e)
		{
			// Read via SecurePassword rather than the Password getter, which would return an
			// immutable, un-zeroable string copy of the passphrase.
			Result = SecureStringToCharArray(PassphraseBox.SecurePassword);
			DialogResult = true;
			Close();
		}

		private static char[] SecureStringToCharArray(SecureString secure)
		{
			var ptr = Marshal.SecureStringToGlobalAllocUnicode(secure);
			try
			{
				var chars = new char[secure.Length];
				for (var i = 0; i < chars.Length; i++)
				{
					chars[i] = (char)Marshal.ReadInt16(ptr, i * 2);
				}

				return chars;
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocUnicode(ptr);
			}
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
	}
}
