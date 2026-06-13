using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PassWinmenu.Utilities;
using PInvoke;

namespace PassWinmenu.WinApi
{
	internal static class KeyboardEmulator
	{
		/// <summary>
		/// Sends text directly to the topmost window, as if it was entered by the user.
		/// </summary>
		/// <param name="text">The text to be sent to the active window.</param>
		internal static void EnterText(string text)
		{
			var inputs = new List<User32.INPUT>();
			foreach (var ch in text)
			{
				var down = Input.FromCharacter(ch, KeyDirection.Down);
				var up = Input.FromCharacter(ch, KeyDirection.Up);
				inputs.Add(down);
				inputs.Add(up);
			}
			SendInputs(inputs);
		}

		internal static void EnterTab()
		{
			var inputs = new List<User32.INPUT>
			{
				Input.FromKeyCode(User32.VirtualKey.VK_TAB, KeyDirection.Down),
				Input.FromKeyCode(User32.VirtualKey.VK_TAB, KeyDirection.Up),
			};
			SendInputs(inputs);
		}

		private static void SendInputs(List<User32.INPUT> inputs)
		{
			var size = Marshal.SizeOf(typeof(User32.INPUT));
			var success = User32.SendInput(inputs.Count, inputs.ToArray(), size);
			if (success != inputs.Count)
			{
				var exc = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())
					?? new Exception("Unknown error sending keyboard input");
				throw exc;
			}
		}

		private static class Input
		{
			internal static User32.INPUT FromKeyCode(User32.VirtualKey keyCode, KeyDirection direction)
			{
				User32.KEYEVENTF flags = 0;
				if (direction == KeyDirection.Up)
				{
					flags |= User32.KEYEVENTF.KEYEVENTF_KEYUP;
				}

				return new User32.INPUT
				{

					type = User32.InputType.INPUT_KEYBOARD,
					Inputs = new User32.INPUT.InputUnion
					{
						ki = new User32.KEYBDINPUT
						{
							wVk = keyCode,
							wScan = User32.ScanCode.NONAME,
							dwFlags = flags,
							time = 0,
							dwExtraInfo_IntPtr = IntPtr.Zero,
						}
					}
				};
			}

			internal static User32.INPUT FromCharacter(char character, KeyDirection direction)
			{
				var flags = User32.KEYEVENTF.KEYEVENTF_UNICODE;

				// If the scan code is preceded by a prefix byte that has the value 0xE0 (224),
				// we need to include the ExtendedKey flag in the Flags property.
				if ((character & 0xFF00) == 0xE000)
				{
					flags |= User32.KEYEVENTF.KEYEVENTF_EXTENDED_KEY;
				}
				if (direction == KeyDirection.Up)
				{
					flags |= User32.KEYEVENTF.KEYEVENTF_KEYUP;
				}

				return new User32.INPUT
				{

					type = User32.InputType.INPUT_KEYBOARD,
					Inputs = new User32.INPUT.InputUnion
					{
						ki = new User32.KEYBDINPUT
						{
							wVk = User32.VirtualKey.VK_NO_KEY,
							wScan = (User32.ScanCode) character,
							dwFlags = flags,
							time = 0,
							dwExtraInfo_IntPtr = IntPtr.Zero,
						}
					}
				};
			}
		}

		private enum KeyDirection
		{
			Up,
			Down,
		}
	}
}
