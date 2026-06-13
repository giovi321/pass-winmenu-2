# Security policy

Pass Winmenu 2 handles GPG passphrases, and if you turn on Windows Hello it also stores a TPM-wrapped
copy of that passphrase on disk. If you find a vulnerability, please report it privately instead of
opening a public issue.

## Reporting

Use GitHub's private reporting: open the [Security tab](https://github.com/giovi321/pass-winmenu-2/security)
and click "Report a vulnerability." That creates a private advisory only the maintainer can see.

Include what you found, how to reproduce it, and what an attacker could actually do with it. I'll
acknowledge that I've received the report and keep you updated while it's looked into.

## Where to look

This is a personal project maintained in spare time, so there's no formal response-time guarantee, but
security reports come before everything else. The parts most worth scrutiny are the Windows Hello unlock
(`pass-winmenu/src/Biometrics`), how GPG gets invoked (`pass-winmenu/src/ExternalPrograms/Gpg`), and
anything that touches the passphrase in memory or on disk.

## Supported versions

Fixes go onto the latest release. Older versions don't get backports.
