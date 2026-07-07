using System.Windows.Media;
using PassWinmenu.Configuration;

namespace PassWinmenu.Windows.Theming
{
	/// <summary>
	/// Derives the colour set used by the shared window theme and the tray menu
	/// from the user's <see cref="StyleConfig"/>. Control shades are computed
	/// relative to the configured background so a custom light config still
	/// produces usable contrast.
	/// </summary>
	public sealed class ThemePalette
	{
		public Color WindowBackground { get; }
		public Color Foreground { get; }
		public Color HintForeground { get; }
		public Color Accent { get; }
		public Color AccentForeground { get; }
		public Color AccentHover { get; }
		public Color Link { get; }
		public Color ControlBackground { get; }
		public Color ControlBorder { get; }
		public Color ControlHover { get; }
		public Color ControlPressed { get; }
		public bool IsDark { get; }

		public ThemePalette(StyleConfig style)
		{
			WindowBackground = GetColour(style.BackgroundColour, Color.FromRgb(0x20, 0x20, 0x20));
			Foreground = GetColour(style.Options.TextColour, Color.FromRgb(0xDD, 0xDD, 0xDD));
			HintForeground = GetColour(style.SearchHint.TextColour, Color.FromRgb(0x99, 0x99, 0x99));
			Accent = GetColour(style.BorderColour, Color.FromRgb(0x00, 0x78, 0xD4));

			IsDark = Luminance(WindowBackground) < 0.5;
			var shadeTarget = IsDark ? Colors.White : Colors.Black;

			AccentForeground = Luminance(Accent) < 0.5 ? Colors.White : Color.FromRgb(0x1A, 0x1A, 0x1A);
			AccentHover = Mix(Accent, shadeTarget, 0.12);
			Link = IsDark ? Mix(Accent, Colors.White, 0.35) : Accent;
			ControlBackground = Mix(WindowBackground, shadeTarget, 0.06);
			ControlHover = Mix(WindowBackground, shadeTarget, 0.11);
			ControlPressed = Mix(WindowBackground, shadeTarget, 0.15);
			ControlBorder = Mix(WindowBackground, shadeTarget, 0.17);
		}

		internal static Color Mix(Color from, Color to, double amount) => Color.FromRgb(
			(byte)System.Math.Round(from.R + (to.R - from.R) * amount),
			(byte)System.Math.Round(from.G + (to.G - from.G) * amount),
			(byte)System.Math.Round(from.B + (to.B - from.B) * amount));

		internal static double Luminance(Color colour) =>
			(0.2126 * colour.R + 0.7152 * colour.G + 0.0722 * colour.B) / 255.0;

		private static Color GetColour(Brush brush, Color fallback) =>
			brush is SolidColorBrush solid ? solid.Color : fallback;
	}
}
