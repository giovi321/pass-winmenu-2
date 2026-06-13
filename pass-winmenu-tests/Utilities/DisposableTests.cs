using System;
using PassWinmenu.Utilities;
using Xunit;

namespace PassWinmenuTests.Utilities
{
	/// <summary>
	/// Tests the <see cref="Disposable"/> class.
	/// </summary>
		public class DisposableTests
	{
		[Fact]
		public void _Is_IDisposable()
		{
			var d = new Disposable(() => {});

			Assert.True(d is IDisposable);
		}

		[Fact]
		public void Dispose_DisallowMultipleDispose()
		{
			var i = 0;

			var d = new Disposable(() => i++, allowMultipleDispose: false);

			d.Dispose();
			d.Dispose();

			Assert.Equal(1, i);
		}
		[Fact]
		public void Dispose_AllowMultipleDispose()
		{
			var i = 0;

			var d = new Disposable(() => i++, allowMultipleDispose: true);

			d.Dispose();
			d.Dispose();

			Assert.Equal(2, i);
		}
	}
}
