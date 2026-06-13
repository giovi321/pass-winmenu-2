# Pass Winmenu 2

[![CI](https://github.com/giovi321/pass-winmenu-2/actions/workflows/ci.yml/badge.svg)](https://github.com/giovi321/pass-winmenu-2/actions/workflows/ci.yml)
[![Release](https://github.com/giovi321/pass-winmenu-2/actions/workflows/release.yml/badge.svg)](https://github.com/giovi321/pass-winmenu-2/actions/workflows/release.yml)

A keyboard-driven password manager for Windows that can unlock with your fingerprint.

Based on [pass-winmenu](https://github.com/geluk/pass-winmenu) by Johan Geluk. It keeps the original's
idea and its `pass`-compatible storage format, and adds Windows Hello unlocking and a field viewer.
Maintained by [giovi321](https://github.com/giovi321/pass-winmenu-2).

Your passwords are GPG-encrypted files in a folder, the same way the Linux
[pass](https://www.passwordstore.org) tool stores them. Pass Winmenu 2 is a small tray app that lets you
find and decrypt them from the keyboard.

## Install

1. Download the latest build from the [releases page](https://github.com/giovi321/pass-winmenu-2/releases)
   and unzip it anywhere.
2. Run `pass-winmenu.exe`. It sits in the tray; press `Ctrl Alt P` to open the menu.

The download isn't code-signed, so the first time you run it Windows SmartScreen may show "Windows
protected your PC." Click More info, then Run anyway. That's expected for an unsigned open-source app;
the source and the workflow that built the exe are both in this repo.

You'll need a few things on the machine:

- Windows 10 or 11.
- The [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (the build is framework-dependent).
- GPG, from [Gpg4win](https://www.gpg4win.org/) or GnuPG for Windows.

The app reads a `pass-winmenu.yaml` next to the exe and creates one on first run. If you've never used
`pass` before, you also need a GPG key and a password store, covered in [First-time setup](#first-time-setup).

## Build from source

You need the .NET 8 SDK. The project targets `net8.0-windows10.0.19041.0` because the Windows Hello APIs
live in the Windows 10 SDK.

```
dotnet build pass-winmenu.sln -c Release
```

Or open `pass-winmenu.sln` in Visual Studio. To produce the single-file exe that releases ship:

```
dotnet publish pass-winmenu/pass-winmenu.csproj -c Release
```

## What's new in 2.1

- Unlock with Windows Hello (fingerprint, PIN, or face) instead of typing your GPG passphrase. The
  passphrase is wrapped by a TPM-backed Hello key and handed to GPG only after you authenticate. See
  [Windows Hello unlock](#windows-hello-unlock).
- A "show password fields" view. Pick a file and it lists every field with its value next to it. The
  password stays hidden behind dots until you click the eye, and clicking any value copies it.
- XKCD-style passphrases ("correct horse battery staple") as an alternative to random characters,
  configurable in your yaml. See [Password generation](#password-generation).
- An in-app About window, and update checks that point at this repository.

## Usage

Press `Ctrl Alt P` to open the menu. Type to search, move with `Tab`, and hit `Enter` to decrypt the
selected entry. GPG asks for your passphrase through pinentry, or through Windows Hello if you've turned
that on. The decrypted password then goes to your clipboard, gets typed into the active window, or both,
depending on your hotkey settings.

To look through a file field by field, use the **Show Password Fields** action (`Ctrl Alt O` by default,
or the tray menu). Each field shows its value, the password is masked until you reveal it, and a click
copies the value you want.

## Windows Hello unlock

Turn it on in `pass-winmenu.yaml` under `gpg.biometrics`:

```yaml
gpg:
    biometrics:
        enabled: true
        # once-per-session | cache | every-password
        mode: once-per-session
        cache-seconds: 600
        credential-name: pass-winmenu-gpg
```

Then enrol once: right-click the tray icon, open **More Actions**, and pick **Set up Windows Hello unlock**
(or run `pw enroll` from a terminal). Enter your GPG passphrase once and it's stored, unlocked from then on
with a Hello gesture.

`mode` controls how often you're asked:

| Mode              | Behaviour                                                                                          |
|-------------------|----------------------------------------------------------------------------------------------------|
| `once-per-session`| One prompt at startup. gpg-agent caches the passphrase for the session, so the app doesn't hold it. |
| `cache`           | A prompt, then a quiet window of `cache-seconds` before the next one.                              |
| `every-password`  | A fresh Hello gesture on every decryption.                                                         |

How it actually protects the passphrase: it's encrypted with AES-256-GCM under a key derived from a
signature made by a TPM-backed Windows Hello key (`KeyCredentialManager`), so the blob can only be
decrypted after a real Hello gesture. The weaker `UserConsentVerifier` API is deliberately not used. The
encrypted blob sits next to your config as `biometric.blob`.

Be honest with yourself about the trade-off. Your GPG passphrase now lives on the machine (TPM-wrapped)
rather than only in your head. That defends against a stolen disk and against a keylogger watching pinentry.
It does not defend against malware running as you once you've unlocked, or someone walking up to your
unlocked session. It's the same bargain your browser makes when it gates saved passwords behind Windows
Hello, and it's off by default. If GPG later rejects the stored passphrase (say you changed it) or your
Hello key gets reset, the app drops the enrolment, asks you to set it up again, and falls back to a normal
pinentry prompt in the meantime.

## Password generation

The "Add password" dialog generates passwords two ways, set in `pass-winmenu.yaml` under
`password-store.password-generation.style`:

- `random` (the default): random characters from the character groups you enable.
- `xkcd`: a passphrase of random dictionary words, like the [XKCD comic](https://xkcd.com/936/).

The XKCD options live under `password-generation.xkcd`:

```yaml
password-generation:
    style: xkcd
    xkcd:
        word-count: 4
        separator: '-'
        random-number-separator: false
        capitalisation: first   # none | first | upper | random
        min-word-length: 4
        max-word-length: 9
        include-number: false
        # word-list-file:       # optional custom list; defaults to the built-in EFF list
```

What each option does:

| Option | What it does | Example |
|---|---|---|
| `word-count` | How many words. More words means stronger but longer; each built-in word adds about 12.9 bits. | `4` gives four words |
| `separator` | Text between words. Use `''` for none. Ignored when `random-number-separator` is on. | `'-'` gives `Correct-Horse-Battery-Staple` |
| `random-number-separator` | Wraps every word with a random digit, one before each word and one at the end. Overrides `separator` and `include-number`. | `8word1balcony9colony0` |
| `capitalisation` | `none` (lower), `first` (Title Case), `upper` (ALL CAPS), or `random` (each word entirely upper or lower). | `random` gives `CORRECT horse BATTERY staple` |
| `min-word-length` / `max-word-length` | Only use words in this length range. The built-in list runs 3 to 9 letters. | `4` to `9` |
| `include-number` | Tacks a random two-digit number on the end, for sites that demand a digit. Ignored when `random-number-separator` is on. | `Correct-Horse-Battery-Staple-42` |
| `word-list-file` | Your own word list, one word per line. Blank uses the built-in list. | |

Turn on `random-number-separator` together with `capitalisation: random` and you get passwords like
`8WORD1balcony9COLONY0`.

The built-in list is the [EFF large diceware word list](https://www.eff.org/dice) (7776 words), used under
CC BY 3.0. Point `word-list-file` at your own list to override it.

## Configuration

There are two config files, and the difference trips people up:

- `pass-winmenu/embedded/default-config.yaml` in the source tree is the reference. It documents every
  option and gets compiled into the exe. Read it when you want to know what a setting does or copy a new one.
- `pass-winmenu.yaml` next to `pass-winmenu.exe` is your personal copy, and the one the app actually reads.
  It's created from the reference the first time you run the app, so it won't grow new options on its own;
  copy those across by hand. To start fresh, delete it and restart, then put your store path back.

## First-time setup

Skip this if you already use `pass`.

### A GPG key

If you don't have a key yet, start the app, right-click the tray icon, and choose **Open shell** for a
PowerShell window. Then:

```
gpg --gen-key
```

The passphrase you set here is what decrypts your passwords, so make it a good one.

### A password store

Pick a folder for your passwords (the default is `%USERPROFILE%\.password-store`):

```
mkdir $HOME\.password-store
```

Put the email address or key ID of your GPG key in a `.gpg-id` file at the root of the store:

```
echo "myemail@example.com" | Out-File -Encoding utf8 $HOME\.password-store\.gpg-id
```

If you chose a different folder, set `password-store.location` in `pass-winmenu.yaml` and restart.

### Across devices

The store is just files, so sync it however you like (Git, Syncthing, Dropbox, and so on). For Git:

```
cd $HOME\.password-store
git init; git add -A; git commit -m "Initialise password repository"
git remote add origin https://github.com/yourusername/password-store.git
git push --set-upstream origin master
```

### On a second machine

Export your secret key where it already works, copy it over, import it, and trust it:

```
gpg --export-secret-key -a youremailaddress@example.com > private.key   # source machine
gpg --import private.key                                                # target machine
gpg --edit-key youremailaddress@example.com                             # then: trust -> 5 -> save
```

Then clone your store and point `pass-winmenu.yaml` at it.

## Command line (`pw`)

A companion command-line tool, `pw`, comes with it:

- `pw list` lists passwords.
- `pw show password <path>` prints a password.
- `pw show all <path>` prints a whole file.
- `pw show key <key> <path>` prints one field.
- `pw enroll` sets up Windows Hello unlock.

## Repository layout

| Path | What it is |
|------|------------|
| `pass-winmenu/` | The tray application (WPF, .NET 8). |
| `pass-winmenu/src/` | The application code, grouped by area: `Actions`, `Biometrics`, `Configuration`, `ExternalPrograms/Gpg`, `Hotkeys`, `Jobs`, `Notifications`, `PasswordGeneration`, `PasswordManagement`, `UpdateChecking`, `Utilities`, `WinApi`, `Windows`. |
| `pass-winmenu/embedded/` | Resources baked into the exe: `default-config.yaml` (the reference config), `wordlist.txt` (the EFF list for XKCD passphrases), and the tray icons. |
| `commandline/` | The `pw` command-line tool. |
| `pass-winmenu-tests/` | Unit tests (xUnit). |
| `resources/` | The tray-icon SVGs and `generate-icons.ps1`, used only during development. |
| `pass-winmenu.sln` | The solution. |

## Credits and licence

Pass Winmenu 2 is a fork of [pass-winmenu](https://github.com/geluk/pass-winmenu) by Johan Geluk, who
deserves the credit for the original. This fork is maintained by [giovi321](https://github.com/giovi321)
and uses the same licence as the [original project](https://github.com/geluk/pass-winmenu).

Want it on another platform? See https://www.passwordstore.org/#other.
