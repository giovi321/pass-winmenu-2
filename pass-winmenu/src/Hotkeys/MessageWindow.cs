using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using PassWinmenu.Utilities;
using PInvoke;

namespace PassWinmenu.Hotkeys
{
	/// <summary>
	/// Represents a message-only window, which can send and receive
	/// messages but which is not visible, not enumerable, and does
	/// not receive broadcast messages.
	/// </summary>
	internal sealed class MessageWindow
		: IDisposable
	{

		// Internal window procedure
		private IntPtr _proc(IntPtr hWnd, User32.WindowMessage uMsg, IntPtr wp, IntPtr lp)
		{
			var ret = IntPtr.Zero;
			foreach (var wndProc in Procedures)
			{
				ret = wndProc(hWnd, uMsg, wp, lp);

				// If the procedure returned a value, we stop deferring
				// through window procedures.
				if (ret != IntPtr.Zero)
				{
					break;
				}
			}

			// If we have a value, return it.
			//
			// Otherwise, if we don't, either there were no procedures
			// or all the procedures wanted to defer. Either way, we
			// want to defer to the default window procedure.
			if (ret == IntPtr.Zero)
			{
				return User32.DefWindowProc(hWnd, uMsg, wp, lp);
			}

			return ret;
		}

		// Whether we've been disposed
		private bool disposed = false;
		// Atom representing our window class
		private readonly ushort windowAtom;
		// A reference to our window procedure delegate. Required to prevent
		// the GC collecting the delegate we pass to unmanaged code.
		private readonly NativeMethods.WindowProcedure procRef;

		/// <summary>
		/// Creates a message-only window with the specified procedures
		/// for processing messages.
		/// </summary>
		/// <param name="procs">
		/// The procedures for processing messages received by the window,
		/// in the order to which the procedures should be deferred.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="procs"/> is null or contains an element
		/// that is null.
		/// </exception>
		public MessageWindow(IReadOnlyCollection<NativeMethods.WindowProcedure> procs)
		{
			// Create new list to avoid unintended side effects of
			// modifying the collection we were passed as a parameter.
			//
			// This initialisation needs to be before the window is
			// created, as a message is sent to the window immediately
			// on creation and an NRE will result if this is not set.
			Procedures = procs.ToList();

			// Guid used for the window class name.
			var windowClassName = Guid.NewGuid();

			var hInstance = Process.GetCurrentProcess().Handle;

			// Keep a reference around for our window procedure to avoid
			// the GC collecting it. Important that this is not removed.
			procRef = _proc;

			var wndClass = new WindowClass
			{
				// Always use [_procRef] and not [_proc]; see above.
				WindowProcedure = procRef,
				ClassName = windowClassName.ToString(),
				Instance = hInstance,
			};

			windowAtom = NativeMethods.RegisterClass(ref wndClass);

			if (windowAtom == 0)
			{
				throw NativeMethods.LastWin32Exception();
			}

			Handle = NativeMethods.CreateWindowEx(
				dwExtStyle: 0,
				lpClassName: (UIntPtr)windowAtom,
				lpWindowName: IntPtr.Zero,
				dwStyle: 0,
				x: 0,
				y: 0,
				nWidth: 0,
				nHeight: 0,
				hWndParent: IntPtr.Zero,
				hMenu: IntPtr.Zero,
				hInstance: hInstance,
				lpParam: IntPtr.Zero
			);

			if (Handle == IntPtr.Zero)
			{
				throw NativeMethods.LastWin32Exception();
			}
		}
		/// <summary>
		/// Creates a message-only window with the specified procedure
		/// for processing messages.
		/// </summary>
		/// <param name="wndProc">
		/// The procedure for processing messages received by the window.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="wndProc"/> is null.
		/// </exception>
		public MessageWindow(NativeMethods.WindowProcedure wndProc)
			: this(new[] { wndProc })
		{
		}

		/// <summary>
		/// The handle to the window that will receive the messages.
		/// </summary>
		public IntPtr Handle
		{
			get;
		}

		/// <summary>
		/// The window procedures for the message window, in the order
		/// to which they are deferred.
		/// </summary>
		public IList<NativeMethods.WindowProcedure> Procedures
		{
			get;
		}

		/// <summary>
		/// Releases the unmanaged resources held by the instance.
		/// </summary>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			User32.DestroyWindow(Handle);
			NativeMethods.UnregisterClass((IntPtr)windowAtom, IntPtr.Zero);

			Procedures.Clear();

			disposed = true;
		}
	}
}
