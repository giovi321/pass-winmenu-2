using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PassWinmenu.Windows
{
	internal sealed partial class AboutWindow
	{
		public AboutWindow()
		{
			InitializeComponent();
			VersionLine.Text = $"Version {Program.Version}";
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = e.Uri.AbsoluteUri,
				UseShellExecute = true,
			});
			e.Handled = true;
		}

		private void Btn_Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
