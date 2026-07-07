using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PassWinmenu.WinApi
{
	/// <summary>
	/// Switches a window's non-client area (title bar) to the dark variant.
	/// Best-effort: on OS builds that do not support the attribute this is a no-op.
	/// </summary>
	internal static class DarkTitleBar
	{
		private const int DwmwaUseImmersiveDarkMode = 20;

		public static void Apply(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;
			if (handle == IntPtr.Zero)
			{
				return;
			}
			var enabled = 1;
			_ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
		}

		[DllImport("dwmapi.dll", PreserveSig = true)]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
	}
}
