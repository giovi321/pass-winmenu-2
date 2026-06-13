using PassWinmenu.Utilities;
using Shouldly;

namespace PassWinmenuTests.Utilities.ExtensionMethods
{
	internal static class OptionExtensions
	{
		public static void ShouldBeNone<T>(this Option<T> option)
		{
			option.IsNone.ShouldBeTrue();
		}

		public static void ShouldBeSome<T>(this Option<T> option, T value)
		{
			option.IsSome.ShouldBeTrue();
			option.ValueOrDefault().ShouldBe(value);
		}
	}
}
