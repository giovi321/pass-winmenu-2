using System;
using System.Collections.Generic;
using System.Linq;

namespace PassWinmenu.Windows
{
	internal class ScrollableViewOptions
	{
		public int ViewPortSize { get; set; } = 20;
		public int ScrollBoundarySize { get; set; } = 2;
	}

	/// <summary>
	/// Represents a scrollable view into a list of items.
	/// Items can be selected, and if the selected item is out of the scrolling bounds,
	/// it will be scrolled into view.
	/// </summary>
	internal class ScrollableView<TItem> where TItem : IEquatable<TItem>
	{
		private List<TItem> items;

		public ScrollableView(IEnumerable<TItem> items, ScrollableViewOptions options)
		{
			if (options.ViewPortSize <= options.ScrollBoundarySize * 2)
			{
				throw new ArgumentException(
					"Viewport size should be more than twice as big as the scroll boundary size.", nameof(options));
			}

			this.items = items.ToList();
			ViewPortSize = options.ViewPortSize;
			ScrollBoundarySize = options.ScrollBoundarySize;
		}
		private int ViewPortSize { get; }

		public int SelectionIndex { get; private set; } = 0;

		public int ViewPortStart { get; private set; } = 0;

		// If the selection is within this number of items from the boundaries of the viewport,
		// the view will be scrolled to bring the item closer to the centre.
		public int ScrollBoundarySize { get; }

		public List<TItem> ItemsInView => items.GetRange(ViewPortStart, Math.Min(ViewPortSize, items.Count - ViewPortStart));

		public event EventHandler<int> SelectionIndexChanged = Dummy;
		public event EventHandler<int> ViewPortChanged = Dummy;

		public void SelectNext()
		{
			var nextIndex = SelectionIndex + 1;
			if (nextIndex >= items.Count)
			{
				return;
			}

			SelectIndex(nextIndex);
		}

		public void SelectPrevious()
		{
			var nextIndex = SelectionIndex - 1;
			if (nextIndex < 0)
			{
				return;
			}

			SelectIndex(nextIndex);
		}

		public void SelectFirst()
		{
			SelectIndex(0);
		}

		public void SelectLast()
		{
			SelectIndex(items.Count - 1);
		}

		public void SelectIndex(int index)
		{
			if (index == SelectionIndex)
			{
				return;
			}

			if (index < 0 || index >= items.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			var effectiveViewPortStart = ViewPortStart + ScrollBoundarySize;
			var effectiveViewPortSize = ViewPortSize - 2 * ScrollBoundarySize;

			if (index >= effectiveViewPortStart && index < effectiveViewPortStart + effectiveViewPortSize)
			{
				// The index fits in the effective viewport, so we do not need to scroll.
				SelectionIndex = index;
				SelectionIndexChanged(this, SelectionIndex);
				return;
			}

			var nextIndexIsLower = index > effectiveViewPortStart;

			int desiredViewPortStart;
			if (nextIndexIsLower)
			{
				var desiredViewPortEnd = Math.Min(index + 1 + ScrollBoundarySize, items.Count);
				desiredViewPortStart = Math.Max(desiredViewPortEnd - ViewPortSize, 0);
			}
			else
			{
				desiredViewPortStart = Math.Max(index - ScrollBoundarySize, 0);
			}

			ViewPortStart = desiredViewPortStart;
			SelectionIndex = index;

			ViewPortChanged(this, ViewPortStart);
			SelectionIndexChanged(this, SelectionIndex);
		}

		public void ResetItems(IEnumerable<TItem> newItems)
		{
			items = newItems.ToList();

			var viewPortChanged = ViewPortStart != 0;
			var selectionChanged = SelectionIndex != 0;

			ViewPortStart = 0;
			SelectionIndex = 0;

			if (viewPortChanged)
			{
				ViewPortChanged(this, 0);
			}
			if (selectionChanged)
			{
				SelectionIndexChanged(this, 0);
			}
		}

		private static void Dummy(object? sender, int args) { }
	}
}
