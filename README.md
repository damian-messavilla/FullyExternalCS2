# FullyExternalCS2 v2.0 (Forked & Enhanced)

![menu](assets/photo.png)

> **Note:** This repository is a **fork** of the original FullyExternalCS2 by sweeperxz. We have heavily modified and improved this version with massive performance boosts, UI enhancements, offset bugfixes, and brand new tactical features.

FullyExternalCS2 v2.0 is a refreshed external CS2 project with a new ImGui overlay, built-in menu, live configuration, updated visuals, RCS improvements, Bomb Timer, Vote Teller, and cleaner feature control directly from the overlay.

The main focus of this version is the new in-game menu: most features no longer need to be edited manually in `config.json`; they can be enabled, disabled, tuned, saved, reloaded, and reset from the overlay.

## What's new in v2.0

- New ImGui-based overlay menu
- Tabbed interface for Aimbot, Visuals, Misc, and Config
- Built-in keybind editor
- Config save, reload, and reset buttons
- Live feature toggles from the menu
- Updated ESP settings
- Weapon text display for enemies
- ESP flags moved into the menu
- Improved RCS behavior
- Aim FOV circle and smoothing control
- Selectable aimbot target bone
- Bomb Timer update
- New Vote Teller feature
- Auto-loaded offsets from cs2-dumper

## 🚀 Fork Additions & Improvements

We have introduced several crucial updates to make this cheat faster, safer, and more feature-rich:

**Performance Optimizations:**
- **Zero-Cost Strings:** Re-wrote the entity memory reader to cache player names upon initialization instead of polling `ReadString()` every frame.
- **Render-Thread Offloading:** Moved heavy memory-reading logic (e.g., `ObserverTarget`) out of the ImGui draw loop and into the background `GameData` update thread, massively boosting overlay FPS.

**New Features:**
- **Global Visuals Toggle:** Added a master switch in the UI to toggle all visual/ESP features instantly.
- **Smart Bomb Timer:** The bomb timer now intelligently compares remaining explosion time with defuse time. It dynamically shifts colors (Green = Safe to Defuse, Red = Run away!).
- **Sniper No-Scope Crosshair:** Added a centered dot that *only* appears when wielding a sniper rifle (AWP, Scout, SCAR, G3SG1) and disappears instantly when scoping in.
- **Spectator Network:** Upgraded the spectator list. When you are dead and spectating someone, you can now see exactly who is spectating *them*.
- **Clean UI:** Simplified text layouts to keep the overlay stealthy and clutter-free (e.g. `Spectators (X)`).

**Critical Bug Fixes:**
- **Dynamic Offset Patches:** Repaired broken `m_bIsScoped` and `m_pClippingWeapon` offset chains caused by recent CS2 updates. Weapons and scope states are now resolved flawlessly via the `m_pWeaponServices -> m_hActiveWeapon` handle chain.
- Fixed ESP weapon names and Aimbot Grenade-Checks.

## Menu

The cheat now includes a full overlay menu.

Default menu key: `Insert`

### Aimbot tab

- Enable or disable Aimbot
- Show or hide FOV Circle
- Change Aim FOV
- Change Aim Smoothing
- Select Target Bone: Head, Neck, Chest, Pelvis
- Enable or disable RCS
- Change RCS Strength
- Change Aim Key

### Visuals tab

- ESP Box
- ESP Name
- ESP Weapon text
- ESP Flags
- Box color picker
- Skeleton ESP
- Aim Crosshair
- Bomb Timer
- Vote Teller

### Misc tab

- TriggerBot
- TriggerBot keybind
- Team Check
- Menu keybind

### Config tab

- Save Config
- Reload Config
- Reset Defaults
- Game status information
- Overlay FPS and resolution information

## Features

### Aimbot

- Key activation
- Visibility check
- Adjustable FOV
- Adjustable smoothing
- Target bone selection
- FOV circle
- Recoil Control System
- RCS strength slider

Default aim key: `LButton`

### ESP

- Box ESP
- Health bar
- Health numbers
- Player name
- Current weapon text
- Enemy flags:
  - Scoped
  - Flashed
  - Shifting
  - Shifting in scope
- Skeleton ESP
- Team Check
- Custom box color

### World visuals

- Bomb Timer
- Vote Teller
- Aim Crosshair

### TriggerBot

- Key activation
- Configurable keybind

Default trigger key: `LAlt`

### Config system

- `config.json` is created automatically
- Settings can be changed from the menu
- Config can be saved from the menu
- Config can be reloaded while the program is running
- Defaults can be restored from the menu
- Keybinds are stored in config

### System

- Auto update offsets
- External overlay
- Does not write to game memory
- Built on .NET 8
- Uses ClickableTransparentOverlay and ImGuiNET

## Default keys

| Action | Default key |
| --- | --- |
| Open menu | Insert |
| Aimbot | LButton |
| TriggerBot | LAlt |

## Requirements

- Windows
- .NET 8 SDK
- Counter-Strike 2

## Dependencies

```xml
<ItemGroup>
    <PackageReference Include="ClickableTransparentOverlay" Version="11.1.0"/>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3"/>
    <PackageReference Include="Process.NET" Version="1.0.2"/>
</ItemGroup>
```

## Installation

```bash
git clone https://github.com/sweeperxz/FullyExternalCS2.git
cd FullyExternalCS2
```

## Starting the program

```bash
dotnet build
dotnet run
```

## Notes

FullyExternalCS2 was created for learning and improving Windows API, overlay, and external memory-reading skills.

Use at your own risk.

## Authors

- sweeperxz - Developer/Engineer
