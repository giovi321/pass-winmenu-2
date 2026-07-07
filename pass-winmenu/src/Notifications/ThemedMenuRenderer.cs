using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PassWinmenu.Notifications
{
	/// <summary>
	/// Renders the tray context menu with colours from the shared theme palette,
	/// so it matches the WPF windows.
	/// </summary>
	internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
	{
		private readonly ThemedMenuColours colours;

		public ThemedMenuRenderer(ThemedMenuColours colours)
			: base(new ThemedColourTable(colours))
		{
			this.colours = colours;
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			if (e.Item.Enabled && e.Item.Selected)
			{
				e.TextColor = colours.SelectionText;
			}
			else if (e.Item.ForeColor.ToArgb() == SystemColors.ControlText.ToArgb())
			{
				// Item colour was never customised; use the themed default.
				e.TextColor = colours.Text;
			}
			base.OnRenderItemText(e);
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			e.ArrowColor = colours.Text;
			base.OnRenderArrow(e);
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			// The stock check glyph is a dark bitmap; draw a themed check mark instead.
			var r = e.ImageRectangle;
			using var pen = new Pen(e.Item.Selected ? colours.SelectionText : colours.Text, 1.8f);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.DrawLines(pen, new[]
			{
				new PointF(r.Left + r.Width * 0.25f, r.Top + r.Height * 0.55f),
				new PointF(r.Left + r.Width * 0.45f, r.Top + r.Height * 0.75f),
				new PointF(r.Left + r.Width * 0.78f, r.Top + r.Height * 0.30f),
			});
		}

		private sealed class ThemedColourTable : ProfessionalColorTable
		{
			private readonly ThemedMenuColours colours;

			public ThemedColourTable(ThemedMenuColours colours)
			{
				this.colours = colours;
			}

			public override Color ToolStripDropDownBackground => colours.Background;
			public override Color ImageMarginGradientBegin => colours.Background;
			public override Color ImageMarginGradientMiddle => colours.Background;
			public override Color ImageMarginGradientEnd => colours.Background;
			public override Color MenuItemSelected => colours.SelectionBackground;
			public override Color MenuItemSelectedGradientBegin => colours.SelectionBackground;
			public override Color MenuItemSelectedGradientEnd => colours.SelectionBackground;
			public override Color MenuItemBorder => colours.SelectionBackground;
			public override Color MenuBorder => colours.Border;
			public override Color SeparatorDark => colours.Separator;
			public override Color SeparatorLight => colours.Background;
		}
	}
}
