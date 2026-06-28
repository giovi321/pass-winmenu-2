using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Shows a small window to host the Windows Hello gesture prompt and exposes its window handle, so the
/// NCrypt unlock can parent the Hello dialog to it (<c>NCRYPT_WINDOW_HANDLE_PROPERTY</c>) and the
/// dialog appears focused. pass-winmenu runs in the tray and is usually not the foreground process, so
/// the window is also forced to the foreground (a global hotkey grants no foreground rights, so a
/// synthetic ALT keypress is used to lift the foreground lock). Used as:
/// <c>using (var prompt = WindowsHelloPrompt.Show("...")) { ...use prompt.Handle... }</c>.
/// No-ops (handle is <see cref="IntPtr.Zero"/>) when there is no WPF application (the <c>pw</c> CLI).
/// </summary>
internal static class WindowsHelloPrompt
{
	/// <summary>A shown prompt window whose <see cref="Handle"/> can parent the Hello dialog.</summary>
	internal interface IPromptWindow : IDisposable
	{
		IntPtr Handle { get; }
	}

	public static IPromptWindow Show(string message)
	{
		var app = Application.Current;
		if (app == null)
		{
			return new NoWindow();
		}

		return app.Dispatcher.Invoke(() => (IPromptWindow)new PromptWindow(message));
	}

	private sealed class NoWindow : IPromptWindow
	{
		public IntPtr Handle => IntPtr.Zero;
		public void Dispose() { }
	}

	private sealed class PromptWindow : IPromptWindow
	{
		private readonly Window window;

		public IntPtr Handle { get; }

		public PromptWindow(string message)
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
				// NOT topmost: a topmost window would cover the Hello dialog it is meant to host.
				Topmost = false,
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
			Handle = new WindowInteropHelper(window).EnsureHandle();
			ForceForeground(Handle);
		}

		public void Dispose()
		{
			window.Dispatcher.Invoke(() => window.Close());
		}
	}

	/// <summary>
	/// Forces the given window to the foreground. A global hotkey does NOT grant foreground rights, so
	/// <c>SetForegroundWindow</c> is normally refused; synthesising an ALT keypress makes Windows treat
	/// us as having just received input, which clears the foreground lock, and attaching to the current
	/// foreground window's input queue does the rest.
	/// </summary>
	private static void ForceForeground(IntPtr hWnd)
	{
		if (hWnd == IntPtr.Zero)
		{
			return;
		}

		const int SW_RESTORE = 9;
		const int SW_SHOW = 5;
		const byte VK_MENU = 0x12;
		const uint KEYEVENTF_KEYUP = 0x0002;

		keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
		keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

		var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
		var currentThread = GetCurrentThreadId();
		var attached = foregroundThread != currentThread && foregroundThread != 0;

		if (attached)
		{
			AttachThreadInput(foregroundThread, currentThread, true);
		}

		ShowWindow(hWnd, SW_RESTORE);
		BringWindowToTop(hWnd);
		ShowWindow(hWnd, SW_SHOW);
		SetForegroundWindow(hWnd);
		SetActiveWindow(hWnd);

		if (attached)
		{
			AttachThreadInput(foregroundThread, currentThread, false);
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
	private static extern IntPtr SetActiveWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool BringWindowToTop(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
