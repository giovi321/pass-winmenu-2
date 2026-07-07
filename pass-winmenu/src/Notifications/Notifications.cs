using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.UpdateChecking;
using PassWinmenu.WinApi;
using PassWinmenu.Windows.Theming;

namespace PassWinmenu.Notifications
{
	internal class Notifications : INotificationService, INotifyIcon, ISyncStateTracker
	{
		private readonly NotifyIcon icon;
		private readonly NotificationConfig config;
		private readonly ToolStripMenuItem downloadUpdate;
		private readonly ToolStripSeparator downloadSeparator;

		private const string DownloadUpdateString = "https://github.com/giovi321/pass-winmenu-2/releases";
		private const int ToolTipTimeoutMs = 5000;

		private Notifications(NotifyIcon icon, ActionDispatcher actionDispatcher, NotificationConfig config)
		{
			this.icon = icon;
			this.config = config;
			this.icon.Click += HandleIconClick;

			downloadUpdate = new ToolStripMenuItem("Download Update")
			{
				Visible = false,
			};
			downloadUpdate.Click += HandleDownloadUpdateClick;

			downloadSeparator = new ToolStripSeparator
			{
				Visible = false
			};

			AddMenuActions(actionDispatcher);
		}

		public static Notifications Create(ActionDispatcher actionDispatcher, NotificationConfig config)
		{
			var icon = new NotifyIcon
			{
				Icon = EmbeddedResources.Icon,
				Visible = true
			};


			return new Notifications(icon, actionDispatcher, config);
		}

		private void HandleIconClick(object? sender, EventArgs e)
		{
			var args = (MouseEventArgs)e;
			if (args.Button == MouseButtons.Left)
			{
				// Unfortunately, calling Show() here does not do what you'd expect.
				// It displays the menu in the wrong place, and the menu won't hide if you click outside it.
				// ShowContextMenu() does the right thing, but it's a private method,
				// so we have to resort to a bit of a hack to get it to work.
				var mi = typeof(NotifyIcon)
					.GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
				mi!.Invoke(icon, null);
			}
		}

		private void AddMenuActions(ActionDispatcher actionDispatcher)
		{
			var menu = new ContextMenuStrip();
			var colours = Theme.Current is { IsDark: true } palette ? ThemedMenuColours.FromPalette(palette) : null;
			if (colours != null)
			{
				menu.Renderer = new ThemedMenuRenderer(colours);
				menu.BackColor = colours.Background;
				menu.ForeColor = colours.Text;
				downloadUpdate.ForeColor = colours.Link;
			}
			var versionLabel = new ToolStripLabel("Pass Winmenu 2 " + Program.Version);
			if (colours != null)
			{
				versionLabel.ForeColor = colours.Hint;
			}
			menu.Items.Add(versionLabel);
			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(downloadUpdate);
			menu.Items.Add(downloadSeparator);

			menu.Items.Add("Decrypt Password", null, (sender, args) => actionDispatcher.DecryptPassword(true, false, false));
			menu.Items.Add("Show Password Fields", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.ShowPassword));
			menu.Items.Add("Add new Password", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.AddPassword));
			menu.Items.Add("Edit Password File", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.EditPassword));
			menu.Items.Add("Generate TOTP Code", null, (sender, args) => actionDispatcher.GenerateTotpCode(true, false));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Push to Remote", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.GitPush));
			menu.Items.Add("Pull from Remote", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.GitPull));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Open Explorer", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.OpenExplorer));
			menu.Items.Add("Open Shell", null, (sender, args) => Task.Run(() => actionDispatcher.Dispatch(HotkeyAction.OpenShell)));
			menu.Items.Add(new ToolStripSeparator());

			var dropDown = new ToolStripMenuItem("More Actions");
			if (colours != null)
			{
				dropDown.DropDown.Renderer = new ThemedMenuRenderer(colours);
				dropDown.DropDown.BackColor = colours.Background;
				dropDown.DropDown.ForeColor = colours.Text;
			}
			dropDown.DropDownItems.Add("Check for Updates", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.CheckForUpdates));
			dropDown.DropDownItems.Add("Edit Configuration", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.EditConfiguration));
			dropDown.DropDownItems.Add("Re-Encrypt Password Store", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.ReencryptPasswordStore));
			dropDown.DropDownItems.Add("Set up Windows Hello unlock", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.EnrollBiometrics));
			dropDown.DropDownItems.Add("View Log", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.ViewLog));

			menu.Items.Add(dropDown);
			menu.Items.Add(new ToolStripSeparator());

			var startupLink = new StartupLink("Pass Winmenu 2");
			var startWithWindows = new ToolStripMenuItem("Start with Windows")
			{
				Checked = startupLink.Exists
			};
			startWithWindows.Click += (sender, args) =>
			{
				var target = Application.ExecutablePath;
				var workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
				startupLink.Toggle(target, workingDirectory);
				startWithWindows.Checked = startupLink.Exists;
			};

			menu.Items.Add(startWithWindows);

			menu.Items.Add("About", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.ShowAbout));
			menu.Items.Add("Quit", null, (sender, args) => App.Exit());
			icon.ContextMenuStrip = menu;
		}

		public void AddUpdate(ProgramVersion version)
		{
			downloadUpdate.Text += $" ({version})";
			downloadUpdate.Visible = true;
			downloadSeparator.Visible = true;
		}

		public void Raise(string message, Severity level)
		{
			if (config.Enabled)
			{
				icon.ShowBalloonTip(ToolTipTimeoutMs, "Pass Winmenu 2", message, GetIconForSeverity(level));
			}
		}

		private ToolTipIcon GetIconForSeverity(Severity severity)
		{
			return severity switch
			{
				Severity.None => ToolTipIcon.None,
				Severity.Info => ToolTipIcon.Info,
				Severity.Warning => ToolTipIcon.Warning,
				Severity.Error => ToolTipIcon.Error,
				_ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
			};
		}

		private static void HandleDownloadUpdateClick(object? sender, EventArgs e)
		{
			var openDownloadPage = new ProcessStartInfo
			{
				FileName = DownloadUpdateString,
				UseShellExecute = true,
			};
			Process.Start(openDownloadPage);
		}

		public void Dispose()
		{
			icon.Dispose();
			downloadUpdate.Dispose();
			downloadSeparator.Dispose();
		}

		public void SetSyncState(SyncState state)
		{
			icon.Icon = state switch
			{
				SyncState.UpToDate => EmbeddedResources.Icon,
				SyncState.Ahead => EmbeddedResources.IconAhead,
				SyncState.Behind => EmbeddedResources.IconBehind,
				SyncState.Diverged => EmbeddedResources.IconDiverged,
				_ => icon.Icon
			};
		}
	}

	internal enum Severity
	{
		None,
		Info,
		Warning,
		Error,
	}
}
