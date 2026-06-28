# Design: Windows Hello focus fix + password-generation UI

Date: 2026-06-28
Status: Approved
Scope: pass-winmenu (WPF app)

## Problem

1. **Biometric prompt not focused.** When a decrypt triggers the Windows Hello
   gesture, the system broker dialog opens behind the active window and never
   takes focus, so touching the fingerprint sensor does not authenticate and the
   password is not decrypted. An earlier "show a top-most window on top"
   workaround did not help.

2. **No in-window control over generated password shape.** When generating a new
   password the user cannot choose the length from the generate window, and there
   is no toggle to append special characters to satisfy services with arbitrary
   password policies. The special-character rule must be user-configurable.

## Decisions (from brainstorming)

- **Hello focus:** seamless first, with a clickable safety-net retry.
- **Special chars:** full rule — configurable char pool + count + placement.
- **Length slider:** controls *total character length*; in word (XKCD) mode the
  word count and word length adapt automatically.
- **Persistence:** session-only. The generate window never rewrites the YAML
  config; in-window changes affect only the current generation.
- **Slider range:** 8–64.
- **Special chars are added on top** of the slider length (e.g. length 24 with a
  trailing `!` yields 25 chars). The live character-count label always shows the
  true final length.

---

## Issue 1 — Windows Hello focus

### Root cause (confirmed in code)

The unlock runs on a background thread-pool thread
(`UiThread.RunBlocking` → `Task.Run`, `src/Utilities/UiThread.cs:22`). Three
things sabotage focus:

1. `AllowSetForegroundWindow(ASFW_ANY)` is called on that background thread
   (`src/Biometrics/KeyCredentialBiometricKeyStore.cs:58`), after the process's
   transient foreground rights (from the just-handled hotkey) have lapsed.
2. The helper prompt window is `Topmost = true`
   (`src/Biometrics/WindowsHelloPrompt.cs:49`) — it sits *above* the Hello broker
   dialog, covering the prompt it is meant to surface. (This is the failed
   workaround.)
3. `RequestSignAsync` is invoked from a thread-pool (MTA) thread rather than the
   STA UI thread that owns the foreground window, so WinRT cannot associate the
   consent UI with our foreground window.

### Chosen approach

Run the Hello call on the UI/STA thread, grant foreground rights there, drop
`Topmost`, and add a clickable safety-net retry.

Rejected alternatives:
- Marshalling only `AllowSetForegroundWindow` to the UI thread while keeping the
  background thread — leaves the MTA-thread problem; likely still flaky.
- Replacing `KeyCredentialManager` with `UserConsentVerifier` +
  `IUserConsentVerifierInterop` (HWND parenting) — rejected: it returns only a
  yes/no consent, not the TPM-backed signature used to derive the AES key. Would
  break the encryption model in `PassphraseProtector`.

### Components

**`UiThread.RunOnUi<T>(Func<Task<T>> work)` (new)**
- Ensures `work` runs on the WPF UI (STA) thread and pumps `DispatcherFrame`s
  until the returned task completes, so WinRT continuations marshal back via the
  WPF `SynchronizationContext` and `RequestSignAsync` executes on the STA thread.
- If already on the UI thread: start `work()` inline, pump frames until complete.
- If on another thread but a WPF app exists: `Dispatcher.Invoke` the pumping
  helper onto the UI thread.
- If there is no WPF app (the `pw` CLI): run `work()` directly
  (`.GetAwaiter().GetResult()`), preserving current CLI behavior.
- A non-generic `RunOnUi(Func<Task>)` overload mirrors the existing
  `RunBlocking` overloads.

**`BiometricPassphraseProvider.GetPassphrase`**
- Calls `UiThread.RunOnUi(() => vault.TryUnlockAsync(...))` instead of
  `RunBlocking`. No other behavioral change (outcome handling unchanged).

**`WindowsHelloPrompt` (reworked into a focus + retry coordinator)**
- New API:
  `Task<T> RunAsync<T>(string message, Func<CancellationToken, Task<T>> helloCall)`.
- Behavior (all on the UI thread):
  1. Show a small prompt window: `Topmost = false`, `ShowInTaskbar = false`,
     `WindowStyle = ToolWindow`, centered; content is the message
     ("Touch your fingerprint sensor to unlock…") plus a button
     **"Authenticate with Windows Hello"**.
  2. Force our window to the foreground (existing `AttachThreadInput` /
     `SetForegroundWindow` dance in `ForceForeground`) and call
     `AllowSetForegroundWindow(ASFW_ANY)` **on the UI thread while we hold
     foreground**.
  3. Start attempt #1 (seamless): `helloCall(cts1.Token)`.
  4. **Safety net:** the button's click handler (a genuine user input event →
     guaranteed foreground rights) re-runs the foreground dance, cancels
     attempt #1 (`cts1.Cancel()`, which cancels the in-flight `IAsyncOperation`
     via `AsTask(token)`), and starts attempt #2: `helloCall(cts2.Token)`.
  5. Returns the result of whichever attempt actually completes; an attempt we
     cancel ourselves is swallowed (never surfaced as a user cancellation).
  6. Closes the window on completion (success or failure).
- The old `WindowsHelloPrompt.Show(...)` `IDisposable` form and the
  `Topmost = true` / background-thread `AllowSetForegroundWindow` are removed.
- When there is no WPF app, `RunAsync` simply awaits `helloCall` with no window
  (CLI fallback).

**`KeyCredentialBiometricKeyStore`**
- `SignAsync` / `CreateAsync` invoke their WinRT call through
  `WindowsHelloPrompt.RunAsync(message, ct => …RequestSignAsync(buffer).AsTask(ct))`
  (and the create equivalent). Status checks stay in the key store. The old
  `AllowForegroundForHelloPrompt()` helper is removed.

### Caveat

Foreground behavior is OS/timing/hardware-sensitive. The threading + ASFW +
non-topmost changes target the root cause; the click safety-net is the
guaranteed path. Final validation is manual testing on the user's fingerprint
sensor.

---

## Issue 2 — Generation window: length slider + special-char toggle

### Configuration

New block under `password-store → password-generation` in the embedded
`default-config.yaml`:

```yaml
special-characters:
  enabled: false          # default state of the in-window toggle
  characters: "!@#$%^&*"  # pool of characters to draw from
  count: 1                # how many to insert
  placement: end          # end | start | random
```

- New class `SpecialCharacterConfig` with properties `Enabled` (bool, default
  `false`), `Characters` (string, default `"!@#$%^&*"`), `Count` (int, default
  `1`), `Placement` (enum, default `End`).
- New enum `SpecialCharacterPlacement { End, Start, Random }`, registered with
  the existing `PascalCaseEnumConverter` in `ConfigurationDeserialiser`.
- Added as `SpecialCharacters` property on `PasswordGenerationConfig`
  (default `new SpecialCharacterConfig()`).
- `config-version` bumped (and `Program.LastConfigVersion` updated to match) so
  existing users get the upgrade prompt; the new field has a safe default for
  configs that omit it.

### Generation engine

- **Special characters applied as a post-processing step** in `PasswordGenerator`
  for *both* Random and XKCD modes — cleaner than editing each generator.
  `ApplySpecialCharacters(string base, bool enabled)`:
  - If disabled or `count <= 0` or `characters` empty: return `base` unchanged.
  - Draw `count` characters from the pool using the existing crypto RNG.
  - Placement `End`: append; `Start`: prepend; `Random`: insert each at a random
    index.
  - Added on top of the base length.
- **XKCD adaptive word count.** Given target length `L` (from the slider),
  separator length `sepLen`, and the configured `MinWordLength`/`MaxWordLength`:
  - `midWordLen = (MinWordLength + MaxWordLength) / 2`
  - `n = max(1, round((L + sepLen) / (midWordLen + sepLen)))`
  - Generate `n` words from the configured length band. Realized length tracks
    the slider closely but is not exact (words are discrete); the window shows
    the realized length and word count.
  - The window passes the desired length/word-count into generation per
    regeneration (session-only override of `Options.Length` / `Options.Xkcd.WordCount`);
    the config object on disk is not modified.
- Random mode uses `L` exactly for the base length.

### UI

**Refactor:** the near-duplicate generator UI in `PasswordWindow` and
`EditWindow` is extracted into one shared WPF `UserControl`
`PasswordGeneratorControl` (in `src/Windows/`), owning: the password textbox,
the regenerate button, the length slider + live label, the special-character
toggle, and the character-group checkboxes. Both windows host this control and
read the final password from it. This removes duplication before new controls
are added.

**New controls in the shared control:**
- **Length slider** (range 8–64), initialized to config `length` (clamped),
  with a live label: `Length: N` and, in XKCD mode, `(≈ M words)`. Moving the
  slider regenerates live.
- **Special-character toggle** (checkbox), label reflects the configured pool,
  e.g. `Add special characters (!@#$%^&*)`. Initialized to
  `special-characters.enabled`. Toggling regenerates live. Hidden/disabled if no
  pool is configured.
- **Character-group checkboxes** retained, shown in Random mode only (current
  behavior).
- Light styling pass to match the rest of the app (the window is currently
  unstyled).

Layout sketch:

```
┌─ Add password ──────────────────────────────┐
│ Password:  [ Xy7$kappa-delta-river!      ] 🔄│
│ Length:    [======●===========]  24  (≈4 wds)│
│ [✓] Add special characters (!@#$%^&*)        │
│ Character groups:  (Random mode only)        │
│   [✓]Symbols [✓]Numeric [✓]Lower [✓]Upper    │
│ Extra content:                               │
│   [ Username:                            ]   │
│              [ Cancel ]            [  OK  ]   │
└──────────────────────────────────────────────┘
```

**Persistence:** session-only. Slider/toggle/checkbox changes mutate only the
in-memory generation options for the current window; the YAML file is untouched.
Mode (Random vs XKCD) stays driven by config.

---

## Testing

- **Issue 1:** Unit-test `UiThread.RunOnUi` thread/affinity behavior where
  feasible (STA, pumping, CLI fallback). The Hello broker focus itself requires
  manual verification on the user's hardware (seamless path + click safety net).
- **Issue 2:**
  - `SpecialCharacterConfig` deserialization (all fields, defaults when omitted).
  - `PasswordGenerator.ApplySpecialCharacters` for each placement, count > pool
    size, empty pool, disabled.
  - XKCD adaptive word-count derivation across slider range (monotonic, ≥ 1).
  - Existing password-generation tests continue to pass.
  - Config version upgrade path.

## Out of scope

- Switching generation mode (Random/XKCD) from the window.
- Persisting in-window choices back to the YAML file.
- Configurable slider bounds (fixed at 8–64).
