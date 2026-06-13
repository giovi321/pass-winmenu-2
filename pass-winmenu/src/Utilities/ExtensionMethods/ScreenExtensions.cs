using System;
using System.Windows.Forms;
using PassWinmenu.Configuration;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class ScreenExtensions
	{
		public static double ProjectSizeToPixels(this Screen screen, Size size, Direction direction)
		{
			return size switch
			{
				Size.Pixels p => p.Value,
				Size.Percent p when direction == Direction.Horizontal => p.Factor * screen.Bounds.Width,
				Size.Percent p when direction == Direction.Vertical => p.Factor * screen.Bounds.Height,
				_ => throw new ArgumentOutOfRangeException(nameof(size))
			};
		}
	}

	internal enum Direction
	{
		Horizontal,
		Vertical
	}
}
