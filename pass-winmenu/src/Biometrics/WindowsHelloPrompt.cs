using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Coordinates a Windows Hello prompt so the system broker dialog is focused and a fingerprint
/// touch authenticates. pass-winmenu runs in the tray and is usually not the foreground process,
/// so the Hello dialog otherwise opens behind the active window.
///
/// Strategy: on the WPF UI (STA) thread show a small, NON-topmost window (so the broker can sit
/// above it), force it to the foreground, grant any process the right to set the foreground window
/// (so the broker can raise itself), then run the Hello call. A "retry" button gives a guaranteed
/// foreground path: a real click restores our foreground rights, cancels the in-flight attempt, and
/// starts a fresh one. No-ops the window when there is no WPF application (the <c>pw</c> CLI).
/// </summary>
internal static class WindowsHelloPrompt
{
	public static async Task<T> RunAsync<T>(string message, Func<CancellationToken, Task<T>> helloCall)
	{
		var app = Application.Current;
		if (app == null)
		{
			// No WPF (CLI/headless): no focus management possible, just run the call.
			return await helloCall(CancellationToken.None);
		}

		return await app.Dispatcher.Invoke(() => RunOnUiAsync(message, helloCall));
	}

	private static async Task<T> RunOnUiAsync<T>(string message, Func<CancellationToken, Task<T>> helloCall)
	{
		var window = CreateWindow(message, out var retryButton);
		try
		{
			window.Show();
			window.Activate();
			ForceForeground(new WindowInteropHelper(window).Handle);
			AllowForegroundForBroker();

			var attempt = new CancellationTokenSource();

			void OnRetry(object sender, RoutedEventArgs e)
			{
				// A genuine click restores our foreground rights; use them to re-raise the broker.
				ForceForeground(new WindowInteropHelper(window).Handle);
				AllowForegroundForBroker();
				var previous = attempt;
				attempt = new CancellationTokenSource();
				previous.Cancel();
			}

			retryButton.Click += OnRetry;
			try
			{
				while (true)
				{
					var current = attempt;
					try
					{
						return await helloCall(current.Token);
					}
					catch (OperationCanceledException) when (current.IsCancellationRequested && !ReferenceEquals(current, attempt))
					{
						// We cancelled this attempt in favour of a retry; loop and await the new one.
					}
				}
			}
			finally
			{
				retryButton.Click -= OnRetry;
			}
		}
		finally
		{
			window.Close();
		}
	}

	private static Window CreateWindow(string message, out Button retryButton)
	{
		retryButton = new Button
		{
			Content = "Authenticate with Windows Hello",
			Padding = new Thickness(10, 4, 10, 4),
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		var panel = new StackPanel { Margin = new Thickness(18) };
		panel.Children.Add(new TextBlock
		{
			Text = message,
			TextWrapping = TextWrapping.Wrap,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(0, 0, 0, 12),
		});
		panel.Children.Add(retryButton);

		return new Window
		{
			Title = "Pass Winmenu 2",
			Width = 340,
			Height = 150,
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			WindowStyle = WindowStyle.ToolWindow,
			ResizeMode = ResizeMode.NoResize,
			ShowInTaskbar = false,
			// NOT topmost: a topmost window covers the Hello broker dialog itself.
			Topmost = false,
			Content = panel,
		};
	}

	private static void AllowForegroundForBroker()
	{
		try
		{
			AllowSetForegroundWindow(ASFW_ANY);
		}
		catch (DllNotFoundException)
		{
			// Non-Windows or stripped environment: focus is best-effort.
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

	private const int ASFW_ANY = -1;

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AllowSetForegroundWindow(int dwProcessId);

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
