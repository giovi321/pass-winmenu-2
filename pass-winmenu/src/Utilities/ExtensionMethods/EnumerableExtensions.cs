using System;
using System.Collections.Generic;
using System.Linq;

namespace PassWinmenu.Utilities.ExtensionMethods
{
	internal static class EnumerableExtensions
	{
		public static IEnumerable<TSource> ConcatSingle<TSource>(this IEnumerable<TSource> source, TSource element)
		{
			return source.Concat(new[] {element});
		}
	}
}
