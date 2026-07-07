using System.Windows.Media;
using PassWinmenu.Configuration;
using PassWinmenu.Utilities;
using PassWinmenu.Windows.Theming;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Windows
{
	public class ThemePaletteTests
	{
		private static StyleConfig DarkStyle() => new StyleConfig
		{
			BackgroundColour = Helpers.BrushFromColourString("#FF202020"),
			BorderColour = Helpers.BrushFromColourString("#FF0078D4"),
		};

		private static StyleConfig LightStyle() => new StyleConfig
		{
			BackgroundColour = Helpers.BrushFromColourString("#FFF5F5F5"),
			BorderColour = Helpers.BrushFromColourString("#FF0078D4"),
		};

		[Fact]
		public void DarkBackground_IsDetectedAsDark()
		{
			new ThemePalette(DarkStyle()).IsDark.ShouldBeTrue();
		}

		[Fact]
		public void LightBackground_IsDetectedAsLight()
		{
			new ThemePalette(LightStyle()).IsDark.ShouldBeFalse();
		}

		[Fact]
		public void DarkTheme_ControlShadesAreLighterThanBackground()
		{
			var palette = new ThemePalette(DarkStyle());
			ThemePalette.Luminance(palette.ControlBackground)
				.ShouldBeGreaterThan(ThemePalette.Luminance(palette.WindowBackground));
			ThemePalette.Luminance(palette.ControlBorder)
				.ShouldBeGreaterThan(ThemePalette.Luminance(palette.ControlBackground));
		}

		[Fact]
		public void LightTheme_ControlShadesAreDarkerThanBackground()
		{
			var palette = new ThemePalette(LightStyle());
			ThemePalette.Luminance(palette.ControlBackground)
				.ShouldBeLessThan(ThemePalette.Luminance(palette.WindowBackground));
		}

		[Fact]
		public void DarkTheme_LinkIsLighterThanAccent()
		{
			var palette = new ThemePalette(DarkStyle());
			ThemePalette.Luminance(palette.Link)
				.ShouldBeGreaterThan(ThemePalette.Luminance(palette.Accent));
		}

		[Fact]
		public void NonSolidBrush_FallsBackToDefaultColour()
		{
			var style = DarkStyle();
			style.BorderColour = new LinearGradientBrush(Colors.Red, Colors.Blue, 0);
			var palette = new ThemePalette(style);
			palette.Accent.ShouldBe(Color.FromRgb(0x00, 0x78, 0xD4));
		}

		[Fact]
		public void Luminance_IsZeroForBlackAndOneForWhite()
		{
			ThemePalette.Luminance(Colors.Black).ShouldBe(0.0, 0.001);
			ThemePalette.Luminance(Colors.White).ShouldBe(1.0, 0.001);
		}
	}
}
