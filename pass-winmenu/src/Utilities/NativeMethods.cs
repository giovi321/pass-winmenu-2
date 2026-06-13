using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PassWinmenu.Hotkeys;
using PInvoke;

namespace PassWinmenu.Utilities
{
	internal class NativeMethods
	{
		public static Process GetWindowProcess(IntPtr hWnd)
		{
			_ = User32.GetWindowThreadProcessId(hWnd, out var pid);
			return Process.GetProcessById(pid);
		}

		/// <summary>
		/// Creates an overlapped, pop-up, or child window with an extended
		/// window style (otherwise identical to CreateWindow).
		/// </summary>
		/// <param name="dwExtStyle">
		/// The extended window styles to apply to the window.
		/// </param>
		/// <param name="lpClassName">
		/// A pointer to the name of the class to use for the window, or an
		/// atom returned by <see cref="RegisterClass(ref WindowClass)"/>.
		/// </param>
		/// <param name="lpWindowName">
		/// The name of the window to create.
		/// </param>
		/// <param name="dwStyle">
		/// The style values for the window to create.
		/// </param>
		/// <param name="x">
		/// The horizontal position of the window.
		/// </param>
		/// <param name="y">
		/// The vertical position of the window.
		/// </param>
		/// <param name="nWidth">
		/// The width of the window in device units.
		/// </param>
		/// <param name="nHeight">
		/// The height of the window in device units.
		/// </param>
		/// <param name="hWndParent">
		/// The handle to the window that is to be the parent of the
		/// created window.
		/// </param>
		/// <param name="hMenu">
		/// The handle to a menu or child window.
		/// </param>
		/// <param name="hInstance">
		/// The handle to an instance of the module to be associated
		/// with the window.
		/// </param>
		/// <param name="lpParam">
		/// A pointer to a value to be passed as the <c>lParam</c> of the
		/// <c>WM_CREATE</c> message sent to the window on creation.
		/// </param>
		/// <returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr CreateWindowEx(
			uint dwExtStyle,
			UIntPtr lpClassName,
			IntPtr lpWindowName,
			uint dwStyle,
			int x,
			int y,
			int nWidth,
			int nHeight,
			IntPtr hWndParent,
			IntPtr hMenu,
			IntPtr hInstance,
			IntPtr lpParam);

		/// <summary>
		/// Registers a system-wide hotkey.
		/// </summary>
		/// <param name="hWnd">
		/// The handle to the window that is to receive notification of
		/// the hotkey being triggered.
		/// </param>
		/// <param name="id">
		/// A handle-unique identifier for the hotkey.
		/// </param>
		/// <param name="fsModifiers">
		/// The modifier keys to be pressed with the hotkey, and other
		/// behavioural flags.
		/// </param>
		/// <param name="vk">
		/// The virtual-key code of the hotkey to be pressed with the
		/// modifier keys.
		/// </param>
		/// <returns>
		/// True if the hotkey was registered, false if otherwise.
		/// </returns>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterHotKey(
			IntPtr hWnd,
			int id,
			uint fsModifiers,
			uint vk);

		/// <summary>
		/// Unregisters a hotkey registered through the
		/// <see cref="RegisterHotKey(IntPtr, int, uint, uint)"/> function.
		/// </summary>
		/// <param name="hWnd">
		/// The handle to the window that receives notifications of the
		/// triggering of the hotkey to unregister.
		/// </param>
		/// <param name="id">
		/// The handle-unique identifier of the hotkey to unregister.
		/// </param>
		/// <returns>
		/// True if the hotkey was unregistered, false if otherwise.
		/// </returns>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		/// <summary>
		/// A procedure for handling messages received by the window.
		/// </summary>
		/// <param name="handle">
		/// A handle to the window receiving the message.
		/// </param>
		/// <param name="message">
		/// The type of message received.
		/// </param>
		/// <param name="wParam">
		/// Additional information dependent on the value of
		/// <paramref name="message"/>.
		/// </param>
		/// <param name="lParam">
		/// Additional information dependent on the value of
		/// <paramref name="message"/>.
		/// </param>
		/// <returns>
		/// A value which depends on the value of <paramref name="message"/>
		/// and which indicates the result of processing the message. Return
		/// null to defer to the next available window procedure (which is
		/// the default procedure if no other procedure is registered).
		/// </returns>
		public delegate IntPtr WindowProcedure(
			IntPtr handle,
			User32.WindowMessage message,
			IntPtr wParam,
			IntPtr lParam);

		/// <summary>
		/// Registers a window class for use in creating a window.
		/// </summary>
		/// <param name="wndClass">
		/// A description of the class to be created.
		/// </param>
		/// <returns>
		/// Zero if the operation does not succeed, or an atom uniquely
		/// identifying the registered class otherwise.
		/// </returns>
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern ushort RegisterClass(ref WindowClass wndClass);

		/// <summary>
		/// Unregisters a class registered using
		/// <see cref="RegisterClass(ref WindowClass)"/>.
		/// </summary>
		/// <param name="lpClassName">
		/// A pointer to the name of the class, or an atom returned
		/// by <see cref="RegisterClass(ref WindowClass)"/>.
		/// </param>
		/// <param name="hInstance">
		/// A handle to the instance of the module that created the
		/// class.
		/// </param>
		/// <returns>
		/// True if the class was unregistered, false if otherwise.
		/// </returns>
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnregisterClass(IntPtr lpClassName, IntPtr hInstance);

		/// <summary>
		/// Retrieves an <see cref="Exception"/> representing the last Win32
		/// error.
		/// </summary>
		internal static Exception LastWin32Exception()
		{
			var HResult = Marshal.GetHRForLastWin32Error();
			return Marshal.GetExceptionForHR(HResult) ?? new Exception($"Unknown exception with code {HResult}");
		}
	}
}
