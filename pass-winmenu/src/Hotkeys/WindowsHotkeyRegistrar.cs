using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PassWinmenu.Utilities;
using PInvoke;
using Disposable = PassWinmenu.Utilities.Disposable;
using Helpers = PassWinmenu.Utilities.Helpers;

#nullable enable
namespace PassWinmenu.Hotkeys
{
	/// <summary>
	/// A registrar for system-wide hotkeys registered through the Windows
	/// API.
	/// </summary>
	internal sealed class WindowsHotkeyRegistrar
		: IHotkeyRegistrar, IDisposable
	{
		/// <summary>
		/// The flag passed to <see cref="NativeMethods.RegisterHotKey(IntPtr, int, uint, uint)"/>
		/// to indicate that continuously holding the combination down should
		/// trigger the hotkey multiple times.
		/// </summary>
		private const ushort ModNoRepeat = 0x4000;

		private static WindowsHotkeyRegistrar? singleton;

		/// <summary>
		/// Retrieves a <see cref="WindowsHotkeyRegistrar"/> instance,
		/// creating one if one does not already exist.
		/// </summary>
		/// <returns>
		/// A hotkey registrar for the Windows API.
		/// </returns>
		public static WindowsHotkeyRegistrar Retrieve()
		{
			return singleton ??= new WindowsHotkeyRegistrar();
		}

		// The window procedure for handling hotkey messages.
		private IntPtr _windowProcedure(
			IntPtr hWnd, User32.WindowMessage msg, IntPtr wParam, IntPtr lParam
			)
		{
			// We only care about hotkey messages
			if (msg == User32.WindowMessage.WM_HOTKEY)
			{
				// If we don't recognise the hotkey ID, ignore it.
				if (!hotkeys.TryGetValue((int)wParam, out var handler))
				{
					// TODO: Trace here?
					return IntPtr.Zero;
				}

				// The logic in the rest of the class should prevent this
				// from being null. If it doesn't, we want the error, as
				// it means we aren't doing something properly.
				handler.Invoke(this, null!);

				// Indicate success
				return IntPtr.Zero;
			}

			// We didn't handle it; defer.
			return IntPtr.Zero;
		}

		// The window that will receive hotkey notifications for us.
		private readonly MessageWindow msgWindow;
		// Event handlers for the hotkey being triggered, keyed by the ID
		// provided when registering the hotkey.
		private readonly IDictionary<int, EventHandler> hotkeys = new Dictionary<int, EventHandler>();
		// Whether we're disposed
		private bool disposed;

		private WindowsHotkeyRegistrar()
		{
			msgWindow = new MessageWindow(_windowProcedure);
		}


		/*** IHotkeyRegistrar impl ***/
		IDisposable IHotkeyRegistrar.Register(
			ModifierKeys modifierKeys, Key key,
			EventHandler firedHandler
			)
		{
			if (firedHandler == null)
			{
				throw new ArgumentNullException(nameof(firedHandler));
			}

			// ID mirrors the [lParam] for the [WM_HOTKEY] message, but with
			// the [MOD_NOREPEAT] flag bit included where appropriate.
			var virtualKey = KeyInterop.VirtualKeyFromKey(key);
			var hotkeyId = ((int) modifierKeys << 16) | (ModNoRepeat << 16) | virtualKey;

			// If a hotkey for this combination is already registered, then
			// we can use a multicast delegate instead of re-registering.
			if (hotkeys.ContainsKey(hotkeyId))
			{
				hotkeys[hotkeyId] += firedHandler;
			}
			// Otherwise, the hotkey is not yet registered.
			else
			{
				var success = NativeMethods.RegisterHotKey(
					msgWindow.Handle,
					hotkeyId,
					(uint)modifierKeys | ModNoRepeat,
					(uint)virtualKey
				);

				if (success)
				{
					// Will fail if the ID is already in the collection
					hotkeys.Add(hotkeyId, firedHandler);
				}
				else
				{
					throw new HotkeyException(
						message: "An error occured in registering the hotkey.",
						innerException: NativeMethods.LastWin32Exception()
						);
				}
			}

			return new Disposable(() =>
			{
				var handler = hotkeys[hotkeyId];
				handler -= firedHandler;

				// A multicast delegate becomes null when all of its member
				// delegates are removed. If there are no handlers, we want
				// to unregister the hotkey.
				if (handler == null)
				{
					var unregistered = NativeMethods.UnregisterHotKey(
						hWnd: msgWindow.Handle,
						id: hotkeyId);

					if (!unregistered)
					{
						throw NativeMethods.LastWin32Exception();
					}

					hotkeys.Remove(hotkeyId);
				}
			});
		}

		/*** IDisposable impl ***/
		void IDisposable.Dispose()
		{
			if (disposed)
			{
				return;
			}

			// Attempt to unregister all of our hotkeys
			if (!hotkeys.All(hk => NativeMethods.UnregisterHotKey(msgWindow.Handle, hk.Key)))
			{
				throw NativeMethods.LastWin32Exception();
			}

			hotkeys.Clear();

			msgWindow.Dispose();

			// Next call to [Retrieve] will create a new registrar.
			singleton = null;

			disposed = true;
		}
	}
}
