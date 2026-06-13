using System.Collections.Generic;
using System.Linq;
using Moq;
using PassWinmenu.Windows;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Windows
{
	public class ScrollableViewTests
	{
		[Fact]
		public void SelectNext_UpdatesIndex()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 100), new ScrollableViewOptions());
			
			view.SelectNext();

			view.SelectionIndex.ShouldBe(1);
		}

		[Fact]
		public void SelectNext_AlreadyAtLastIndex_KeepsIndexAndDoesNotFireEvent()
		{
			var indexRecorder = new EventRecorder();
			var view = new ScrollableView<int>(Enumerable.Range(0, 100), new ScrollableViewOptions());
			view.SelectIndex(99);
			view.SelectionIndexChanged += indexRecorder.RecordIndex;
			
			view.SelectNext();

			view.SelectionIndex.ShouldBe(99);
			indexRecorder.IndexCallData.ShouldBeEmpty();
		}

		[Fact]
		public void SelectPrevious_AlreadyAtFirstIndex_KeepsIndexAndDoesNotFireEvent()
		{
			var indexRecorder = new EventRecorder();
			var view = new ScrollableView<int>(Enumerable.Range(0, 100), new ScrollableViewOptions());
			view.SelectionIndexChanged += indexRecorder.RecordIndex;
			
			view.SelectPrevious();

			view.SelectionIndex.ShouldBe(0);
			indexRecorder.IndexCallData.ShouldBeEmpty();
		}

		[Fact]
		public void SelectIndex_IndexRemainsWithinScrollBounds_ChangesIndexWithoutScrolling()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 100), new ScrollableViewOptions
			{
				ViewPortSize = 20,
				ScrollBoundarySize = 2,
			});
			
			view.SelectIndex(10);

			view.SelectionIndex.ShouldBe(10);
			view.ViewPortStart.ShouldBe(0);
		}

		[Fact]
		public void SelectIndex_IndexOutsideScrollBoundsButCannotScrollFurther_ChangesIndexWithoutScrolling()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 10), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 2,
			});
			
			view.SelectIndex(1);

			view.SelectionIndex.ShouldBe(1);
			view.ViewPortStart.ShouldBe(0);
		}

		[Fact]
		public void SelectIndex_NewIndexIsLowerAndScrollBoundaryReached_ScrollsDown()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 10), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 2,
			});
			
			view.SelectIndex(3);

			view.SelectionIndex.ShouldBe(3);
			view.ViewPortStart.ShouldBe(1);
		}

		[Fact]
		public void SelectIndex_NewIndexIsHigherAndScrollBoundaryReached_ScrollsUp()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 10), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 2,
			});
			view.SelectIndex(5);
			
			view.SelectIndex(4);

			view.SelectionIndex.ShouldBe(4);
			view.ViewPortStart.ShouldBe(2);
		}

		[Fact]
		public void SelectIndex_NewIndexIsLower_ScrollsJustEnoughToBringIndexIntoView()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 10), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 0,
			});
			
			view.SelectIndex(8);

			view.SelectionIndex.ShouldBe(8);
			view.ViewPortStart.ShouldBe(4);
		}

		[Fact]
		public void SelectIndex_NewIndexIsHigherAndOutsideViewPort_ScrollsJustEnoughToBringIndexIntoView()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 10), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 0,
			});
			view.SelectIndex(8);

			view.SelectIndex(3);

			view.SelectionIndex.ShouldBe(3);
			view.ViewPortStart.ShouldBe(3);
		}

		[Fact]
		public void SelectIndex_NewIndexIsHigherButStillWithinViewPort_DoesNotScroll()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 50), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 0,
			});
			view.SelectIndex(30);

			view.SelectIndex(27);

			view.SelectionIndex.ShouldBe(27);
			view.ViewPortStart.ShouldBe(26);
		}

		[Fact]
		public void ItemsInView_ReturnsItemsVisibleInViewPort()
		{
			var view = new ScrollableView<int>(Enumerable.Range(0, 10), new ScrollableViewOptions
			{
				ViewPortSize = 5,
				ScrollBoundarySize = 1,
			});
			view.SelectIndex(8);

			var itemsInView = view.ItemsInView;

			itemsInView.ShouldBe(new List<int>
			{
				5,
				6,
				7,
				8,
				9,
			});
		}

		private class EventRecorder
		{
			public void RecordIndex(object? _, int eventData)
			{
				IndexCallData.Add(eventData);
			}

			public List<int> IndexCallData { get; } = new List<int>();
		}

	}
}
