using System;

#nullable enable
namespace PassWinmenu.Utilities
{
	public readonly struct Option<T>
	{
		private T Value { get; }
		public bool IsSome { get; }
		public bool IsNone => !IsSome;

		public Option(T value, bool hasValue)
		{
			Value = value;
			IsSome = hasValue;
		}

		//  Bit of a hack, but we can't use nullable annotations until C# 9.
		public static Option<T> None => new Option<T>(default!, false);

		public Option<TDst> Select<TDst>(Func<T, TDst> valueMap)
		{
			if (IsSome)
			{
				return new Option<TDst>(valueMap(Value), true);
			}
			return Option<TDst>.None;
		}

		public TDst Match<TDst>(Func<T, TDst> some, Func<TDst> none)
		{
			if (IsSome)
			{
				return some(Value);
			}
			return none();
		}

		public void Match(Action<T>? some, Action? none)
		{
			if (IsSome)
			{
				some?.Invoke(Value);
			}
			else
			{
				none?.Invoke();
			}
		}
	}

	internal static class Option
	{
		public static Option<T> FromNullable<T>(T? value) where T : class => new Option<T>(value!, value != null);

		public static Option<T> Some<T>(T value) => new Option<T>(value, true);

		public static Option<T> None<T>() => new Option<T>(default!, false);
	}

	internal static class OptionExtensions
	{

		public static void Apply<T>(this Option<T> source, Action<T> action)
		{
			source.Match(v => action(v), default);
		}

		public static T? ValueOrDefault<T>(this Option<T> source)
		{
			return source.Match<T?>(v => v, () => default);
		}
	}
}
