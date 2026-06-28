using System;
using PassWinmenu.Biometrics;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Biometrics
{
	// Headless (no WPF Application.Current): exercises the no-window fast path.
	public class WindowsHelloPromptTests
	{
		[Fact]
		public void Show_WithoutWpfApp_ReturnsAZeroHandleScope()
		{
			using var prompt = WindowsHelloPrompt.Show("unlock");

			prompt.Handle.ShouldBe(IntPtr.Zero);
		}
	}
}
