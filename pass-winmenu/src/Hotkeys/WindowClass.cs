using System;
using System.Runtime.InteropServices;
using NativeMethods = PassWinmenu.Utilities.NativeMethods;

namespace PassWinmenu.Hotkeys
{
	/// <summary>
	/// A description of a window class to be registered through
	/// <see cref="NativeMethods.RegisterClass(ref WindowClass)"/>.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct WindowClass
	{
		/// <summary>
		/// The styles for the window class.
		/// </summary>
		private readonly uint style;

		/// <summary>
		/// The window procedure for handling messages received
		/// by windows of this class.
		/// </summary>
		[MarshalAs(UnmanagedType.FunctionPtr)]
		public NativeMethods.WindowProcedure WindowProcedure;

		/// <summary>
		/// The quantity of additional bytes to allocate after the
		/// window class structure.
		/// </summary>
		private readonly int cbClsExtra;
		/// <summary>
		/// The quantity of additional bytes to allocate after an
		/// instance of a window of this class.
		/// </summary>
		private readonly int cbWndExtra;
		/// <summary>
		/// A handle to the instance that contains the window
		/// procedure for the class.
		/// </summary>
		public IntPtr Instance;
		/// <summary>
		/// A handle to the icon for the class.
		/// </summary>
		private readonly IntPtr hIcon;
		/// <summary>
		/// A handle to the cursor for the class.
		/// </summary>
		private readonly IntPtr hCursor;
		/// <summary>
		/// A handle to the class background brush.
		/// </summary>
		private readonly IntPtr hbrBackground;

		/// <summary>
		/// The name of the default menu for the class.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		private readonly string lpszMenuName;

		/// <summary>
		/// The name for this window class.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ClassName;
	}
}
