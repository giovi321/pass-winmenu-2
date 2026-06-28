using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PassWinmenu.Utilities
{
	/// <summary>
	/// Helpers for synchronously waiting on asynchronous work without freezing the WPF UI thread.
	/// </summary>
	internal static class UiThread
	{
		/// <summary>
		/// Runs <paramref name="work"/> on a background thread and waits for it. When called on the
		/// WPF UI thread, the dispatcher is pumped while waiting, so the UI stays responsive and
		/// background threads can still marshal back to the UI thread (e.g. to show the Windows Hello
		/// prompt window). Hard-blocking instead would deadlock that marshalling.
		/// </summary>
		public static T RunBlocking<T>(Func<Task<T>> work)
		{
			var dispatcher = Application.Current?.Dispatcher;
			var task = Task.Run(work);

			if (dispatcher != null && dispatcher.CheckAccess())
			{
				var frame = new DispatcherFrame();
				task.ContinueWith(
					_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
					TaskScheduler.Default);
				Dispatcher.PushFrame(frame);
			}

			return task.GetAwaiter().GetResult();
		}

		public static void RunBlocking(Func<Task> work)
		{
			RunBlocking(async () =>
			{
				await work();
				return true;
			});
		}

		/// <summary>
		/// Runs <paramref name="work"/> on the WPF UI (STA) thread and pumps the dispatcher until it
		/// completes, so WinRT continuations (e.g. the Windows Hello sign call) marshal back to the UI
		/// thread and the call is made by the thread that owns the foreground window. Falls back to
		/// running <paramref name="work"/> directly when there is no WPF application (the <c>pw</c> CLI
		/// and headless tests).
		/// </summary>
		public static T RunOnUi<T>(Func<Task<T>> work)
		{
			var dispatcher = Application.Current?.Dispatcher;
			if (dispatcher == null)
			{
				return work().GetAwaiter().GetResult();
			}

			if (dispatcher.CheckAccess())
			{
				return PumpUntilComplete(work);
			}

			return dispatcher.Invoke(() => PumpUntilComplete(work));
		}

		public static void RunOnUi(Func<Task> work)
		{
			RunOnUi(async () =>
			{
				await work();
				return true;
			});
		}

		/// <summary>
		/// Starts <paramref name="work"/> on the current (UI) thread and pumps a nested dispatcher
		/// frame until the returned task finishes, keeping the UI responsive without leaving the thread.
		/// </summary>
		private static T PumpUntilComplete<T>(Func<Task<T>> work)
		{
			var task = work();
			if (!task.IsCompleted)
			{
				var frame = new DispatcherFrame();
				task.ContinueWith(
					_ => Application.Current!.Dispatcher.BeginInvoke(new Action(() => frame.Continue = false)),
					TaskScheduler.Default);
				Dispatcher.PushFrame(frame);
			}

			return task.GetAwaiter().GetResult();
		}
	}
}
