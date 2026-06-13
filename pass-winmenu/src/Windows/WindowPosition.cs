using System.Linq;
using System.Windows;
using System.Windows.Media;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities.ExtensionMethods;
using Screen = System.Windows.Forms.Screen;
using Cursor = System.Windows.Forms.Cursor;

namespace PassWinmenu.Windows
{
	internal class WindowPosition
	{
		public Point Position { get; set; }
		public Point Dimensions { get; set; }
		/// <summary>
		/// Builds a SelectionWindowConfiguration object according to the config settings.
		/// </summary>
		/// <returns></returns>
		public static WindowPosition Calculate(Matrix fromDevice, InterfaceConfig config)
		{
			var activeScreen = Screen.AllScreens.First(screen => screen.Bounds.Contains(Cursor.Position));
			var selectedScreen = config.FollowCursor ? activeScreen : Screen.PrimaryScreen;

			var left = selectedScreen.ProjectSizeToPixels(config.Style.OffsetLeft, Direction.Horizontal);
			var top = selectedScreen.ProjectSizeToPixels(config.Style.OffsetTop, Direction.Vertical);
			var width = selectedScreen.ProjectSizeToPixels(config.Style.Width, Direction.Horizontal);
			var height = selectedScreen.ProjectSizeToPixels(config.Style.Height, Direction.Vertical);
			return new WindowPosition
			{
				Dimensions = fromDevice.Transform(new Point(width, height)),
				Position = fromDevice.Transform(new Point(left + selectedScreen.Bounds.Left, top + selectedScreen.Bounds.Top)),
			};
		}
	}
}
