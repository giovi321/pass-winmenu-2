using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Shows a small top-most window and forces it to the foreground while a Windows Hello
/// prompt is displayed. pass-winmenu runs in the tray and is usually not the foreground
/// process, so without this the system Hello dialog opens behind the active window and is
/// never focused. Use as: <c>using (WindowsHelloPrompt.Show("...")) { await helloCall; }</c>.
/// No-ops when there is no WPF application (e.g. the <c>pw</c> command line).
/// </summary>
internal static class WindowsHelloPrompt
{
	public static IDisposable Show(string message)
	{
		var app = Application.Current;
		if (app == null)
		{
			return new NoopScope();
		}

		return app.Dispatcher.Invoke(() => (IDisposable)new PromptScope(message));
	}

	private sealed class NoopScope : IDisposable
	{
		public void Dispose() { }
	}

	private sealed class PromptScope : IDisposable
	{
		private readonly Window window;

		public PromptScope(string message)
		{
			window = new Window
			{
				Title = "Pass Winmenu 2",
				Width = 340,
				Height = 120,
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				WindowStyle = WindowStyle.ToolWindow,
				ResizeMode = ResizeMode.NoResize,
				ShowInTaskbar = false,
				Topmost = true,
				Content = new TextBlock
				{
					Text = message,
					Margin = new Thickness(18),
					TextWrapping = TextWrapping.Wrap,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					TextAlignment = TextAlignment.Center,
				},
			};

			window.Show();
			window.Activate();
			ForceForeground(new WindowInteropHelper(window).Handle);
		}

		public void Dispose()
		{
			window.Dispatcher.Invoke(() => window.Close());
		}
	}

	/// <summary>
	/// Forces the given window to the foreground, working around the SetForegroundWindow
	/// restrictions by temporarily attaching to the current foreground window's input queue.
	/// </summary>
	private static void ForceForeground(IntPtr hWnd)
	{
		if (hWnd == IntPtr.Zero)
		{
			return;
		}

		const int SW_SHOW = 5;
		var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
		var currentThread = GetCurrentThreadId();

		if (foregroundThread != currentThread && foregroundThread != 0)
		{
			AttachThreadInput(foregroundThread, currentThread, true);
			BringWindowToTop(hWnd);
			ShowWindow(hWnd, SW_SHOW);
			SetForegroundWindow(hWnd);
			AttachThreadInput(foregroundThread, currentThread, false);
		}
		else
		{
			BringWindowToTop(hWnd);
			ShowWindow(hWnd, SW_SHOW);
			SetForegroundWindow(hWnd);
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool BringWindowToTop(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
