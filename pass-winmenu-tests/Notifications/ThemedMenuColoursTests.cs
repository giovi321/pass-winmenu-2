using PassWinmenu.Configuration;
using PassWinmenu.Notifications;
using PassWinmenu.Utilities;
using PassWinmenu.Windows.Theming;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Notifications
{
	public class ThemedMenuColoursTests
	{
		[Fact]
		public void FromPalette_ConvertsWpfColoursToDrawingColours()
		{
			var style = new StyleConfig
			{
				BackgroundColour = Helpers.BrushFromColourString("#FF202020"),
				BorderColour = Helpers.BrushFromColourString("#FF0078D4"),
			};
			var palette = new ThemePalette(style);

			var colours = ThemedMenuColours.FromPalette(palette);

			colours.Text.R.ShouldBe(palette.Foreground.R);
			colours.Text.G.ShouldBe(palette.Foreground.G);
			colours.Text.B.ShouldBe(palette.Foreground.B);
			colours.SelectionBackground.R.ShouldBe(palette.Accent.R);
			colours.Background.ShouldNotBe(colours.SelectionBackground);
		}
	}
}
