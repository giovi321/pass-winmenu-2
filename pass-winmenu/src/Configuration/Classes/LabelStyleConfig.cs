using System.Windows;
using System.Windows.Media;

#nullable enable
namespace PassWinmenu.Configuration
{
	public class LabelStyleConfig
	{
		public Brush? TextColour { get; set; }
		public Brush? BackgroundColour { get; set; }
		public Thickness BorderWidth { get; set; }
		public Brush? BorderColour { get; set; }
		public Thickness Margin { get; set; }
	}
}
