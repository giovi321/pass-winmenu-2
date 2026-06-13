using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PassWinmenu.Configuration;
using PassWinmenu.Hotkeys;

#nullable enable
namespace PassWinmenu.Windows
{
	/// <summary>
	/// A window containing a textbox and several labels. One of those labels can be selected
	/// using the keyboard or mouse. Implementers should override <see cref="OnSearchTextChanged(object, TextChangedEventArgs)"/>
	/// in order to specify how search input should be handled.
	/// </summary>
	internal abstract partial class SelectionWindow
	{
		private readonly ScrollableView<string> scrollableView;
		private readonly InterfaceConfig interfaceConfig;
		private readonly bool tryRemainOnTop = true;
		private bool isClosing;
		private bool firstActivation = true;

		protected readonly List<SelectionLabel> Labels = new();
		/// <summary>
		/// The label that is currently selected.
		/// </summary>
		public SelectionLabel SelectedLabel { get; private set; }

		/// <summary>
		/// The text of the currently selected label.
		/// </summary>
		public string SelectionText => SelectedLabel.Text;

		/// <summary>
		/// True if the user has chosen one of the options, false otherwise.
		/// It only makes sense to check this property after the window has closed.
		/// </summary>
		public bool Success { get; protected set; }

		/// <summary>
		/// A search hint to show when the search box is empty.
		/// </summary>
		public string HintText { get; set; }

		/// <summary>
		/// Initialises the window with the provided options.
		/// </summary>
		protected SelectionWindow(InterfaceConfig interfaceConfig, string hint)
		{
			HintText = hint;
			this.interfaceConfig = interfaceConfig;

			// Position and size the window according to user configuration.
			Matrix fromDevice;
			using (var source = new HwndSource(new HwndSourceParameters()))
			{
				if (source.CompositionTarget == null)
				{
					// I doubt this path is ever triggered, but we'll handle it just in case.
					Log.Send("Could not determine the composition target. Window may not be positioned and sized correctly.", LogLevel.Warning);
					// We'll just use the identity matrix here.
					// This works fine as long as the screen's DPI scaling is set to 100%.
					fromDevice = Matrix.Identity;
				}
				else
				{
					fromDevice = source.CompositionTarget.TransformFromDevice;
				}
			}

			var windowPosition = WindowPosition.Calculate(fromDevice, interfaceConfig);
			Left = windowPosition.Position.X;
			Top = windowPosition.Position.Y;
			Width = windowPosition.Dimensions.X;
			MaxHeight = windowPosition.Dimensions.Y;

			InitializeComponent();

			InitialiseLabels(interfaceConfig.Style.Orientation);
			SelectedLabel = Labels[0];
			ApplySelectionStyle(SelectedLabel);

			var styleConfig = interfaceConfig.Style;
			scrollableView = new ScrollableView<string>(
				Array.Empty<string>(),
				new ScrollableViewOptions
				{
					ScrollBoundarySize = styleConfig.ScrollBoundary,
					ViewPortSize = Labels.Count,
				});
			scrollableView.ViewPortChanged += OnViewPortChanged;
			scrollableView.SelectionIndexChanged += OnSelectionIndexChanged;

			SearchBox.BorderBrush = styleConfig.Search.BorderColour;
			SearchBox.CaretBrush = styleConfig.CaretColour;
			SearchBox.Background = styleConfig.Search.BackgroundColour;
			SearchBox.Foreground = styleConfig.Search.TextColour;
			SearchBox.Margin = styleConfig.Search.Margin;
			SearchBox.BorderThickness = styleConfig.Search.BorderWidth;
			SearchBox.FontSize = styleConfig.FontSize;
			SearchBox.FontFamily = new FontFamily(styleConfig.FontFamily);

			Hint.Foreground = styleConfig.SearchHint.TextColour;
			Hint.FontSize = styleConfig.FontSize;
			Hint.FontFamily = new FontFamily(styleConfig.FontFamily);

			Background = styleConfig.BackgroundColour;
			BorderBrush = styleConfig.BorderColour;
			BorderThickness = styleConfig.BorderWidth;
		}

		private void OnSelectionIndexChanged(object? sender, int index)
		{
			var relativeIndex = index - scrollableView.ViewPortStart;
			
			ApplyDeselectionStyle(SelectedLabel);
			SelectedLabel = Labels[relativeIndex];
			ApplySelectionStyle(SelectedLabel);
		}

		private void OnViewPortChanged(object? sender, int viewPortStart)
		{
			var itemsInView = scrollableView.ItemsInView;
			SetLabelContents(itemsInView);
		}


		private void InitialiseLabels(Orientation orientation)
		{
			var labelCount = 10;
			if (orientation == Orientation.Horizontal)
			{
				DockPanel.SetDock(SearchBox, Dock.Left);
				OptionsPanel.Orientation = Orientation.Horizontal;
			}
			else
			{
				// First measure how high the search box wants to be.
				SearchBox.Measure(new System.Windows.Size(double.MaxValue, double.MaxValue));
				// Now find out how much space we have to lay out our labels.
				var availableSpace =
					MaxHeight // Start with the maximum window height
					- Padding.Top - Padding.Bottom // Subtract window padding
					- WindowDock.Margin.Top - WindowDock.Margin.Bottom // Subtract window dock margin
					- SearchBox.DesiredSize.Height // Subtract size of the search box (includes margins)
					- OptionsPanel.Margin.Top - OptionsPanel.Margin.Bottom; // Subtract the margins of the options panel

				var labelHeight = CalculateLabelHeight();

				var labelFit = availableSpace / labelHeight;
				labelCount = (int) labelFit;

				if (!interfaceConfig.Style.ScaleToFit)
				{
					var remainder = (labelFit - labelCount) * labelHeight;
					Log.Send($"Max height: {MaxHeight:F}, Available for labels: {availableSpace:F}, Total used by labels: {labelCount * labelHeight:F}, Remainder: {remainder:F}");
					//MinHeight = MaxHeight;
				}
			}

			for (var i = 0; i < labelCount; i++)
			{
				var label = CreateLabel($"label_{i}");
				Labels.Add(label);
				OptionsPanel.Children.Add(label);
			}
		}

		/// <summary>
		/// Generates a dummy label to measure how high it wants to be.
		/// </summary>
		private double CalculateLabelHeight()
		{
			var sizeTest = CreateLabel("size-test");
			sizeTest.Measure(new System.Windows.Size(double.MaxValue, double.MaxValue));
			var labelHeight = sizeTest.DesiredSize.Height;
			return labelHeight;
		}

		/// <summary>
		/// Handles text input in the textbox.
		/// </summary>
		protected abstract void OnSearchTextChanged(object sender, TextChangedEventArgs e);

		/// <summary>
		/// Resets the labels to the given option strings, and scrolls back to the top.
		/// </summary>
		protected void ResetItems(IEnumerable<string> options)
		{
			scrollableView.ResetItems(options);
			SetLabelContents(scrollableView.ItemsInView);
		}

		private void OnSearchTextChangedInternal(object sender, TextChangedEventArgs e)
		{
			OnSearchTextChanged(sender, e);
		}

		/// <summary>
		/// Sets the contents of the labels to the given values,
		/// hiding any labels that did not receive a value.
		/// </summary>
		private void SetLabelContents(IReadOnlyList<string> values)
		{
			for (var i = 0; i < Labels.Count; i++)
			{
				if (values.Count > i)
				{
					Labels[i].Visibility = Visibility.Visible;
					Labels[i].Text = values[i];
				}
				else
				{
					Labels[i].Visibility = Visibility.Hidden;
				}
			}
		}

		private void ApplySelectionStyle(SelectionLabel label)
		{
			var selectStyle = interfaceConfig.Style.Selection;
			label.Background = selectStyle.BackgroundColour;
			label.Foreground = selectStyle.TextColour;
			label.LabelBorder.BorderBrush = selectStyle.BorderColour;
			label.LabelBorder.BorderThickness = selectStyle.BorderWidth;
		}

		private void ApplyDeselectionStyle(SelectionLabel label)
		{
			var deselectStyle = interfaceConfig.Style.Options;
			label.Background = deselectStyle.BackgroundColour;
			label.Foreground = deselectStyle.TextColour;
			label.LabelBorder.BorderBrush = deselectStyle.BorderColour;
			label.LabelBorder.BorderThickness = deselectStyle.BorderWidth;
		}

		protected SelectionLabel CreateLabel(string content)
		{
			var label = new SelectionLabel(
				content,
				interfaceConfig.Style.Options,
				interfaceConfig.Style.FontSize,
				new FontFamily(interfaceConfig.Style.FontFamily));

			label.MouseLeftButtonUp += (sender, args) =>
			{
				if (label == SelectedLabel)
				{
					HandleConfirm();
				}
				else
				{
					var myIndex = Labels.IndexOf(label);
					scrollableView.SelectIndex(scrollableView.ViewPortStart + myIndex);
				}
				// Return focus to the searchbox so the user can continue typing immediately.
				SearchBox.Focus();
			};
			return label;
		}

		protected override void OnActivated(EventArgs e)
		{
			// If this is the first time the window is activated, we need to do a second call to Activate(),
			// otherwise it won't actually gain focus for some reason ¯\_(ツ)_/¯
			if (firstActivation)
			{
				firstActivation = false;
				Activate();
			}
			base.OnActivated(e);

			// Whenever the window is activated, the search box should gain focus.
			if (!isClosing)
			{
				SearchBox.Focus();
			}
		}

		// Whenever the window loses focus, we reactivate it so it's brought to the front again, allowing it
		// to regain focus.
		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnLostKeyboardFocus(e);
			if (!isClosing && tryRemainOnTop)
			{
				Activate();
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			isClosing = true;
		}

		protected void SetSearchBoxText(string text)
		{
			SearchBox.Text = text;
			SearchBox.CaretIndex = text.Length;
		}

		protected abstract void HandleConfirm();

		private bool IsPressed(HotkeyConfig hotkey)
		{
			// TODO: Don't parse the key combination on every key event
			var combination = KeyCombination.Parse(hotkey.Hotkey);

			if (combination.Key != Key.None)
			{
				if (!Keyboard.IsKeyDown(combination.Key))
				{
					return false;
				}
			}
			return Keyboard.Modifiers == combination.ModifierKeys;
		}

		private void SearchBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			var matches = interfaceConfig.Hotkeys.Where(IsPressed).ToList();

			// Prefer manually defined shortcuts over default shortcuts.
			if (matches.Any())
			{
				e.Handled = true;
				foreach (var match in matches)
				{
					switch (match.Action)
					{
						case HotkeyAction.SelectNext:
							scrollableView.SelectNext();
							break;
						case HotkeyAction.SelectPrevious:
							scrollableView.SelectPrevious();
							break;
						case HotkeyAction.SelectFirst:
							scrollableView.SelectFirst();
							break;
						case HotkeyAction.SelectLast:
							scrollableView.SelectLast();
							break;
					}
				}
				return;
			}

			switch (e.Key)
			{
				case Key.Left:
				case Key.Up:
					e.Handled = true;
					scrollableView.SelectPrevious();
					break;
				case Key.Right:
				case Key.Down:
					e.Handled = true;
					scrollableView.SelectNext();
					break;
				case Key.Enter:
					e.Handled = true;
					HandleConfirm();
					break;
				case Key.Escape:
					e.Handled = true;
					Close();
					break;
			}
		}

		private void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
			{
				scrollableView.SelectPrevious();
			}
			else if (e.Delta < 0)
			{
				scrollableView.SelectNext();
			}
		}
	}
}
