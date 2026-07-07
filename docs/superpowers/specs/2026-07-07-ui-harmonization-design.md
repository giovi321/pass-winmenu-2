# UI harmonization and new keyhole icon

Date: 2026-07-07
Status: approved by giovi321

## Context

The password selector (SelectionWindow) and PasswordDetailsWindow follow a dark, config-driven theme from `StyleConfig` (#202020 background, Windows accent, Consolas). Every other window (About, PasswordWindow, PassphraseWindow, EditWindow, LogViewer, PasswordGeneratorControl) uses default light WPF chrome, and the tray menu is a system-rendered WinForms `ContextMenuStrip` with a Beige update item. The app icon is an Inkscape line-drawn key that looks dated and muddy at 16 px.

Goal: one coherent visual language across all UI surfaces, driven by the existing `StyleConfig`, plus a modern minimal icon. Decisions made with the user:

- Config-driven dark theme extended to every window (not a fixed theme, not a system-theme library)
- Tray menu styled with a custom dark renderer (no new dependencies)
- Icon: monochrome white keyhole glyph (option B of the presented concepts)

## 1. Icon

New glyph: a solid disc with a punched-out keyhole, drawn in white with a faint dark outline (rgba(0,0,0,0.55), 0.8 units at 32 viewBox) so it stays visible on light taskbars.

Reference geometry (32x32 viewBox), from the approved preview:

```svg
<path fill="#fff" fill-rule="evenodd"
      d="M16 3 a13 13 0 1 0 0 26 a13 13 0 1 0 0 -26 z
         M16 7.3 A4.2 4.2 0 0 0 13.4 14.8 L11.2 23.5 L20.8 23.5 L18.6 14.8 A4.2 4.2 0 0 0 16 7.3 Z"/>
```

Git-sync badges replace the old overlay orbs: a filled circle (r 6.4 at cx/cy 24.6) in the bottom-right corner with a white symbol, separated from the glyph by a transparent ring (the glyph is punched out under the badge in the final render; the preview used a dark stroke)

- Ahead: green #3fb950, up arrow
- Behind: blue #388bfd, down arrow
- Diverged: orange #db6d28, diamond

Deliverables:

- `resources/icon.svg` redrawn, keeping the base + three overlay layer structure
- A pixel-hinted 16 px variant (thicker keyhole, disc edges aligned to the pixel grid) used for the 16 and 20 px ico entries; 24 px and larger render from the base SVG
- `resources/generate_icons.py`: renders SVGs with resvg-py, packs multi-size `.ico` (16, 20, 24, 32, 48, 256) with Pillow, writes the four files to `pass-winmenu/embedded/` (`pass-winmenu-plain.ico`, `-ahead.ico`, `-behind.ico`, `-diverged.ico`)
- No code changes: embedded resource names and `<ApplicationIcon>` already point at these paths

## 2. Shared config-driven theme

New `Theme` class (`pass-winmenu/src/Windows/Theme.cs`):

- Input: the loaded `StyleConfig` (`pass-winmenu/src/Configuration/Classes/StyleConfig.cs`)
- Derives a palette: window background (`BackgroundColour`), text (`Options.TextColour`), hint (`SearchHint.TextColour`), accent (`BorderColour`, resolves `[accent]` via `Helpers.BrushFromColourString`), plus computed shades for control background, control border, hover, and pressed states (lighten when the background is dark, darken when light, decided by luminance)
- Builds a `ResourceDictionary` with named brushes and implicit styles for `Button`, `TextBox`, `PasswordBox`, `CheckBox`, `Label`, `TextBlock` (foreground via window inheritance), `GroupBox`, `Slider`, `ToggleButton`, `Hyperlink`, and `ScrollBar`
- Merged into `Application.Resources` during startup (`Setup.cs`), after config load, so all windows inherit implicit styles automatically

Native title bars: a `DarkTitleBar` helper P/Invoking `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)`, applied on `SourceInitialized` for every dialog window, only when the theme background is dark.

`Theme.Apply` is idempotent (the merged `Theme.xaml` dictionary is only added once; brushes are still re-set every call) and is re-invoked from `ConfigManager.Reload` on the UI thread after a successful config-file reload, so live WPF windows re-theme via `DynamicResource` immediately; the WinForms tray menu builds its colours once at startup and stays on the old theme until restart.

Window changes:

- AboutWindow, PasswordWindow, PassphraseWindow, EditWindow, LogViewer, PasswordGeneratorControl: remove hardcoded light-theme colors (for example `#FF666666` in AboutWindow, `#569de5` divider in EditWindow), rely on the shared theme; AboutWindow additionally shows the new icon and accent-styled hyperlinks
- PasswordDetailsWindow: keep behavior, consolidate its `ApplyStyle` brushes onto the shared theme resources where they overlap
- SelectionWindow and SelectionLabel: unchanged (already config-styled)

## 3. Tray menu

`DarkMenuRenderer : ToolStripProfessionalRenderer` with a `ProfessionalColorTable` subclass fed from the same theme palette (`pass-winmenu/src/Notifications/`):

- Menu background, item hover (accent), text, and separator colors derived from `StyleConfig`
- Applied to the `ContextMenuStrip` and the More Actions dropdown in `Notifications.cs`
- The `Download Update` item loses its `Beige` background and becomes accent-colored text
- The version `ToolStripLabel` styled as a muted header

Applied only when the theme background is dark; with a light user config the menu keeps default rendering.

## Out of scope

- Selection window layout or behavior changes
- New config options (the theme derives everything from existing `StyleConfig` values)
- Balloon notifications (unchanged)

## Verification

- Build and test with the local SDK at `~/.dotnet-local` (no system dotnet)
- Run the app; screenshot and inspect: About, Windows Hello setup, Choose a Password, Edit, Log viewer, password details, selector, tray menu (including the More Actions dropdown and an item hover state)
- Tray icon checked at 100% and 150% DPI, on dark and light taskbar; sync badge variants verified by triggering `SetSyncState` or temporarily swapping the plain icon
- Icon regeneration reproducible: `python resources/generate_icons.py` rewrites identical ico files
