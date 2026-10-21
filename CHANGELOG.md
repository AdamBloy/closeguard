# Changelog

All notable changes to Close Guard are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Nothing yet.

## [1.0.1] - 2026-10-21

### Fixed

- Incognito windows are now detected reliably after a page has loaded. Previously, detection only worked while the window showed the "New Incognito tab" start page.

## [1.0.0] - 2026-10-21

First public release.

### Added

- Confirmation dialog when the close (X) button of a Chrome window is clicked with the mouse.
- Low-level mouse hook that only watches mouse clicks. There is no keyboard hook, so Alt+F4 and other shortcuts are never intercepted.
- Incognito windows are skipped and close immediately, detected from the window title.
- System tray icon with a **Run at startup** toggle and **Exit**.
- Branded, borderless confirmation dialog with a custom logo, rounded buttons and drag-to-move. **Keep open** is the default for Enter and Esc.
- Windows installer built with Inno Setup, with a custom wizard, an optional "start when I sign in" checkbox, and automatic installation of the .NET 8 Desktop Runtime if it is missing.
- Single-instance protection, so a second copy exits quietly.

### Security and privacy

- No network access, no data collection, and no keystroke monitoring.
- Installs to Program Files so the executable can't be modified without administrator rights.

### Known limitations

- Only Chrome is guarded by default.
- Incognito detection relies on window titles and may vary by Chrome version and language.
- The installer is not code-signed, so Windows SmartScreen may show a warning on first run.

[Unreleased]: https://github.com/AdamBloy/closeguard/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/AdamBloy/closeguard/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/AdamBloy/closeguard/releases/tag/v1.0.0
