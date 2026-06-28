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

### Chosen approach (final)

The first attempt (run on the UI/STA thread + `AllowSetForegroundWindow` +
non-topmost window) did not fix it. Research confirmed why:
`KeyCredentialManager` **has no way to parent or focus its dialog**, and
`AllowSetForegroundWindow(ASFW_ANY)` is a no-op unless the process already holds
the foreground privilege — which a global hotkey never grants. Winning the focus
race with `SetForegroundWindow` tricks is therefore unreliable.

The robust fix (the one KeePassWinHello ships for this exact hotkey scenario) is
to **drop `KeyCredentialManager` and use the NCrypt/CNG layer**, which exposes
`NCRYPT_WINDOW_HANDLE_PROPERTY` to parent the Hello gesture dialog to our window
so it is always focused.

Rejected alternatives:
- `UserConsentVerifier` + `IUserConsentVerifierInterop` (HWND parenting) — returns
  only a yes/no consent, not key material; can't protect the passphrase.
- Foreground tricks alone (`SetForegroundWindow` / synthetic ALT / `AttachThreadInput`)
  — best-effort only; kept solely to raise our host window, not relied on for the
  dialog's focus.

### Security model (unchanged seam)

`IBiometricKeyStore`, `BiometricVault`, `IPassphraseProtector` (AES-256-GCM),
`IBiometricBlobStore`, and all their tests are **unchanged**. The new key store
implements the existing deterministic `SignAsync(challenge) → bytes` contract via
NCrypt: at enrolment it generates a random 32-byte secret, encrypts it with the
NGC RSA key (no gesture) and stores the ciphertext; at unlock it decrypts that
ciphertext (gesture-gated, HWND-parented). RSA decryption is deterministic, so
the recovered secret is stable and the AES-GCM protector keeps working. The
secret is unrecoverable without a successful Hello gesture on this machine's TPM.

### Components

**`NgcBiometricKeyStore` (new, replaces `KeyCredentialBiometricKeyStore`)**
- Backed by the "Microsoft Passport Key Storage Provider" via NCrypt P/Invoke.
- `CreateAsync`: `NCryptCreatePersistedKey` (RSA-2048, `OverwriteExistingKey`),
  sets `Length`, `Key Usage` (decrypt), `NgcCacheType = AUTH_MANDATORY` (force a
  gesture per use), the `HWND Handle`, then `NCryptFinalizeKey`; generates a
  random secret, `NCryptEncrypt`s it (PKCS#1) and writes the ciphertext to
  `biometric.hellokey` next to the config.
- `SignAsync`: reads the ciphertext, opens the key, sets `HWND Handle` +
  `PinCacheIsGestureRequired`, and `NCryptDecrypt`s (single call — the gesture
  fires here) to recover the deterministic secret.
- `ExistsAsync` / `DeleteAsync`: open / delete the NGC key and the sidecar file.
- `IsAvailableAsync`: `KeyCredentialManager.IsSupportedAsync()` (no UI).
- Status mapping: `NTE_USER_CANCELLED` / `ERROR_CANCELLED` →
  `KeyCredentialStatus.UserCanceled`; `NTE_NO_KEY` / `NTE_BAD_KEYSET` →
  `NotFound`; else a generic `BiometricException` → `Failed`.

**`WindowsHelloPrompt` (host window providing the parent HWND)**
- `Show(message) : IPromptWindow` shows a small non-topmost window on the UI
  thread, force-foregrounds it (synthetic ALT keypress + `AttachThreadInput` +
  `SetForegroundWindow`, best-effort), and exposes `Handle`. `Dispose` closes it.
- No WPF app (the `pw` CLI / headless tests) → `Handle == IntPtr.Zero`, no window.

**`BiometricPassphraseProvider` / `EnrollBiometricsAction`**
- Keep `UiThread.RunBlocking` (background thread + dispatcher pump): NCrypt is
  synchronous and blocks during the gesture, so it must run off the UI thread
  while the host window stays responsive. (The interim `UiThread.RunOnUi` helper
  is removed.)

### Caveat

The HWND-parenting fix is deterministic, but the real gesture path can only be
validated on hardware with a fingerprint sensor. Switching key stores invalidates
any existing enrolment (different key), so the user re-enrols once; the vault's
existing `KeyInvalidated → ClearEnrollment` path handles the stale blob
gracefully.

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
- **No `config-version` bump.** A version bump triggers `LoadResult.NeedsUpgrade`,
  which backs up the user's config, regenerates from the template, and halts
  (`ConfigurationLoader.cs:67-80`) — disruptive. The new field is fully
  backward-compatible (safe default when omitted), so existing configs keep
  working untouched; the documented block is added to the embedded
  `default-config.yaml` for new installs and anyone who regenerates.

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
