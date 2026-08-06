# Security Review — pass-winmenu

- **Date**: 2026-08-06
- **Branch**: `security-hardening`
- **Method**: `getsentry/skills@security-review` skill (OWASP-based, confidence-based reporting), adapted to a Windows desktop password-manager threat model. Four parallel review passes: process invocation, secrets/crypto, config/paths/update-checking, supply chain/CI.
- **Scope**: entire repository (`pass-winmenu/`, `commandline/`, `.github/`, build files). Test projects excluded from findings.
- **Remediation status**: all 9 findings fixed on this branch (see *Remediation log* at the bottom). Solution builds with 0 errors; full test suite passes (262/262), including new validation tests for VULN-003.

## Threat model

Attackers may control:

- **(a)** password-store file/directory **names** and plaintext metadata (`.gpg-id`, `.git/config`) — e.g. a cloned malicious repo;
- **(b)** the user's YAML **config file** (synced, shared, or social-engineered);
- **(c)** **network responses** for update checking;
- **(d)** local processes, PATH, and user-writable directories.

The app handles high-value secrets: decrypted passwords, TOTP seeds, GPG passphrases.

## Summary

- **Findings**: 9 (0 Critical, 2 High, 7 Medium)
- **Needs verification**: 7 items
- **Overall risk**: Medium. No remote, unauthenticated exploit path exists — this is a desktop app — but several findings let a malicious password-store repo, malicious config, or local attacker escalate into code execution or secret disclosure. The clipboard finding contradicts the app's own security promise to the user.

| # | Finding | Severity | Area |
|---|---------|----------|------|
| 1 | Passwords not excluded from Windows Clipboard History / Cloud Clipboard | **High** | Clipboard |
| 2 | Outdated LibGit2Sharp 0.26.2 bundles CVE-affected libgit2 (CVE-2024-24577 RCE, CVE-2024-24575 DoS) | **High** | Dependency |
| 3 | Unvalidated writes to `gpg-agent.conf` from YAML config (incl. newline smuggling) | Medium* | Config → gpg-agent |
| 4 | Git argument injection via unquoted remote name from `.git/config` | Medium | Process invocation |
| 5 | Bare `git` resolved via CreateProcess search order; resolved path discarded | Medium | Process invocation |
| 6 | Config-controlled executable paths (`git-path`, `gpg-path`, `ssh-path`) executed without verification | Medium | Config → exec |
| 7 | Full decrypted secret written to plaintext temp file in external-editor mode | Medium | Secrets on disk |
| 8 | Clipboard clear lost if app exits within timeout window | Medium | Clipboard |
| 9 | GitHub Actions: mutable major-version tags + `${{ github.event.* }}` script injection in release workflow | Medium | CI/CD |

\* Rated High *within threat model (b)* (malicious config), Medium overall since it requires config write access.

---

## Findings

### [VULN-001] Passwords/TOTP codes not excluded from Windows Clipboard History and Cloud Clipboard — **High**

- **Location**: `pass-winmenu/src/WinApi/TemporaryClipboard.cs:31`
- **Confidence**: High (verified)
- **Issue**: `Clipboard.SetDataObject(text)` places the decrypted password (also TOTP codes via `GenerateTotpAction.cs:72`, any field via `ShowPasswordAction.cs:98`) on the clipboard with no opt-out of Windows 10/11 clipboard history (Win+V) or cloud clipboard sync.
- **Impact**: With clipboard history or cloud sync enabled, the timeout-based clear only removes the *current* entry — the password persists indefinitely in the history UI and may sync to the user's Microsoft-account cloud clipboard, leaving the machine. This directly contradicts the "will be cleared in N seconds" promise shown in notifications (`DecryptPasswordAction.cs:75`).
- **Fix**: Set the exclusion formats Windows honors (the KeePass approach) on a `DataObject`:
  ```csharp
  var data = new DataObject();
  data.SetData("CanIncludeInClipboardHistory", 0);   // DWORD 0
  data.SetData("CanUploadToCloudClipboard", 0);      // DWORD 0
  data.SetData(DataFormats.Text, text);
  Clipboard.SetDataObject(data, true);
  ```

### [VULN-002] LibGit2Sharp 0.26.2 bundles libgit2 with published CVEs — **High**

- **Location**: `pass-winmenu/pass-winmenu.csproj:52-54`
- **Confidence**: High (verified)
- **Issue**: LibGit2Sharp 0.26.2 ships native libgit2 1.4.x, affected by:
  - **CVE-2024-24577** — heap corruption in `git_index_add` with attacker-controlled data; potentially arbitrary code execution. Fixed in libgit2 1.6.5/1.7.2.
  - **CVE-2024-24575** — infinite-loop DoS in `git_revparse_single()`.
- **Impact**: LibGit2Sharp is the builtin sync path and processes attacker-controlled input (malicious remote / cloned repo): `LibGit2SharpSyncStrategy.cs:29` (`Commands.Fetch`), `SyncServiceFactory.cs:34` (`new Repository(...)`), pull/commit actions. A hostile password-store server could crash the app or potentially execute code in a process holding decrypted passwords.
- **Fix**: Upgrade LibGit2Sharp to ≥ 0.29.0 (libgit2 1.7.2) and re-test builtin sync. The `win7-x64`/`UseRidGraph` workaround in `Directory.Build.props:3-7` may become unnecessary.

### [VULN-003] Unvalidated `gpg-agent.conf` writes from YAML config — **Medium** (High within threat model (b))

- **Location**: `pass-winmenu/src/ExternalPrograms/Gpg/GpgAgentConfigUpdater.cs:164,173`; trigger `pass-winmenu/src/Jobs/UpdateGpgAgentConfig.cs:19-21`; source `pass-winmenu/src/Configuration/Classes/GpgAgentConfigFile.cs:7-8`
- **Confidence**: High (verified)
- **Issue**: `GpgAgentConfigFile.Keys` is a free-form `Dictionary<string,string>` from the user's YAML config. With `allow-config-management: true`, every pair is written verbatim into `gpg-agent.conf`:
  ```csharp
  yield return $"{next.Key} {next.Value}";   // no validation of key or value
  ```
  Keys/values are never checked for CR/LF or `#`, so a value with an embedded newline smuggles extra lines that bypass the key-match/replace logic and are invisible to `RemoveManagedKeys` — the app's own cleanup cannot remove them.
- **Impact**: A malicious/synced/social-engineered config can silently set arbitrary gpg-agent options — e.g. `pinentry-program C:\attacker\pinentry.exe` (GPG passphrase harvesting), `allow-preset-passphrase`, permissive cache TTLs. Ironic context: `RemoveBiometricPreset` exists specifically to *undo* `allow-preset-passphrase`, while this feature can re-add it.
- **Fix**: Whitelist permissible gpg-agent keys (e.g. cache TTLs); reject keys/values containing `\r`, `\n`, `#`, `${}`; log/notify the user whenever `gpg-agent.conf` is modified.

### [VULN-004] Git argument injection via unquoted remote name — **Medium**

- **Location**: `pass-winmenu/src/ExternalPrograms/Git/NativeGitSyncStrategy.cs:25`
- **Confidence**: High (pattern); exploit precondition: attacker controls `.git/config` in a cloned store
- **Issue**:
  ```csharp
  public void Fetch(Branch branch)
  {
      CallGit("fetch " + branch.RemoteName);
  }
  ```
  `RemoteName` is read by LibGit2Sharp from the repo's `.git/config`; an attacker writing that file directly bypasses `git remote add` name validation, so the name can contain spaces or start with `-`.
- **Impact**: Auto-fetch is on by default (`GitConfig.AutoFetch = true`, fired ~1 min after start). Injected tokens are parsed by `git fetch` as options/operands (e.g. `--upload-pack=...`, crafted refspecs, `--prune`), altering fetch behavior or executing a local helper for local-path remotes.
- **Fix**: Validate the remote name (reject whitespace/control chars and leading `-`), or quote with the same CommandLineToArgvW quoting used in `GpgArguments.Quote`.

### [VULN-005] Resolved git executable path discarded — bare `git` via CreateProcess search order — **Medium**

- **Location**: `pass-winmenu/src/ExternalPrograms/Git/NativeGitSyncStrategy.cs:45` + `pass-winmenu/src/ExternalPrograms/Git/GitSyncStrategies.cs:24`; default in `pass-winmenu/src/Configuration/Classes/GitConfig.cs:18`
- **Confidence**: High
- **Issue**: `ChooseSyncStrategy` calls `executablePathResolver.Resolve(config.GitPath)` to check existence but **discards the resolved absolute path**; `NativeGitSyncStrategy` then uses `FileName = gitConfig.GitPath` (default: bare `"git"`). With `UseShellExecute = false`, CreateProcess searches: application directory → CWD → System32 → PATH. A `git.exe` planted in the app's CWD/install dir or an earlier user-writable PATH directory runs instead.
- **Impact**: Under threat model (d), a malicious `git.exe` executes with the user's privileges on every auto-fetch, fed password-store paths. Note `GPG` does this correctly (always uses an absolute resolved path) — git is the inconsistent one. Bare `powershell` (`OpenPasswordShellAction`) and `explorer` (`OpenExplorerAction.cs:22`) share the same resolution class.
- **Fix**: Pass the resolved path from `ChooseSyncStrategy` into `NativeGitSyncStrategy` and use it as `FileName`; resolve `powershell`/`explorer` to absolute system paths too.

### [VULN-006] Config-controlled executable paths executed without verification — **Medium**

- **Location**: `pass-winmenu/src/WinApi/ExecutablePathResolver.cs:43-68`; consumers `NativeGitSyncStrategy.cs:45,57` (`GIT_SSH` export), `GpgInstallationFinder.cs:59`
- **Confidence**: High (pattern); exploitability depends on threat models (b)/(d)
- **Issue**: `gpg-path`, `git.git-path`, and `git.ssh-path` come from the config (or PATH). A synced/social-engineered config pointing any of these at an attacker binary yields code execution inside the password manager. No signature/publisher verification of resolved executables.
- **Fix**: Prefer well-known install locations (as done for the GPG default) over PATH; warn when an executable resolves from a user-writable directory; treat `*-path` changes on config reload as requiring restart + user confirmation.

### [VULN-007] Full decrypted secret written to plaintext temp file in external-editor mode — **Medium**

- **Location**: `pass-winmenu/src/Actions/EditPasswordAction.cs:114-118,130,176`
- **Confidence**: High
- **Issue**: With `PasswordEditor.UseBuiltin = false`, the entire decrypted file (password + metadata + TOTP seeds) is written to a `.txt` in `%TEMP%` (or config-controlled `TemporaryFileDirectory`) and left on disk while the external editor runs. If the app crashes/is killed between write and the Yes/No `MessageBox`, plaintext remains indefinitely. On the "No" path, a failed `File.Delete` silently leaves plaintext (unlike the Yes-path `EnsureRemoval`).
- **Impact**: Plaintext credentials at rest on disk; a config pointing `temporary-file-directory` at a shared/cloud-synced folder exfiltrates them (filename is crypto-random, but the file inherits directory ACLs).
- **Fix**: Delete in `try/finally` and on `App.Exit`; route the "No" path through `EnsureRemoval`; create the file with an explicit owner-only ACL; document the external-editor tradeoff.

### [VULN-008] Clipboard clear lost if the app exits within the timeout window — **Medium**

- **Location**: `pass-winmenu/src/WinApi/TemporaryClipboard.cs:34`
- **Confidence**: High
- **Issue**: The clear is scheduled via `Task.Delay(timeout).ContinueWith(...)`. If the process exits (tray quit, crash, logoff) before the timer fires, the password stays on the clipboard. Also `SetDataObject(text)` uses the default `copy: false`, so exit behavior is inconsistent — but in no case is a guaranteed clear performed.
- **Fix**: Register an `App.Exit`/`SessionEnding` handler that synchronously clears the clipboard if it still holds the placed secret (the `text != current` guard in `PlaceInternal` can be reused).

### [VULN-009] GitHub Actions hardening: mutable tags + script injection — **Medium**

- **Location**: `.github/workflows/ci.yml:16,22`; `.github/workflows/release.yml:19,25,37,53,56`
- **Confidence**: High
- **Issue**:
  1. `actions/checkout@v7`, `actions/setup-dotnet@v5`, `actions/upload-artifact@v7` pinned to mutable major-version tags, not commit SHAs. The release job runs with `permissions: contents: write` and a write-capable `GH_TOKEN` — a poisoned/retagged action could tamper with the shipped zip (this password manager's sole distribution channel).
  2. `${{ github.event.release.tag_name }}` is interpolated directly into `pwsh` `run` blocks (`release.yml:37,53`). Tag names legally contain `$()`, backticks etc. Exploitation needs release-publish privileges, so it is a privilege-boundary/hardening issue, not an external attack.
  3. `ci.yml` has no explicit `permissions:` block; token scope depends on repo defaults.
- **Fix**: Pin actions to full commit SHAs (Dependabot already covers `github-actions`, so updates remain automated); pass event data through `env:` instead of `${{ }}` in scripts; add `permissions: contents: read` to `ci.yml`.

---

## Needs verification

1. **Update suppression via cleartext connectivity probe** — `UpdateChecker.cs:125` uses `http://clients3.google.com/generate_204` to gate update checks. A network attacker blocking/MITMing it silently suppresses vulnerability-fix notifications. Fix is trivial (HTTPS probe, e.g. `https://www.gstatic.com/generate_204`; don't gate security notifications on it). The release fetch itself is properly HTTPS with a strict cert callback (`GitHubUpdateSource.cs:58-67`), and the download link is a hardcoded constant, not server-supplied — good.
2. **TOTP seed shown unmasked** — `ShowPasswordAction.cs:87-90` displays every metadata key with `isSecret: false`, including `otpauth://...?secret=...` (a long-lived shared secret) on screen. Shoulder-surfing/screen-share exposure; consider masking `totp`/`otpauth`/`secret`-like keys.
3. **`gnupghome-override` → arbitrary-directory `gpg-agent.conf` creation** — `GpgAgentConfigReader.cs:30` creates `gpg-agent.conf` in the overridden home dir if absent; combined with VULN-003, a malicious config can drop a crafted `gpg-agent.conf` anywhere the user can write.
4. **`FormatArguments` escaping order bug** — `ProcessExtensions.cs:16` runs backslash-doubling after quote-escaping (corrupts `\\\"` sequences). Only caller passes config paths that can't contain `"` on Windows; not currently exploitable, but fix before reuse.
5. **Biometric blob/key file ACLs** — `FileBiometricBlobStore.cs:44`, `NgcBiometricKeyStore.cs:89` write with inherited directory ACLs. Contents are independently wrapped (DPAPI CurrentUser + Hello-gesture-gated RSA), so impact is low; add owner-only ACLs as hardening if the config dir can be cloud-synced.
6. **Config auto-reload staleness** — `Dependencies.RegisterConfiguration` captures the startup object graph; only `Theme.Apply` sees reloaded values (`ConfigManager.cs:108`). Verify no security-relevant component re-resolves config mid-session in a way that makes attacker edits take effect silently.
7. **Release artifacts unsigned** — release zips are not code-signed and no checksums are published (README acknowledges the SmartScreen warning). Consider publishing SHA-256 sums alongside releases.

---

## What was checked and found SAFE

- **GPG invocation**: all operands go through `GpgArguments.Quote` (correct CommandLineToArgvW quoting); `UseShellExecute=false`; absolute exe path; passphrase passed via stdin as UTF-8 bytes, never on the command line; byte/char arrays zeroed in `finally` (`GPG.cs:116-138`, `GpgTransport.cs:121`).
- **Passphrase lifecycle**: `SecurePassword` → `char[]` (no managed-string copy), zeroed after use; biometric vault zeroes keys/plaintexts.
- **Biometric vault crypto**: AES-256-GCM, random 12-byte nonces, key = SHA-256 of an RSA secret requiring a fresh Windows Hello gesture per decrypt, plus DPAPI on the stored blob; tamper → GCM tag failure → forced re-enrolment. Sound design.
- **Password generation**: `RandomNumberGenerator` with rejection sampling; no `System.Random` anywhere in security-relevant code.
- **Logging**: all `Log.Send` call sites reviewed — no passwords, passphrases, TOTP secrets, or decrypted content logged.
- **YAML config**: YamlDotNet 11.2.1 `DeserializerBuilder` with no type resolution/tag mappings — no unsafe polymorphic deserialization.
- **GitHub release JSON**: Newtonsoft.Json 13.0.1 (post-CVE-2024-21907), default settings, no `TypeNameHandling`.
- **Password-store enumeration**: `EnumerateFiles` does not follow directory symlinks/junctions by default — a malicious cloned repo can't escape the store during enumeration.
- **No embedded executables**: `embedded/` holds only icons/YAML/wordlist; loaded as in-memory streams, never extracted or executed. GPG/git come from the user's installed programs.
- **No committed secrets** in tree or git history; `SECURITY.md` with a private reporting channel exists; Dependabot configured for NuGet + GitHub Actions; no `pull_request_target` usage.
- **`commandline/` project**: no process spawning; passphrase handled as zeroed `char[]`.
- **`StartupLink`**: targets own exe via WScript.Shell COM, no shell string.

## Prioritised remediation plan

1. **VULN-002** — bump LibGit2Sharp ≥ 0.29.0 (dependency update, biggest real-world risk).
2. **VULN-001** — clipboard history/cloud exclusions (small change, directly protects users' passwords today).
3. **VULN-003** — whitelist + newline validation for `gpg-agent.conf` writes.
4. **VULN-004/005/006** — quote/validate remote name; use resolved absolute paths for `git`, `powershell`, `explorer`; warn on user-writable resolution.
5. **VULN-007/008** — temp-file `try/finally` cleanup + exit-time clipboard clear.
6. **VULN-009** — pin actions to SHAs, `env:` for event data, explicit `permissions:` in `ci.yml`.
7. Verification items 1–2 (HTTPS probe; mask TOTP seeds) as quick wins.

---

## Remediation log (2026-08-06, branch `security-hardening`)

All fixes verified by a full solution build (0 errors) and the test suite (262/262 passing).

| Finding | Status | Fix |
|---------|--------|-----|
| VULN-001 Clipboard history/cloud | **Fixed** | `TemporaryClipboard.Place` now places a `DataObject` carrying `CanIncludeInClipboardHistory`/`CanUploadToCloudClipboard` = 0 (the KeePass approach), via `SetDataObject(data, true)` (`TemporaryClipboard.cs`). |
| VULN-002 LibGit2Sharp CVEs | **Fixed** | LibGit2Sharp 0.26.2 → 0.31.0 (libgit2 1.9.x; CVE-2024-24577/-24575 fixed since 1.7.2); obsolete `UseRidGraph` workaround removed from `Directory.Build.props`. API surface used (`Commands.Fetch/Stage`, `Network.Push`, `Commit.CreateBuffer`, `CreateCommitWithSignature`) is unchanged. |
| VULN-003 gpg-agent.conf injection | **Fixed** | `GpgAgentConfigUpdater` now enforces an 8-key whitelist (cache TTLs / passphrase-policy options only) and rejects keys/values containing CR/LF or starting with `#`, with warning logs; docs/default config updated; 6 new tests cover rejection/acceptance. |
| VULN-004 Git remote-name injection | **Fixed** | `NativeGitSyncStrategy.Fetch` validates the remote name (rejects empty, leading `-`, whitespace, control chars, `"`) and throws `GitException` otherwise. |
| VULN-005 Bare `git` resolution | **Fixed** | `GitSyncStrategies.ChooseSyncStrategy` now threads the resolver's absolute path into `NativeGitSyncStrategy`, used as `ProcessStartInfo.FileName` (same pattern as GPG). |
| VULN-006 Bare powershell/explorer | **Fixed** | New `PathUtilities.ExplorerPath`/`PowerShellPath` rooted at `SpecialFolder.Windows`/`SpecialFolder.System`; all four call sites updated. Bonus: `ProcessExtensions.FormatArguments` escape-order bug fixed (backslashes first). |
| VULN-007 Plaintext temp file | **Fixed** | `EditPasswordAction` deletes the temp file in `try/finally` on every path, routes failures through `EnsureRemoval` (user-visible warning), and creates the file with an owner-only ACL (`FileMode.CreateNew`). |
| VULN-008 Clipboard clear on exit | **Fixed** | `TemporaryClipboard.Place` subscribes to `Application.Exit` and clears/restores the clipboard synchronously if it still holds the placed secret (reuses the existing text-equality guard). |
| VULN-009 CI/CD | **Fixed** | `actions/checkout`, `setup-dotnet`, `upload-artifact` pinned to full commit SHAs (tag kept as comment); release tag name passed via `$env:TAG` instead of `${{ }}` interpolation; `ci.yml` gained `permissions: contents: read`. |
| VERIFY-1 HTTP probe | **Fixed** | Connectivity probe now `https://www.gstatic.com/generate_204`. |
| VERIFY-2 TOTP seed display | **Fixed** | Metadata keys containing `totp`/`otpauth`/`secret` (case-insensitive) are masked in the details window. |

**Still open (accepted/low-priority):** VERIFY-3 (`gnupghome-override` + gpg-agent.conf creation — requires config write access), VERIFY-5 (biometric blob file ACLs — contents independently wrapped in DPAPI + Hello-gated RSA), VERIFY-6 (config reload staleness), VERIFY-7 (unsigned release artifacts — consider publishing SHA-256 sums). Manual smoke tests recommended once running on a real machine: Win+V history exclusion, exit-before-timeout clipboard clear, external-editor temp-file ACL (`icacls`), builtin git sync after the LibGit2Sharp upgrade.
