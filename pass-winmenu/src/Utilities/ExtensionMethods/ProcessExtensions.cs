using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class ProcessExtensions
	{
		public static void FormatArguments(this ProcessStartInfo processStartInfo, params string[] args)
		{
			processStartInfo.Arguments = string.Join(
				" ",
				// Escape backslashes first: escaping quotes first would double the backslash
				// introduced by the quote escape, corrupting the sequence.
				args.Select(a => $"\"{a.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""));
		}
	}
}
