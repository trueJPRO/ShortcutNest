# ShortcutNest

ShortcutNest is a lightweight Windows popup launcher designed for **fast access to your own shortcuts** (apps, folders, URLs, and commands) using a single key.

It opens a clean **3x3 launcher popup** with up to **9 customizable slots**. It's built to work especially well with remapped special keys (such as the **Copilot key**) via tools like **PowerToys**.

---

## Why I built this

I built ShortcutNest because I wanted a **simple, fast, personal popup launcher** triggered by a single key (in my case, the **Copilot key**), but I never found exactly what I wanted:

- not a full command launcher
- not a heavy app switcher
- not a complicated macro tool
- just a clean popup with **my shortcuts**, always in the same place

So I made it.

The original goal was to repurpose the Copilot key into something actually useful for my workflow.

---

## Features

- 3x3 popup launcher (9 slots)
- Mouse support
- Keyboard support:
  - `1..9` / Numpad `1..9`
  - Arrow keys
  - `Tab` / `Shift+Tab`
  - `Enter` / `Space` to run selected slot
  - `Esc` to close
- Supports shortcut types:
  - `app`
  - `folder`
  - `url`
  - `command`
- Optional per-slot icons (`IconPath`)
- Clean dark UI (blue/gray theme)
- Configurable through `launcher-config.json`
- Optional **resident mode** for faster open/close (recommended)

---

## Requirements

- Windows 11 (recommended)
- .NET 8 SDK (only if building from source)
- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) (mandatory if you want to remap the Copilot key, or you can set a shortcut directly in the `.exe` properties, though only for other keys that diverge from special/combo keys)

---

## How it works (Copilot key use case)

On many systems, the Copilot key is reserved by Windows/OEM and cannot be captured directly by normal app hotkeys.

The practical workaround is:

1. Use **PowerToys Keyboard Manager**
2. Remap the Copilot key to **launch ShortcutNest**
3. ShortcutNest opens the popup
4. Choose a slot via mouse or keyboard

This avoids low-level key conflicts and keeps the launcher stable.

---

## Installation (prebuilt release)

1. Download the latest release (`ShortcutNest.exe`)
2. Place it in a folder (for example: `C:\Tools\ShortcutNest\`)
3. Run it once — it will create `launcher-config.json` if missing
4. (Optional) Create an `icons` folder in the same directory for custom slot icons

Recommended runtime folder structure:

```text
ShortcutNest/
├─ ShortcutNest.exe
├─ launcher-config.json
└─ icons/
   ├─ terminal.png
   ├─ explorer.png
   └─ ...
```

---

## Configure the Copilot key with PowerToys

> The exact UI may change depending on the PowerToys version.

1. Open **PowerToys**
2. Go to **Keyboard Manager**
3. Use the remap option that lets you assign a key to launch an app
4. Point it to `ShortcutNest.exe`

If your system still reserves the Copilot key in Windows settings, PowerToys is usually the simplest working path.

---

## Customizing the popup

ShortcutNest is intentionally designed to be customized via a config file, not via an in-app settings panel.

When the app runs for the first time, it creates `launcher-config.json`.

### Slot schema

Each slot supports:

| Field | Description |
|---|---|
| `Title` | Display name shown in the popup |
| `Type` | `app`, `folder`, `url`, or `command` |
| `Target` | What to run/open |
| `IconPath` | Optional — path to a custom icon |

### Example `launcher-config.json`

```json
{
  "Slots": [
    { "Title": "Terminal", "Type": "app", "Target": "wt.exe", "IconPath": "icons\\terminal.png" },
    { "Title": "Explorer", "Type": "app", "Target": "explorer.exe", "IconPath": "icons\\explorer.png" },
    { "Title": "Browser", "Type": "url", "Target": "https://google.com", "IconPath": "icons\\browser.png" },
    { "Title": "Notes", "Type": "app", "Target": "notepad.exe", "IconPath": "icons\\notes.png" },
    null,
    null,
    null,
    null,
    null
  ]
}
```

### Notes

- `IconPath` is optional. If an icon is missing, ShortcutNest shows a fallback letter icon.
- `IconPath` can be relative to the app folder (`icons\\myicon.png`) or an absolute path (`C:\\...\\myicon.png`).
- `app` targets can be executable names in `PATH` (e.g. `wt.exe`, `code`) or full paths.
- `folder` targets can be relative or absolute paths.
- `command` runs through `cmd.exe /c`.
- The config is normalized to 9 slots internally (extra slots are ignored, missing slots are filled with `null`).

---

## Keyboard controls

| Key | Action |
|---|---|
| `1..9` / Numpad `1..9` | Run slot directly |
| Arrow keys | Move selection |
| `Tab` | Next slot |
| `Shift+Tab` | Previous slot |
| `Enter` / `Space` | Run selected slot |
| `Esc` | Close popup (or hide it in resident mode) |

---

## Resident mode (experimental attempt, not included in current release)

I explored a **resident/background mode** to make ShortcutNest feel almost instant by keeping one instance running and toggling the popup from subsequent launches.

### Why resident mode could improve performance

In simple mode, each key press launches a new process (`ShortcutNest.exe`), which adds startup overhead (process startup + .NET runtime + UI initialization).

A resident mode could improve responsiveness because:

- one instance stays alive in the background
- repeated key presses would signal the running instance (for example, `TOGGLE`)
- the popup could open/close much faster than a full process launch each time

### Why it is not included (for now)

I implemented and tested a resident-mode approach, but I ran into reliability/UX issues on my setup, including:

- inconsistent behavior when triggered from the remapped key
- unexpected internal Windows/.NET windows appearing (e.g. broadcast/IME helper windows)
- cases where the popup only opened correctly through the tray instead of the key trigger

Since the goal of ShortcutNest is to feel simple and stable (almost like a native Windows feature), I decided to keep the current release on **simple mode** until a cleaner resident implementation is proven reliable.

### If you want to try building resident mode

If you fork this project and want maximum responsiveness, resident mode is still a valid direction to explore.

What a good resident mode should do:

- keep one background instance alive
- receive toggle commands from new launches
- show/hide the popup without creating visual artifacts
- avoid extra visible windows
- remain stable with PowerToys key remapping

If you build a reliable version, feel free to open an issue or PR.

---

## Build from source

**Build:**

```bash
dotnet build -c Release
```

**Publish (single-file, self-contained):**

```bash
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Published output will be at:

```
bin\Release\net8.0-windows\win-x64\publish\
```

---

## Performance Notes and Limitations

- The current release uses **simple mode** (a new process starts on each key press).
- This keeps the app behavior predictable and easy to deploy.
- Depending on your machine, you may notice a small delay when opening the popup (~1 second).
- A future **resident mode** may improve responsiveness significantly, but it is not included in the current release due to reliability/UX issues in testing.
- On some PCs, the Copilot key is intercepted by Windows/OEM drivers and cannot be captured directly by normal app hotkeys. PowerToys remapping is the intended workaround.
- This project is **Windows-only** (if this ends up being useful to a lot of people, I may do a **MAC version**).

---

## Contributing

Issues and suggestions are welcome.

If you use or modify this project, please keep attribution to the original author.

---

## Author

Created by **João Oliveira**.

---

## License

MIT License — Copyright (c) 2025 João Oliveira

See the [LICENSE](LICENSE) file for full text.