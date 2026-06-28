# Windows Hello focus + password-generation UI — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Windows Hello unlock prompt reliably focused so a fingerprint touch decrypts, and add a length slider + configurable special-character toggle to the password-generation window.

**Architecture:** Issue 1 — run the Hello call on the WPF UI (STA) thread, grant foreground rights there, drop the topmost helper window, and add a clickable safety-net retry. Issue 2 — add a backward-compatible `special-characters` config block, apply it as a post-processing step in `PasswordGenerator`, derive XKCD word count from a target length, and surface a length slider + special-char toggle through one shared `PasswordGeneratorControl` hosted by both `PasswordWindow` and `EditWindow`.

**Tech Stack:** C# / .NET 8 (`net8.0-windows10.0.19041.0`), WPF, WinRT `KeyCredentialManager`, YamlDotNet, xUnit + Shouldly + Moq.

## Global Constraints

- Build/test with the local SDK (no system SDK on this machine):
  - `$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet-local"`
  - Build: `& "$env:DOTNET_ROOT\dotnet.exe" build pass-winmenu.sln -c Debug`
  - Test: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug`
- Baseline: **230 tests passing**; ~295 pre-existing warnings (CA1416/nullable) are expected and are not errors.
- Tests use **xUnit** (`[Fact]`/`[Theory]`) + **Shouldly** (`.ShouldBe(...)`) + **Moq**. Internals are visible to `pass-winmenu-tests` (`InternalsVisibleTo`).
- Config classes are `public` POCOs under namespace `PassWinmenu.Configuration`; YAML uses kebab-case via `HyphenatedNamingConvention`. Enums need a `PascalCaseEnumConverter<T>` registered in `ConfigurationDeserialiser`.
- **Do not bump `config-version`** (stays `1.7` in `embedded/default-config.yaml`, `Config.cs`, `Program.cs`). The new field is backward-compatible.
- **Session-only persistence:** the generate window must never write the YAML file. Slider/toggle state lives in the window/control; the shared `Config` object's `Length`/`SpecialCharacters.Enabled` are not mutated (character-group `Enabled` toggling keeps its existing in-memory behavior).
- Special-character pool default: `"!@#$%^&*"`. Slider range: **8–64**.
- Commit messages end with a trailing line: `giovi321`.
- Work happens on branch `feature/hello-focus-and-passgen-ui` (already created).

---

## File Structure

**Issue 1 (biometrics):**
- Modify `pass-winmenu/src/Utilities/UiThread.cs` — add `RunOnUi` (UI/STA-thread pumping).
- Modify `pass-winmenu/src/Biometrics/WindowsHelloPrompt.cs` — replace `Show(IDisposable)` with `RunAsync` coordinator (foreground + seamless + safety-net retry).
- Modify `pass-winmenu/src/Biometrics/KeyCredentialBiometricKeyStore.cs` — call `WindowsHelloPrompt.RunAsync`; remove the background-thread `AllowForegroundForHelloPrompt`.
- Modify `pass-winmenu/src/Biometrics/BiometricPassphraseProvider.cs` — call `UiThread.RunOnUi`.
- Test `pass-winmenu-tests/Utilities/UiThreadTests.cs` (new), `pass-winmenu-tests/Biometrics/WindowsHelloPromptTests.cs` (new).

**Issue 2 (password generation):**
- Create `pass-winmenu/src/Configuration/Classes/SpecialCharacterConfig.cs` — `SpecialCharacterConfig` + `SpecialCharacterPlacement` enum.
- Modify `pass-winmenu/src/Configuration/Classes/PasswordGenerationConfig.cs` — add `SpecialCharacters`.
- Modify `pass-winmenu/src/Configuration/ConfigurationDeserialiser.cs` — register the enum converter.
- Modify `pass-winmenu/src/PasswordGeneration/PasswordGenerator.cs` — length-aware overload, `ApplySpecialCharacters`, `ComputeXkcdWordCount`.
- Modify `pass-winmenu/src/PasswordGeneration/XkcdPasswordGenerator.cs` — optional `wordCountOverride`.
- Create `pass-winmenu/src/Windows/PasswordGeneratorControl.xaml` + `.xaml.cs` — shared generator UI (password box, generate button, length slider, special toggle, char-group checkboxes).
- Modify `pass-winmenu/src/Windows/PasswordWindow.xaml` + `.xaml.cs` — host the control; expose `GeneratedPassword`.
- Modify `pass-winmenu/src/Actions/AddPasswordAction.cs` — read `GeneratedPassword`.
- Modify `pass-winmenu/src/Windows/EditWindow.xaml` + `.xaml.cs` — host the control; Replace reads from the control.
- Modify `pass-winmenu/embedded/default-config.yaml` — documented `special-characters` block.
- Test `pass-winmenu-tests/Configuration/SpecialCharacterConfigTests.cs` (new), and additions to `pass-winmenu-tests/PasswordGeneration/PasswordGeneratorTests.cs` + `XkcdPasswordGeneratorTests.cs`.

---

## Task 1: `UiThread.RunOnUi` — run async work on the UI/STA thread

**Files:**
- Modify: `pass-winmenu/src/Utilities/UiThread.cs`
- Test: `pass-winmenu-tests/Utilities/UiThreadTests.cs` (create)

**Interfaces:**
- Produces:
  - `static T UiThread.RunOnUi<T>(Func<Task<T>> work)`
  - `static void UiThread.RunOnUi(Func<Task> work)`
- Behavior contract: when there is no WPF app (`Application.Current == null`, e.g. headless tests and the `pw` CLI), it runs `work` and returns its result without touching any dispatcher.

- [ ] **Step 1: Write the failing tests**

Create `pass-winmenu-tests/Utilities/UiThreadTests.cs`:

```csharp
using System.Threading.Tasks;
using PassWinmenu.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Utilities
{
	// These run headless (no WPF Application.Current), so they exercise the
	// no-WPF-app fast path of RunOnUi.
	public class UiThreadTests
	{
		[Fact]
		public void RunOnUi_Generic_WithoutWpfApp_RunsWorkAndReturnsResult()
		{
			var result = UiThread.RunOnUi(() => Task.FromResult(42));

			result.ShouldBe(42);
		}

		[Fact]
		public void RunOnUi_Void_WithoutWpfApp_RunsWork()
		{
			var ran = false;

			UiThread.RunOnUi(() =>
			{
				ran = true;
				return Task.CompletedTask;
			});

			ran.ShouldBeTrue();
		}
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~UiThreadTests"`
Expected: FAIL — `RunOnUi` does not exist (compile error).

- [ ] **Step 3: Implement `RunOnUi`**

In `pass-winmenu/src/Utilities/UiThread.cs`, add these methods inside the `UiThread` class (keep the existing `RunBlocking` methods):

```csharp
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
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~UiThreadTests"`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add pass-winmenu/src/Utilities/UiThread.cs pass-winmenu-tests/Utilities/UiThreadTests.cs
git commit -m "Add UiThread.RunOnUi to run async work on the UI/STA thread

giovi321"
```

---

## Task 2: `WindowsHelloPrompt.RunAsync` — foreground + seamless + safety-net retry

**Files:**
- Modify: `pass-winmenu/src/Biometrics/WindowsHelloPrompt.cs`
- Test: `pass-winmenu-tests/Biometrics/WindowsHelloPromptTests.cs` (create)

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `static Task<T> WindowsHelloPrompt.RunAsync<T>(string message, Func<CancellationToken, Task<T>> helloCall)`.
- Contract: with no WPF app, awaits `helloCall(CancellationToken.None)` exactly once and returns its result (CLI/headless path). With a WPF app, shows a small **non-topmost** window with a retry button, forces our window to the foreground, grants `AllowSetForegroundWindow(ASFW_ANY)` on the UI thread, runs attempt #1, and lets a button click cancel it and start attempt #2.

- [ ] **Step 1: Write the failing test**

Create `pass-winmenu-tests/Biometrics/WindowsHelloPromptTests.cs`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using PassWinmenu.Biometrics;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Biometrics
{
	// Headless (no WPF Application.Current): exercises the no-window fast path.
	public class WindowsHelloPromptTests
	{
		[Fact]
		public void RunAsync_WithoutWpfApp_InvokesHelloCallOnceAndReturnsResult()
		{
			var calls = 0;

			var result = WindowsHelloPrompt.RunAsync(
				"unlock",
				ct =>
				{
					calls++;
					return Task.FromResult(7);
				}).GetAwaiter().GetResult();

			result.ShouldBe(7);
			calls.ShouldBe(1);
		}
	}
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~WindowsHelloPromptTests"`
Expected: FAIL — `RunAsync` does not exist (compile error).

- [ ] **Step 3: Rewrite `WindowsHelloPrompt`**

Replace the entire contents of `pass-winmenu/src/Biometrics/WindowsHelloPrompt.cs` with:

```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PassWinmenu.Biometrics;

/// <summary>
/// Coordinates a Windows Hello prompt so the system broker dialog is focused and a fingerprint
/// touch authenticates. pass-winmenu runs in the tray and is usually not the foreground process,
/// so the Hello dialog otherwise opens behind the active window.
///
/// Strategy: on the WPF UI (STA) thread show a small, NON-topmost window (so the broker can sit
/// above it), force it to the foreground, grant any process the right to set the foreground window
/// (so the broker can raise itself), then run the Hello call. A "retry" button gives a guaranteed
/// foreground path: a real click restores our foreground rights, cancels the in-flight attempt, and
/// starts a fresh one. No-ops the window when there is no WPF application (the <c>pw</c> CLI).
/// </summary>
internal static class WindowsHelloPrompt
{
	public static async Task<T> RunAsync<T>(string message, Func<CancellationToken, Task<T>> helloCall)
	{
		var app = Application.Current;
		if (app == null)
		{
			// No WPF (CLI/headless): no focus management possible, just run the call.
			return await helloCall(CancellationToken.None);
		}

		return await app.Dispatcher.Invoke(() => RunOnUiAsync(message, helloCall));
	}

	private static async Task<T> RunOnUiAsync<T>(string message, Func<CancellationToken, Task<T>> helloCall)
	{
		var window = CreateWindow(message, out var retryButton);
		try
		{
			window.Show();
			window.Activate();
			ForceForeground(new WindowInteropHelper(window).Handle);
			AllowForegroundForBroker();

			var attempt = new CancellationTokenSource();

			void OnRetry(object sender, RoutedEventArgs e)
			{
				// A genuine click restores our foreground rights; use them to re-raise the broker.
				ForceForeground(new WindowInteropHelper(window).Handle);
				AllowForegroundForBroker();
				var previous = attempt;
				attempt = new CancellationTokenSource();
				previous.Cancel();
			}

			retryButton.Click += OnRetry;
			try
			{
				while (true)
				{
					var current = attempt;
					try
					{
						return await helloCall(current.Token);
					}
					catch (OperationCanceledException) when (current.IsCancellationRequested && !ReferenceEquals(current, attempt))
					{
						// We cancelled this attempt in favour of a retry; loop and await the new one.
					}
				}
			}
			finally
			{
				retryButton.Click -= OnRetry;
			}
		}
		finally
		{
			window.Close();
		}
	}

	private static Window CreateWindow(string message, out Button retryButton)
	{
		retryButton = new Button
		{
			Content = "Authenticate with Windows Hello",
			Padding = new Thickness(10, 4, 10, 4),
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		var panel = new StackPanel { Margin = new Thickness(18) };
		panel.Children.Add(new TextBlock
		{
			Text = message,
			TextWrapping = TextWrapping.Wrap,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(0, 0, 0, 12),
		});
		panel.Children.Add(retryButton);

		return new Window
		{
			Title = "Pass Winmenu 2",
			Width = 340,
			Height = 150,
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			WindowStyle = WindowStyle.ToolWindow,
			ResizeMode = ResizeMode.NoResize,
			ShowInTaskbar = false,
			// NOT topmost: a topmost window covers the Hello broker dialog itself.
			Topmost = false,
			Content = panel,
		};
	}

	private static void AllowForegroundForBroker()
	{
		try
		{
			AllowSetForegroundWindow(ASFW_ANY);
		}
		catch (DllNotFoundException)
		{
			// Non-Windows or stripped environment: focus is best-effort.
		}
	}

	/// <summary>
	/// Forces the given window to the foreground, working around the SetForegroundWindow
	/// restrictions by temporarily attaching to the current foreground window's input queue.
	/// </summary>
	private static void ForceForeground(IntPtr hWnd)
	{
		if (hWnd == IntPtr.Zero)
		{
			return;
		}

		const int SW_SHOW = 5;
		var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
		var currentThread = GetCurrentThreadId();

		if (foregroundThread != currentThread && foregroundThread != 0)
		{
			AttachThreadInput(foregroundThread, currentThread, true);
			BringWindowToTop(hWnd);
			ShowWindow(hWnd, SW_SHOW);
			SetForegroundWindow(hWnd);
			AttachThreadInput(foregroundThread, currentThread, false);
		}
		else
		{
			BringWindowToTop(hWnd);
			ShowWindow(hWnd, SW_SHOW);
			SetForegroundWindow(hWnd);
		}
	}

	private const int ASFW_ANY = -1;

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AllowSetForegroundWindow(int dwProcessId);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool BringWindowToTop(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
```

> Note: `app.Dispatcher.Invoke(() => RunOnUiAsync(...))` returns a `Task<T>` from the UI thread and is awaited from the caller. Combined with Task 3 (the call runs under `UiThread.RunOnUi`), the whole flow stays on the UI thread.

- [ ] **Step 4: Run the test to verify it passes**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~WindowsHelloPromptTests"`
Expected: PASS (1 test). (`KeyCredentialBiometricKeyStore` still references the old API and will be fixed in Task 3; build the test project only — it builds the main project too, so if there are compile errors from Task 3's file, do Task 3 in the same change. If the build breaks here because `KeyCredentialBiometricKeyStore` calls the removed `Show`/`AllowForegroundForHelloPrompt`, proceed directly to Task 3 and run both tests together.)

- [ ] **Step 5: Commit**

```bash
git add pass-winmenu/src/Biometrics/WindowsHelloPrompt.cs pass-winmenu-tests/Biometrics/WindowsHelloPromptTests.cs
git commit -m "Rework WindowsHelloPrompt: non-topmost window, UI-thread foreground grant, retry

giovi321"
```

---

## Task 3: Wire the key store and passphrase provider to the new APIs

**Files:**
- Modify: `pass-winmenu/src/Biometrics/KeyCredentialBiometricKeyStore.cs`
- Modify: `pass-winmenu/src/Biometrics/BiometricPassphraseProvider.cs`
- Test: existing `pass-winmenu-tests/Biometrics/BiometricPassphraseProviderTests.cs` (must keep passing)

**Interfaces:**
- Consumes: `WindowsHelloPrompt.RunAsync` (Task 2), `UiThread.RunOnUi` (Task 1).

- [ ] **Step 1: Update `KeyCredentialBiometricKeyStore`**

In `pass-winmenu/src/Biometrics/KeyCredentialBiometricKeyStore.cs`:

Replace `CreateAsync` body:

```csharp
	public async Task CreateAsync(string credentialName)
	{
		var result = await WindowsHelloPrompt.RunAsync(
			"Setting up Windows Hello for Pass Winmenu 2…",
			_ => KeyCredentialManager.RequestCreateAsync(
				credentialName,
				KeyCredentialCreationOption.ReplaceExisting).AsTask());

		if (result.Status != KeyCredentialStatus.Success)
		{
			throw new BiometricException(
				$"Could not create a Windows Hello credential ({result.Status}).",
				result.Status);
		}
	}
```

Replace the `using (WindowsHelloPrompt.Show(...)) { ... }` block in `SignAsync` with:

```csharp
		var buffer = CryptographicBuffer.CreateFromByteArray(challenge);
		var signResult = await WindowsHelloPrompt.RunAsync(
			"Unlock your passwords with Windows Hello…",
			ct => opened.Credential.RequestSignAsync(buffer).AsTask(ct));

		if (signResult.Status != KeyCredentialStatus.Success)
		{
			throw new BiometricException(
				$"Windows Hello authentication failed ({signResult.Status}).",
				signResult.Status);
		}

		CryptographicBuffer.CopyToByteArray(signResult.Result, out var signature);
		return signature;
```

Delete the now-unused `AllowForegroundForHelloPrompt` method and the `NativeMethods` nested class (the foreground grant now lives in `WindowsHelloPrompt`). Remove the `using System.Runtime.InteropServices;` if it becomes unused.

> `RequestCreateAsync(...).AsTask()` has no cancellation token (enrollment is one-shot); `RequestSignAsync(buffer).AsTask(ct)` takes the token so the retry path can cancel the in-flight gesture.

- [ ] **Step 2: Update `BiometricPassphraseProvider`**

In `pass-winmenu/src/Biometrics/BiometricPassphraseProvider.cs`, change the unlock call in `GetPassphrase`:

```csharp
		// Run the unlock on the UI (STA) thread, pumping the dispatcher, so the Windows Hello sign
		// call is made by the thread that owns the foreground window (required for the broker to focus).
		var result = UiThread.RunOnUi(() => vault.TryUnlockAsync());
```

(Replaces the previous `UiThread.RunBlocking(() => vault.TryUnlockAsync())` line and its comment.)

- [ ] **Step 3: Build and run the biometric tests**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~Biometrics"`
Expected: PASS — all biometric tests (including `BiometricPassphraseProviderTests`, which run headless and exercise the no-WPF path of `RunOnUi`/`RunAsync` via `FakeBiometricKeyStore`).

- [ ] **Step 4: Full build to confirm nothing else broke**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" build pass-winmenu.sln -c Debug`
Expected: build succeeds (warnings OK, no errors).

- [ ] **Step 5: Commit**

```bash
git add pass-winmenu/src/Biometrics/KeyCredentialBiometricKeyStore.cs pass-winmenu/src/Biometrics/BiometricPassphraseProvider.cs
git commit -m "Run Windows Hello unlock on the UI thread via RunAsync/RunOnUi

giovi321"
```

---

## Task 4: `SpecialCharacterConfig` + enum + config wiring

**Files:**
- Create: `pass-winmenu/src/Configuration/Classes/SpecialCharacterConfig.cs`
- Modify: `pass-winmenu/src/Configuration/Classes/PasswordGenerationConfig.cs`
- Modify: `pass-winmenu/src/Configuration/ConfigurationDeserialiser.cs`
- Test: `pass-winmenu-tests/Configuration/SpecialCharacterConfigTests.cs` (create)

**Interfaces:**
- Produces:
  - `enum SpecialCharacterPlacement { End, Start, Random }`
  - `class SpecialCharacterConfig { bool Enabled; string Characters; int Count; SpecialCharacterPlacement Placement; }`
  - `PasswordGenerationConfig.SpecialCharacters` (type `SpecialCharacterConfig`, default non-null).

- [ ] **Step 1: Write the failing tests**

Create `pass-winmenu-tests/Configuration/SpecialCharacterConfigTests.cs`:

```csharp
using PassWinmenu.Configuration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Configuration
{
	public class SpecialCharacterConfigTests
	{
		[Fact]
		public void Defaults_AreSafeAndDisabled()
		{
			var sc = new PasswordGenerationConfig().SpecialCharacters;

			sc.ShouldNotBeNull();
			sc.Enabled.ShouldBeFalse();
			sc.Count.ShouldBe(1);
			sc.Characters.ShouldBe("!@#$%^&*");
			sc.Placement.ShouldBe(SpecialCharacterPlacement.End);
		}

		[Fact]
		public void Deserialise_ReadsAllFields()
		{
			var source = @"
password-store:
  password-generation:
    special-characters:
      enabled: true
      characters: '!@#'
      count: 3
      placement: random";

			var config = ConfigurationDeserialiser.Deserialise<Config>(source.IntoReader());
			var sc = config.PasswordStore.PasswordGeneration.SpecialCharacters;

			sc.Enabled.ShouldBeTrue();
			sc.Characters.ShouldBe("!@#");
			sc.Count.ShouldBe(3);
			sc.Placement.ShouldBe(SpecialCharacterPlacement.Random);
		}

		[Fact]
		public void Deserialise_WhenOmitted_UsesDefaults()
		{
			var source = @"
password-store:
  password-generation:
    length: 24";

			var config = ConfigurationDeserialiser.Deserialise<Config>(source.IntoReader());
			var sc = config.PasswordStore.PasswordGeneration.SpecialCharacters;

			sc.Enabled.ShouldBeFalse();
			sc.Placement.ShouldBe(SpecialCharacterPlacement.End);
		}
	}
}
```

> `IntoReader()` is the existing helper in `PassWinmenuTests.Configuration` (`ConfigurationDeserialiserTests.cs`); it is in the same namespace, so no import is needed.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~SpecialCharacterConfigTests"`
Expected: FAIL — `SpecialCharacters` / `SpecialCharacterConfig` / `SpecialCharacterPlacement` do not exist.

- [ ] **Step 3: Create the config class and enum**

Create `pass-winmenu/src/Configuration/Classes/SpecialCharacterConfig.cs`:

```csharp
namespace PassWinmenu.Configuration
{
	/// <summary>Where extra special characters are inserted into a generated password.</summary>
	public enum SpecialCharacterPlacement
	{
		/// <summary>Append at the end (e.g. "password!").</summary>
		End,

		/// <summary>Prepend at the start (e.g. "!password").</summary>
		Start,

		/// <summary>Insert at random positions within the password.</summary>
		Random,
	}

	/// <summary>
	/// Optional rule for adding special characters to a generated password, to satisfy services with
	/// arbitrary "must contain a special character" policies. Disabled by default; the generate window
	/// exposes a toggle, and these values configure what the toggle does.
	/// </summary>
	public class SpecialCharacterConfig
	{
		/// <summary>Default state of the in-window toggle.</summary>
		public bool Enabled { get; set; } = false;

		/// <summary>Pool of characters to draw from when adding special characters.</summary>
		public string Characters { get; set; } = "!@#$%^&*";

		/// <summary>How many characters to add.</summary>
		public int Count { get; set; } = 1;

		/// <summary>Where the characters are inserted.</summary>
		public SpecialCharacterPlacement Placement { get; set; } = SpecialCharacterPlacement.End;
	}
}
```

- [ ] **Step 4: Add the property to `PasswordGenerationConfig`**

In `pass-winmenu/src/Configuration/Classes/PasswordGenerationConfig.cs`, add after the `Xkcd` property (line 9):

```csharp
		/// <summary>Optional rule for adding special characters to satisfy strict password policies.</summary>
		public SpecialCharacterConfig SpecialCharacters { get; set; } = new SpecialCharacterConfig();
```

- [ ] **Step 5: Register the enum converter**

In `pass-winmenu/src/Configuration/ConfigurationDeserialiser.cs`, add to the builder chain after the `XkcdCapitalisation` converter (line 17):

```csharp
			.WithTypeConverter(new PascalCaseEnumConverter<SpecialCharacterPlacement>())
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~SpecialCharacterConfigTests"`
Expected: PASS (3 tests).

- [ ] **Step 7: Commit**

```bash
git add pass-winmenu/src/Configuration/Classes/SpecialCharacterConfig.cs pass-winmenu/src/Configuration/Classes/PasswordGenerationConfig.cs pass-winmenu/src/Configuration/ConfigurationDeserialiser.cs pass-winmenu-tests/Configuration/SpecialCharacterConfigTests.cs
git commit -m "Add configurable special-character rule to password generation config

giovi321"
```

---

## Task 5: Generator engine — length-aware generation, special chars, XKCD adaptation

**Files:**
- Modify: `pass-winmenu/src/PasswordGeneration/PasswordGenerator.cs`
- Modify: `pass-winmenu/src/PasswordGeneration/XkcdPasswordGenerator.cs`
- Test: `pass-winmenu-tests/PasswordGeneration/PasswordGeneratorTests.cs`, `pass-winmenu-tests/PasswordGeneration/XkcdPasswordGeneratorTests.cs`

**Interfaces:**
- Consumes: `SpecialCharacterConfig`, `SpecialCharacterPlacement` (Task 4).
- Produces:
  - `string? PasswordGenerator.GeneratePassword(int targetLength, bool includeSpecialCharacters)`
  - `int PasswordGenerator.ComputeXkcdWordCount(int targetLength)` (internal, visible to tests)
  - `XkcdPasswordGenerator(XkcdConfig config, string[] words, int? wordCountOverride = null)`
  - The existing parameterless `GeneratePassword()` keeps working and now applies the config's default special-character rule.

- [ ] **Step 1: Write the failing tests (XKCD override)**

Add to `pass-winmenu-tests/PasswordGeneration/XkcdPasswordGeneratorTests.cs` (inside the class):

```csharp
		[Fact]
		public void WordCountOverride_OverridesConfiguredWordCount()
		{
			var cfg = Config();
			cfg.WordCount = 4;

			var pw = new XkcdPasswordGenerator(cfg, Words, wordCountOverride: 6).GeneratePassword();

			pw!.Split('-').Length.ShouldBe(6);
		}
```

- [ ] **Step 2: Write the failing tests (PasswordGenerator)**

Add to `pass-winmenu-tests/PasswordGeneration/PasswordGeneratorTests.cs` (inside the class):

```csharp
		[Fact]
		public void GeneratePassword_SpecialCharactersEnd_AppendsConfiguredCount()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig
				{
					Enabled = true,
					Characters = "!",
					Count = 2,
					Placement = SpecialCharacterPlacement.End,
				},
			};

			var pw = new PasswordGenerator(options).GeneratePassword();

			pw!.Length.ShouldBe(12);
			pw.EndsWith("!!").ShouldBeTrue();
		}

		[Fact]
		public void GeneratePassword_SpecialCharactersStart_PrependsConfiguredCount()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig
				{
					Enabled = true,
					Characters = "!",
					Count = 1,
					Placement = SpecialCharacterPlacement.Start,
				},
			};

			var pw = new PasswordGenerator(options).GeneratePassword();

			pw!.Length.ShouldBe(11);
			pw.StartsWith("!").ShouldBeTrue();
		}

		[Fact]
		public void GeneratePassword_SpecialCharactersDisabled_DoesNotChangeLength()
		{
			var options = new PasswordGenerationConfig
			{
				Length = 10,
				SpecialCharacters = new SpecialCharacterConfig { Enabled = false, Characters = "!", Count = 3 },
			};

			new PasswordGenerator(options).GeneratePassword()!.Length.ShouldBe(10);
		}

		[Fact]
		public void GeneratePassword_WithTargetLength_OverridesConfigLength()
		{
			var options = new PasswordGenerationConfig { Length = 20 };

			new PasswordGenerator(options).GeneratePassword(15, includeSpecialCharacters: false)!.Length.ShouldBe(15);
		}

		[Fact]
		public void ComputeXkcdWordCount_GrowsWithTargetLength()
		{
			var options = new PasswordGenerationConfig { Style = PasswordGenerationStyle.Xkcd };
			options.Xkcd.MinWordLength = 4;
			options.Xkcd.MaxWordLength = 8;
			options.Xkcd.Separator = "-";
			var generator = new PasswordGenerator(options);

			generator.ComputeXkcdWordCount(8).ShouldBeGreaterThanOrEqualTo(1);
			generator.ComputeXkcdWordCount(64).ShouldBeGreaterThan(generator.ComputeXkcdWordCount(16));
		}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~PasswordGeneration"`
Expected: FAIL — new members do not exist (compile errors).

- [ ] **Step 4: Add `wordCountOverride` to `XkcdPasswordGenerator`**

In `pass-winmenu/src/PasswordGeneration/XkcdPasswordGenerator.cs`:

Change the field/ctor (lines 18-25) to:

```csharp
		private readonly XkcdConfig config;
		private readonly string[] words;
		private readonly int? wordCountOverride;

		public XkcdPasswordGenerator(XkcdConfig config, string[] words, int? wordCountOverride = null)
		{
			this.config = config;
			this.words = words;
			this.wordCountOverride = wordCountOverride;
		}
```

Change the word-count line (was `var wordCount = Math.Max(1, config.WordCount);`) to:

```csharp
			var wordCount = Math.Max(1, wordCountOverride ?? config.WordCount);
```

- [ ] **Step 5: Update `PasswordGenerator`**

In `pass-winmenu/src/PasswordGeneration/PasswordGenerator.cs`:

Add `using System.Text;` to the usings.

Replace `GeneratePassword()` / `GenerateXkcd()` / `GenerateRandom()` (lines 20-71) with:

```csharp
		public string? GeneratePassword()
		{
			return GeneratePassword(Options.Length, Options.SpecialCharacters.Enabled);
		}

		/// <summary>
		/// Generates a password of approximately <paramref name="targetLength"/> characters. For the
		/// random generator this is the exact character count; for the XKCD generator the word count is
		/// derived from it (see <see cref="ComputeXkcdWordCount"/>), so the realised length is close but
		/// not exact. When <paramref name="includeSpecialCharacters"/> is true the configured
		/// special-character rule is applied on top of the generated password.
		/// </summary>
		public string? GeneratePassword(int targetLength, bool includeSpecialCharacters)
		{
			var password = Options.Style == PasswordGenerationStyle.Xkcd
				? GenerateXkcd(ComputeXkcdWordCount(targetLength))
				: GenerateRandom(targetLength);

			if (password == null)
			{
				return null;
			}

			return includeSpecialCharacters ? ApplySpecialCharacters(password) : password;
		}

		/// <summary>
		/// Derives how many words an XKCD passphrase needs to reach roughly <paramref name="targetLength"/>
		/// characters, using the configured separator and the midpoint of the word-length band.
		/// </summary>
		internal int ComputeXkcdWordCount(int targetLength)
		{
			var xkcd = Options.Xkcd;
			var separatorLength = xkcd.RandomNumberSeparator ? 1 : (xkcd.Separator?.Length ?? 0);
			var midWordLength = Math.Max(1, (xkcd.MinWordLength + xkcd.MaxWordLength) / 2);
			var wordCount = (int)Math.Round((double)(targetLength + separatorLength) / (midWordLength + separatorLength));
			return Math.Max(1, wordCount);
		}

		private string? GenerateXkcd(int wordCount)
		{
			var words = LoadWordList(Options.Xkcd.WordListFile);
			return new XkcdPasswordGenerator(Options.Xkcd, words, wordCount).GeneratePassword();
		}

		private string? GenerateRandom(int length)
		{
			if (!Options.CharacterGroups.Any(g => g.Enabled))
			{
				return null;
			}

			// Build a complete set of all characters in all enabled groups
			var completeCharSet = new HashSet<int>();
			foreach (var group in Options.CharacterGroups.Where(g => g.Enabled))
			{
				completeCharSet.UnionWith(group.CharacterSet);
			}

			// Transform the set into a list, to assign an index to each character.
			var charList = completeCharSet.ToList();

			// Generate as many random list indices as we need to build a password.
			var indices = GetIntegers(charList.Count, length);

			// Transform the list of indices into a list of characters.
			var characters = indices.Select(i => charList[i]).ToArray();

			var password = string.Join("", characters.Select(char.ConvertFromUtf32));
			return password;
		}

		/// <summary>
		/// Adds the configured special characters to <paramref name="password"/>. The caller decides
		/// whether the rule is active; this method only applies the count/placement.
		/// </summary>
		private string ApplySpecialCharacters(string password)
		{
			var special = Options.SpecialCharacters;
			if (special.Count <= 0 || string.IsNullOrEmpty(special.Characters))
			{
				return password;
			}

			var pool = special.Characters;
			var result = new StringBuilder(password);
			for (var i = 0; i < special.Count; i++)
			{
				var ch = pool[(int)GetRandomInteger((uint)pool.Length)];
				switch (special.Placement)
				{
					case SpecialCharacterPlacement.Start:
						result.Insert(0, ch);
						break;
					case SpecialCharacterPlacement.Random:
						result.Insert((int)GetRandomInteger((uint)(result.Length + 1)), ch);
						break;
					case SpecialCharacterPlacement.End:
					default:
						result.Append(ch);
						break;
				}
			}

			return result.ToString();
		}
```

> The existing parameterless `GeneratePassword()` is preserved (now delegating), so `GeneratePassword_MatchesRequiredLength` and `GeneratePassword_OnlyContainsAllowedCharacters` keep passing (special chars default to disabled).

- [ ] **Step 6: Run the tests to verify they pass**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~PasswordGeneration"`
Expected: PASS — all password-generation tests (old + new).

- [ ] **Step 7: Commit**

```bash
git add pass-winmenu/src/PasswordGeneration/PasswordGenerator.cs pass-winmenu/src/PasswordGeneration/XkcdPasswordGenerator.cs pass-winmenu-tests/PasswordGeneration/PasswordGeneratorTests.cs pass-winmenu-tests/PasswordGeneration/XkcdPasswordGeneratorTests.cs
git commit -m "Add length-targeted generation, special-char post-processing, XKCD adaptation

giovi321"
```

---

## Task 6: Shared `PasswordGeneratorControl`

**Files:**
- Create: `pass-winmenu/src/Windows/PasswordGeneratorControl.xaml`
- Create: `pass-winmenu/src/Windows/PasswordGeneratorControl.xaml.cs`

**Interfaces:**
- Consumes: `PasswordGenerator` (Task 5), `PasswordGenerationConfig` / `SpecialCharacterConfig` (Task 4).
- Produces (public members used by Tasks 7 & 8):
  - `void Initialize(PasswordGenerationConfig config)`
  - `string GeneratedPassword { get; }`
  - `void FocusPassword()`

- [ ] **Step 1: Create the XAML**

Create `pass-winmenu/src/Windows/PasswordGeneratorControl.xaml`:

```xml
<UserControl x:Class="PassWinmenu.Windows.PasswordGeneratorControl"
             x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
	<StackPanel>
		<Grid>
			<Grid.ColumnDefinitions>
				<ColumnDefinition Width="*" />
				<ColumnDefinition Width="Auto" />
			</Grid.ColumnDefinitions>
			<TextBox x:Name="Password" Grid.Column="0" Height="23" Padding="3,3,3,3" TextWrapping="NoWrap"
			         VerticalAlignment="Top" FontFamily="Consolas" Margin="0,0,8,0" />
			<Button x:Name="Btn_Generate" Grid.Column="1" Content="Generate" Width="82" Height="23"
			        VerticalAlignment="Top" Click="Btn_Generate_Click" />
		</Grid>

		<Grid Margin="0,10,0,0">
			<Grid.ColumnDefinitions>
				<ColumnDefinition Width="Auto" />
				<ColumnDefinition Width="*" />
				<ColumnDefinition Width="Auto" />
			</Grid.ColumnDefinitions>
			<TextBlock Grid.Column="0" Text="Length:" VerticalAlignment="Center" Margin="0,0,8,0" />
			<Slider x:Name="LengthSlider" Grid.Column="1" Minimum="8" Maximum="64" VerticalAlignment="Center"
			        IsSnapToTickEnabled="True" TickFrequency="1" />
			<TextBlock x:Name="Lbl_Length" Grid.Column="2" VerticalAlignment="Center" MinWidth="90"
			           TextAlignment="Right" Margin="8,0,0,0" />
		</Grid>

		<CheckBox x:Name="Chk_Special" Margin="0,10,0,0" Content="Add special characters" />

		<Grid x:Name="CharacterGroups" Margin="0,10,0,0" />
	</StackPanel>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

Create `pass-winmenu/src/Windows/PasswordGeneratorControl.xaml.cs`:

```csharp
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PassWinmenu.Configuration;
using PassWinmenu.PasswordGeneration;

#nullable enable
namespace PassWinmenu.Windows
{
	internal sealed partial class PasswordGeneratorControl : UserControl
	{
		private PasswordGenerator? passwordGenerator;
		private bool initialised;

		public PasswordGeneratorControl()
		{
			InitializeComponent();
		}

		/// <summary>The currently generated password (the editable text in the box).</summary>
		public string GeneratedPassword => Password.Text;

		public void FocusPassword() => Password.Focus();

		/// <summary>
		/// Wires the control to a generation config and produces the first password. The config object
		/// is used in memory only; nothing here writes the YAML file.
		/// </summary>
		public void Initialize(PasswordGenerationConfig config)
		{
			passwordGenerator = new PasswordGenerator(config);

			// Length slider: start at the configured length, clamped to the slider range.
			var initialLength = config.Length;
			if (initialLength < LengthSlider.Minimum)
			{
				initialLength = (int)LengthSlider.Minimum;
			}
			else if (initialLength > LengthSlider.Maximum)
			{
				initialLength = (int)LengthSlider.Maximum;
			}

			LengthSlider.Value = initialLength;
			LengthSlider.ValueChanged += (_, _) => Regenerate();

			// Special-character toggle: only meaningful when a pool is configured.
			var special = config.SpecialCharacters;
			if (string.IsNullOrEmpty(special.Characters))
			{
				Chk_Special.Visibility = Visibility.Collapsed;
			}
			else
			{
				Chk_Special.Content = $"Add special characters ({special.Characters})";
				Chk_Special.IsChecked = special.Enabled;
				Chk_Special.Checked += (_, _) => Regenerate();
				Chk_Special.Unchecked += (_, _) => Regenerate();
			}

			// Character-group checkboxes only apply to the random generator.
			if (config.Style == PasswordGenerationStyle.Random)
			{
				CreateCheckboxes();
			}
			else
			{
				CharacterGroups.Visibility = Visibility.Collapsed;
			}

			initialised = true;
			Regenerate();
		}

		private void CreateCheckboxes()
		{
			const int colCount = 3;
			var index = 0;
			foreach (var charGroup in passwordGenerator!.Options.CharacterGroups)
			{
				var x = index % colCount;
				var y = index / colCount;

				var cbx = new CheckBox
				{
					Name = charGroup.Name,
					Content = charGroup.Name,
					Margin = new Thickness(x * 100, y * 20, 0, 0),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					IsChecked = charGroup.Enabled,
				};
				cbx.Unchecked += HandleCheckedChanged;
				cbx.Checked += HandleCheckedChanged;
				CharacterGroups.Children.Add(cbx);

				index++;
			}
		}

		private void Regenerate()
		{
			if (!initialised || passwordGenerator == null)
			{
				return;
			}

			var targetLength = (int)LengthSlider.Value;
			var includeSpecial = Chk_Special.Visibility == Visibility.Visible && Chk_Special.IsChecked == true;

			Password.Text = passwordGenerator.GeneratePassword(targetLength, includeSpecial);
			Password.CaretIndex = Password.Text?.Length ?? 0;

			if (passwordGenerator.Options.Style == PasswordGenerationStyle.Xkcd)
			{
				var words = passwordGenerator.ComputeXkcdWordCount(targetLength);
				Lbl_Length.Text = $"{targetLength}  (≈{words} words)";
			}
			else
			{
				Lbl_Length.Text = $"{targetLength}";
			}
		}

		private void Btn_Generate_Click(object sender, RoutedEventArgs e) => Regenerate();

		private void HandleCheckedChanged(object sender, RoutedEventArgs e)
		{
			var checkbox = (CheckBox)sender;
			passwordGenerator!.Options.CharacterGroups.First(c => c.Name == checkbox.Name).Enabled =
				checkbox.IsChecked ?? false;

			Regenerate();
		}
	}
}
```

> The `initialised` guard prevents `Regenerate` from running when `LengthSlider.Value` is set before the generator/handlers are ready.

- [ ] **Step 3: Build to verify the control compiles**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" build pass-winmenu.sln -c Debug`
Expected: build succeeds (the control isn't hosted yet; that's Tasks 7-8).

- [ ] **Step 4: Commit**

```bash
git add pass-winmenu/src/Windows/PasswordGeneratorControl.xaml pass-winmenu/src/Windows/PasswordGeneratorControl.xaml.cs
git commit -m "Add shared PasswordGeneratorControl with length slider and special-char toggle

giovi321"
```

---

## Task 7: Host the control in `PasswordWindow` + update `AddPasswordAction`

**Files:**
- Modify: `pass-winmenu/src/Windows/PasswordWindow.xaml`
- Modify: `pass-winmenu/src/Windows/PasswordWindow.xaml.cs`
- Modify: `pass-winmenu/src/Actions/AddPasswordAction.cs`

**Interfaces:**
- Consumes: `PasswordGeneratorControl.Initialize`, `.GeneratedPassword`, `.FocusPassword` (Task 6).
- Produces: `PasswordWindow.GeneratedPassword` (string) for `AddPasswordAction`.

- [ ] **Step 1: Replace the generator markup in `PasswordWindow.xaml`**

Replace the whole `<DockPanel>...</DockPanel>` top portion that holds the password box, generate button, and `CharacterGroups` grid. The new file:

```xml
<Window x:Class="PassWinmenu.Windows.PasswordWindow"
        x:ClassModifier="internal"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:windows="clr-namespace:PassWinmenu.Windows"
        Title="Choose a Password" Height="380" Width="380" MinHeight="300" MinWidth="380" ResizeMode="CanResizeWithGrip" KeyUp="Window_KeyUp">
	<Grid>
		<DockPanel LastChildFill="True" Margin="10,10,10,40">
			<Label x:Name="Lbl_Password" DockPanel.Dock="Top" Content="Password:" HorizontalAlignment="Left" />
			<windows:PasswordGeneratorControl x:Name="Generator" DockPanel.Dock="Top" Margin="0,2,0,0" />
			<Label x:Name="Lbl_ExtraContent" DockPanel.Dock="Top" Content="Extra content:" Margin="0,10,0,0" />
			<TextBox DockPanel.Dock="Top" x:Name="ExtraContent" Padding="3,3,3,3" TextWrapping="Wrap" AcceptsReturn="True"
			         AcceptsTab="True" CaretIndex="10" FontFamily="Consolas" />
		</DockPanel>
		<StackPanel Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="10,10,10,10">
			<Button x:Name="Btn_Cancel" Content="Cancel" Width="75" Click="Btn_Cancel_Click" />
			<Button x:Name="Btn_OK" Margin="10,0,0,0" Content="OK" IsDefault="True" Click="Btn_OK_Click" Width="75" />
		</StackPanel>
	</Grid>
</Window>
```

- [ ] **Step 2: Update `PasswordWindow.xaml.cs`**

Replace the file with:

```csharp
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using PassWinmenu.Configuration;

namespace PassWinmenu.Windows
{
	internal sealed partial class PasswordWindow
	{
		private readonly PasswordGenerationConfig passwordGenerationConfig;

		public PasswordWindow(string filename, PasswordGenerationConfig passwordGenerationConfig)
		{
			this.passwordGenerationConfig = passwordGenerationConfig;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			Title = "Add new password";

			Generator.Initialize(passwordGenerationConfig);
			AddDefaultMetadata(filename);
			Generator.FocusPassword();
		}

		/// <summary>The password chosen/generated by the user.</summary>
		public string GeneratedPassword => Generator.GeneratedPassword;

		private void AddDefaultMetadata(string filename)
		{
			var now = DateTime.Now;
			var extraContent = passwordGenerationConfig.DefaultContent
				.Replace("$filename", filename)
				.Replace("$date", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
				.Replace("$time", now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
			ExtraContent.Text = extraContent;
		}

		private void Btn_OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				DialogResult = false;
				Close();
			}
		}
	}
}
```

- [ ] **Step 3: Update `AddPasswordAction.cs`**

In `pass-winmenu/src/Actions/AddPasswordAction.cs`, change line 65 from:

```csharp
			var password = passwordWindow.Password.Text;
```

to:

```csharp
			var password = passwordWindow.GeneratedPassword;
```

(`passwordWindow.ExtraContent.Text` on line 66 is unchanged.)

- [ ] **Step 4: Build**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" build pass-winmenu.sln -c Debug`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add pass-winmenu/src/Windows/PasswordWindow.xaml pass-winmenu/src/Windows/PasswordWindow.xaml.cs pass-winmenu/src/Actions/AddPasswordAction.cs
git commit -m "Host PasswordGeneratorControl in PasswordWindow

giovi321"
```

---

## Task 8: Host the control in `EditWindow`

**Files:**
- Modify: `pass-winmenu/src/Windows/EditWindow.xaml`
- Modify: `pass-winmenu/src/Windows/EditWindow.xaml.cs`

**Interfaces:**
- Consumes: `PasswordGeneratorControl` (Task 6). The EditWindow-specific **Replace** button stays in `EditWindow` and reads `Generator.GeneratedPassword`.

- [ ] **Step 1: Replace the generator markup in `EditWindow.xaml`**

Replace the `<GroupBox Header="Password generator">...</GroupBox>` block with one that hosts the shared control plus the Replace button, and add the `windows` xmlns to the `<Window>` root:

```xml
<Window x:Class="PassWinmenu.Windows.EditWindow"
        x:ClassModifier="internal"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:system="clr-namespace:System;assembly=mscorlib"
        xmlns:windows="clr-namespace:PassWinmenu.Windows"
        Title="" Height="400" Width="430" MinHeight="300" MinWidth="380" ResizeMode="CanResizeWithGrip"
        KeyUp="Window_KeyUp">
	<Grid>
		<DockPanel>
			<GroupBox Header="Password generator" DockPanel.Dock="Top" VerticalAlignment="Top" Margin="5,15,5,0">
				<StackPanel>
					<windows:PasswordGeneratorControl x:Name="Generator" Margin="5,5,5,0" />
					<Button x:Name="Btn_Replace" Content="Replace first line" Margin="5,10,5,5" Click="Btn_Replace_Click"
					        Width="120" Height="23" HorizontalAlignment="Right" />
				</StackPanel>
			</GroupBox>
			<Label x:Name="Lbl_ExtraContent" DockPanel.Dock="Top" Content="Password file contents" Margin="10,0,10,0"
			       VerticalAlignment="Top" />
			<Grid DockPanel.Dock="Top">
				<TextBox x:Name="PasswordContent" Margin="10,10,10,45" Padding="3,3,3,3" TextWrapping="Wrap" AcceptsReturn="True"
				         AcceptsTab="True" CaretIndex="10" FontFamily="Consolas" GotFocus="HandlePasswordContentFocus"
				         LostFocus="HandlePasswordContentFocus" TextChanged="PasswordContent_TextChanged">
				</TextBox>
				<Rectangle x:Name="PasswordDivider" Stroke="#569de5" Margin="10,28,10,0" StrokeThickness="1" StrokeDashArray="2 3" Height="1" VerticalAlignment="Top" />
			</Grid>
		</DockPanel>

		<Button x:Name="Btn_OK" Content="Save" Margin="0,0,90,10" IsDefault="True" Click="Btn_OK_Click" Height="20"
		        VerticalAlignment="Bottom" HorizontalAlignment="Right" Width="75" />
		<Button x:Name="Btn_Cancel" Content="Cancel" HorizontalAlignment="Right" Margin="0,0,10,10"
		        VerticalAlignment="Bottom" Width="75" Click="Btn_Cancel_Click" />

	</Grid>
</Window>
```

- [ ] **Step 2: Update `EditWindow.xaml.cs`**

Replace the file with:

```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PassWinmenu.Configuration;

#nullable enable
namespace PassWinmenu.Windows
{
	internal sealed partial class EditWindow
	{
		private readonly string originalContent;

		public EditWindow(string path, string content, PasswordGenerationConfig options)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			InitializeComponent();

			Generator.Initialize(options);

			Title = $"Editing '{path}'";

			originalContent = content.Replace(Environment.NewLine, "\n");

			PasswordContent.Text = content;
			PasswordContent.Focus();
		}

		private void Btn_Replace_Click(object sender, RoutedEventArgs e)
		{
			var content = PasswordContent.Text.Replace(Environment.NewLine, "\n");
			var index = content.IndexOf('\n');
			var password = Generator.GeneratedPassword;
			PasswordContent.Text = index == -1 ? password : password + content.Remove(0, index);
		}

		private void Btn_OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				DialogResult = false;
				Close();
			}
		}

		private void HandlePasswordContentFocus(object sender, RoutedEventArgs e)
		{
			if (PasswordContent.IsFocused)
			{
				PasswordDivider.Stroke = new SolidColorBrush(Color.FromRgb(86, 157, 229));
			}
			else
			{
				PasswordDivider.Stroke = new SolidColorBrush(Color.FromRgb(171, 173, 179));
			}
		}

		private void PasswordContent_TextChanged(object sender, TextChangedEventArgs e)
		{
			Btn_OK.IsEnabled = PasswordContent.Text.Replace(Environment.NewLine, "\n") != originalContent;
		}
	}
}
```

- [ ] **Step 3: Build**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" build pass-winmenu.sln -c Debug`
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add pass-winmenu/src/Windows/EditWindow.xaml pass-winmenu/src/Windows/EditWindow.xaml.cs
git commit -m "Host PasswordGeneratorControl in EditWindow

giovi321"
```

---

## Task 9: Document the `special-characters` block in the default config

**Files:**
- Modify: `pass-winmenu/embedded/default-config.yaml`
- Test: existing `pass-winmenu-tests/Configuration/ConfigFileTests.cs` (must keep passing)

- [ ] **Step 1: Add the documented block**

In `pass-winmenu/embedded/default-config.yaml`, immediately after the `length: 20` line (the "Default length for newly generated passwords." setting) and before the `character-groups:` block, insert:

```yaml
        # Optional rule for adding special characters to generated passwords, to satisfy
        # services with arbitrary "must contain a special character" policies. The password
        # generation window shows a toggle; these settings control what that toggle does.
        special-characters:
            # Default state of the in-window toggle.
            enabled: false
            # Pool of characters to draw from.
            characters: '!@#$%^&*'
            # How many characters to add.
            count: 1
            # Where to put them:
            #   end    - append at the end:        "password!"
            #   start  - prepend at the start:     "!password"
            #   random - insert at random positions
            placement: end
```

(Indentation: the block is nested under `password-generation:`, the same indentation level as `length:` — 8 spaces.)

- [ ] **Step 2: Verify the YAML still parses**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~ConfigFileTests"`
Expected: PASS — `ConfigFile_IsValidYaml`.

- [ ] **Step 3: Verify the documented block deserializes into the config**

This is covered by extending the existing valid-yaml check with a round-trip assertion. Add to `pass-winmenu-tests/Configuration/ConfigFileTests.cs` (inside the class, and add `using PassWinmenu.Configuration;` + `using Shouldly;` at the top):

```csharp
		[Fact]
		public void ConfigFile_SpecialCharactersBlock_Deserialises()
		{
			using var reader = File.OpenText(@"..\..\..\..\pass-winmenu\embedded\default-config.yaml");
			var config = ConfigurationDeserialiser.Deserialise<Config>(reader);

			var sc = config.PasswordStore.PasswordGeneration.SpecialCharacters;
			sc.Characters.ShouldBe("!@#$%^&*");
			sc.Placement.ShouldBe(SpecialCharacterPlacement.End);
		}
```

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug --filter "FullyQualifiedName~ConfigFileTests"`
Expected: PASS (2 tests).

- [ ] **Step 4: Commit**

```bash
git add pass-winmenu/embedded/default-config.yaml pass-winmenu-tests/Configuration/ConfigFileTests.cs
git commit -m "Document special-characters block in default config

giovi321"
```

---

## Task 10: Full verification + README

**Files:**
- Modify: `README.md` (document the new options)

- [ ] **Step 1: Full build**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" build pass-winmenu.sln -c Debug`
Expected: build succeeds, no errors.

- [ ] **Step 2: Full test run**

Run: `& "$env:DOTNET_ROOT\dotnet.exe" test pass-winmenu-tests/pass-winmenu-tests.csproj -c Debug`
Expected: PASS — baseline 230 + new tests (UiThread ×2, WindowsHelloPrompt ×1, SpecialCharacterConfig ×3, PasswordGenerator ×5, XkcdPasswordGenerator ×1, ConfigFile ×1) = **243 passing**, 0 failures.

- [ ] **Step 3: Update README**

In `README.md`, find the password-generation / configuration section and add a short note describing: the in-window **length slider** (8–64; for XKCD mode word count/length adapt automatically), the **special-characters toggle**, and the `special-characters` config block (`enabled`, `characters`, `count`, `placement`). Match the surrounding documentation style.

- [ ] **Step 4: Commit**

```bash
git add README.md
git commit -m "Document password length slider and special-character options

giovi321"
```

- [ ] **Step 5: Manual verification (requires a desktop session — ask the user to run)**

These cannot be automated (WPF UI + a real fingerprint sensor). Ask the user to confirm:

1. **Hello focus:** trigger a decrypt that needs a Hello gesture. The unlock window appears focused and **touching the fingerprint sensor decrypts** without clicking anything. If the broker ever opens behind, the "Authenticate with Windows Hello" button brings it forward and the touch then works.
2. **Add password:** open the add-password window. The length slider moves and the password regenerates live; the label shows the length (and ≈word count in XKCD mode). The special-characters toggle adds/removes the configured characters. Character-group checkboxes still work in random mode.
3. **Edit password:** the same generator controls appear; "Replace first line" swaps in the generated password.
4. **Config:** set `special-characters` → `characters: '!'`, `count: 1`, `placement: end`, `enabled: true`; confirm new passwords end with `!` and the window toggle starts on.

---

## Self-Review

**Spec coverage:**
- Issue 1 root causes (background thread, topmost, MTA) → Tasks 1-3. ✓
- Seamless + safety-net retry → Task 2 (`RunAsync` with retry button). ✓
- Special-char full rule (pool/count/placement) → Tasks 4-5, config + engine. ✓
- Length slider = total length; XKCD adapts → Task 5 (`GeneratePassword(targetLength, …)`, `ComputeXkcdWordCount`) + Task 6 (slider). ✓
- Session-only persistence → control uses slider/toggle local state; config not written; no version bump (Task 9). ✓
- Shared control across both windows → Tasks 6-8. ✓
- Slider range 8–64, special chars added on top → Task 6 XAML + Task 5 engine. ✓

**Placeholder scan:** No TBD/TODO; every code step shows full code; commands have expected output. ✓

**Type consistency:** `RunOnUi`, `RunAsync<T>(string, Func<CancellationToken, Task<T>>)`, `GeneratePassword(int, bool)`, `ComputeXkcdWordCount(int)`, `XkcdPasswordGenerator(…, int?)`, `PasswordGeneratorControl.Initialize/GeneratedPassword/FocusPassword`, `PasswordWindow.GeneratedPassword` — names match across consuming tasks. ✓
