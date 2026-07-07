using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PassWinmenu.Configuration;

namespace PassWinmenu.Windows.Theming
{
	/// <summary>
	/// Applies the shared, config-driven theme to the whole application:
	/// palette brushes + implicit control styles in the application resources,
	/// and window background/foreground via a class handler (implicit styles
	/// cannot target Window subclasses).
	/// </summary>
	internal static class Theme
	{
		public static ThemePalette? Current { get; private set; }

		private static bool handlerRegistered;

		public static void Apply(StyleConfig style)
		{
			var palette = new ThemePalette(style);
			Current = palette;

			var app = Application.Current;
			if (app == null)
			{
				return;
			}

			app.Resources["ThemeWindowBackgroundBrush"] = Freeze(palette.WindowBackground);
			app.Resources["ThemeForegroundBrush"] = Freeze(palette.Foreground);
			app.Resources["ThemeHintForegroundBrush"] = Freeze(palette.HintForeground);
			app.Resources["ThemeAccentBrush"] = Freeze(palette.Accent);
			app.Resources["ThemeAccentForegroundBrush"] = Freeze(palette.AccentForeground);
			app.Resources["ThemeAccentHoverBrush"] = Freeze(palette.AccentHover);
			app.Resources["ThemeLinkBrush"] = Freeze(palette.Link);
			app.Resources["ThemeControlBackgroundBrush"] = Freeze(palette.ControlBackground);
			app.Resources["ThemeControlBorderBrush"] = Freeze(palette.ControlBorder);
			app.Resources["ThemeControlHoverBrush"] = Freeze(palette.ControlHover);
			app.Resources["ThemeControlPressedBrush"] = Freeze(palette.ControlPressed);

			app.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("pack://application:,,,/src/Windows/Theming/Theme.xaml"),
			});

			if (!handlerRegistered)
			{
				handlerRegistered = true;
				EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
					new RoutedEventHandler(OnWindowLoaded));
			}
		}

		private static void OnWindowLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is not Window window)
			{
				return;
			}
			// Windows that style themselves (SelectionWindow, PasswordDetailsWindow)
			// set these locally; leave them alone.
			if (window.ReadLocalValue(Control.BackgroundProperty) == DependencyProperty.UnsetValue)
			{
				window.SetResourceReference(Control.BackgroundProperty, "ThemeWindowBackgroundBrush");
			}
			if (window.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue)
			{
				window.SetResourceReference(Control.ForegroundProperty, "ThemeForegroundBrush");
			}
		}

		private static SolidColorBrush Freeze(Color colour)
		{
			var brush = new SolidColorBrush(colour);
			brush.Freeze();
			return brush;
		}
	}
}
