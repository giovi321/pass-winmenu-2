using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PassWinmenu.Utilities;

namespace PassWinmenu.Configuration
{
	public class StyleConfig
	{
		public LabelStyleConfig Search { get; set; } = new LabelStyleConfig
		{
			TextColour = Helpers.BrushFromColourString("#FFDDDDDD"),
			BackgroundColour = Helpers.BrushFromColourString("#00FFFFFF"),
			BorderWidth = new Thickness(0),
			BorderColour = Helpers.BrushFromColourString("#FF000000"),
			Margin = new Thickness(0)
		};
		public LabelStyleConfig Options { get; set; } = new LabelStyleConfig
		{
			TextColour = Helpers.BrushFromColourString("#FFDDDDDD"),
			BackgroundColour = Helpers.BrushFromColourString("#00FFFFFF"),
			BorderWidth = new Thickness(0),
			BorderColour = Helpers.BrushFromColourString("#FF000000"),
			Margin = new Thickness(0)
		};
		public LabelStyleConfig Selection { get; set; } = new LabelStyleConfig
		{
			TextColour = Helpers.BrushFromColourString("#FFFFFFFF"),
			BackgroundColour = Helpers.BrushFromColourString("[accent]"),
			BorderWidth = new Thickness(0),
			BorderColour = Helpers.BrushFromColourString("#FF000000"),
			Margin = new Thickness(0)
		};
		public TextStyleConfig SearchHint { get; set; } = new TextStyleConfig
		{
			TextColour = Helpers.BrushFromColourString("#FF999999"),
		};
		public int ScrollBoundary { get; set; } = 0;
		public Orientation Orientation { get; set; } = Orientation.Vertical;
		public double FontSize { get; set; } = 14;
		public string FontFamily { get; set; } = "Consolas";
		public Brush BackgroundColour { get; set; } = Helpers.BrushFromColourString("#FF202020");
		public Brush BorderColour { get; set; } = Helpers.BrushFromColourString("[accent]");
		public Thickness BorderWidth { get; set; } = new Thickness(1);
		public Brush CaretColour { get; set; } = Helpers.BrushFromColourString("#FFDDDDDD");
		public Size OffsetLeft { get; set; } = Size.Percent.FromPercentage(40);
		public Size OffsetTop { get; set; } = Size.Percent.FromPercentage(40);
		public Size Width { get; set; } = Size.Percent.FromPercentage(20);
		public Size Height { get; set; } = Size.Percent.FromPercentage(20);
		public bool ScaleToFit { get; set; } = true;
	}

	public abstract record Size
	{
		public record Pixels(double Value) : Size;

		public record Percent : Size
		{
			public double Factor { get; }

			private Percent(double factor)
			{
				Factor = factor;
			}

			public static Percent FromPercentage(double percentage) => new(percentage / 100);
		}
	}
}
