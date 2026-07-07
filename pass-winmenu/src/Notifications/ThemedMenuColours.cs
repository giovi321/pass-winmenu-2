using System.Drawing;
using PassWinmenu.Windows.Theming;

namespace PassWinmenu.Notifications
{
	/// <summary>
	/// The WPF theme palette translated to System.Drawing colours for the
	/// WinForms tray menu.
	/// </summary>
	internal sealed record ThemedMenuColours(
		Color Background,
		Color Text,
		Color Hint,
		Color Link,
		Color SelectionBackground,
		Color SelectionText,
		Color Separator,
		Color Border)
	{
		public static ThemedMenuColours FromPalette(ThemePalette palette) => new(
			Background: ToDrawing(palette.ControlBackground),
			Text: ToDrawing(palette.Foreground),
			Hint: ToDrawing(palette.HintForeground),
			Link: ToDrawing(palette.Link),
			SelectionBackground: ToDrawing(palette.Accent),
			SelectionText: ToDrawing(palette.AccentForeground),
			Separator: ToDrawing(palette.ControlBorder),
			Border: ToDrawing(palette.ControlBorder));

		private static Color ToDrawing(System.Windows.Media.Color colour) =>
			Color.FromArgb(colour.A, colour.R, colour.G, colour.B);
	}
}
