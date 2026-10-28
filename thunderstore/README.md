# MIMESIS - MarkerMod

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/markermod](https://support.doodesch.de/markermod).


> Turn paintballs into permanent dungeon markers. Puddles and the footprints you leave normally vanish almost instantly - MarkerMod makes them persist so you can mark rooms, paths and dead ends, with optional color cycling and infinite paintballs.

![Version](https://img.shields.io/badge/version-1.5.9-blue)
![Game](https://img.shields.io/badge/game-MIMESIS-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![Status](https://img.shields.io/badge/status-working-brightgreen)

![MarkerMod in MIMESIS](https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-MarkerMod/main/docs/img/header.jpg)

## Features

- **Keep footprints** - the paint footprints you leave after walking through a puddle persist instead of fading away (on by default).
- **Keep puddles** - the spawned paint puddles themselves also stay instead of drying up (off by default).
- **Configurable lifetime** - a single setting controls how long persistent paint stays visible, in seconds (default 86400 = 24 hours, minimum 1 second).
- **Paintball color cycling** - while holding a paintball, right-click to cycle its color through Red, Yellow, Green, Blue, Orange, Purple, White, Black and back to default. The color applies locally to the held item and paint effects (on by default).
- **Infinite paintballs** - paintballs are not consumed when thrown, so you never run out of markers (off by default). The inventory shows an infinity symbol while the host enables this option.

## Screenshots

![A native paint trail left by walking through paint while holding a rainbow paintball](https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-MarkerMod/main/docs/img/paint-trail.jpg)

A real paint trail created by movement through paint.

![Native paintball inventory with an infinity symbol and a paint trail on the ground](https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-MarkerMod/main/docs/img/infinite-paintballs.jpg)

Enable infinite paintballs on the host to replace the charge count with the native infinity symbol.

## Requirements

| Component | Version |
| --- | --- |
| MIMESIS | 0.4.0 |
| MelonLoader | 0.7.3+ |
| MimicAPI | Required - recommended maintenance fork: MimicAPI 0.4.3, [Nexus download](https://www.nexusmods.com/mimesis/mods/11), install separately |

For Nexus, install [MimicAPI 0.4.3](https://www.nexusmods.com/mimesis/mods/11) separately. Keep one `Mods/MimicAPI.dll`; the mod archive does not include it. Thunderstore resolves its existing MimicAPI dependency through the mod manager.

## Installation

- **Recommended:** install through a Thunderstore mod manager (r2modman / Gale). MimicAPI is pulled in automatically as a dependency.
- **Manual:** download `MarkerMod.dll`, make sure `MimicAPI.dll` is also installed, and drop both into `MIMESIS/Mods/`. Launch the game once to generate the config file at `UserData/MelonPreferences.cfg`.

## Configuration

Stored in `UserData/MelonPreferences.cfg` under the `MarkerMod` category.

Each Steam lobby uses a snapshot of the host's `infinitePaintballs`, `preventPaintballSelfDamage` and `allowLongPaintballThrows` settings. If the host does not have MarkerMod, these gameplay options use vanilla behavior. Footprints, puddle display, display lifetime and color cycling stay local. With `keepPuddles` enabled on the host, puddles also keep their contact effect for the configured lifetime, so players can still pick up paint from them later. Client configuration is not overwritten. Change the host's gameplay configuration before creating the next lobby.

| Option | Description | Default | Values/Range |
| --- | --- | --- | --- |
| `keepFootprints` | Keep paint footprints after puddles dry. When enabled, footprints persist even after the puddles have dried up. | `true` | `true` / `false` |
| `keepPuddles` | Keep paint puddles for the configured lifetime. On the host, their contact effect stays active too. Footprints are controlled separately by `keepFootprints`. | `false` | `true` / `false` |
| `permanentLifetimeSeconds` | Lifetime in seconds for persistent paint - how long marks remain visible. Values below 1 are clamped up to 1 second at runtime. | `86400` | Any positive number of seconds (min 1; default 86400 = 24h) |
| `infinitePaintballs` | Paintballs are not consumed when thrown, so you never run out. | `false` | `true` / `false` |
| `enablePaintballColorChange` | Allow changing paintball color by right-clicking. Cycles through Red, Yellow, Green, Blue, Orange, Purple, White, Black. | `true` | `true` / `false` |
| `preventPaintballSelfDamage` | Prevent projectile self-hits and self-hits from paint fields. Host rule; the projectile protection also covers other projectiles. | `true` | `true` / `false` |
| `allowLongPaintballThrows` | Experimental host rule: increase the navmesh search radius for all physics projectiles to reduce early landings. | `false` | `true` / `false` |

## Usage

1. Hold a paintball and throw it to create a puddle.
2. Walk through wet paint to leave footprints.
3. With `keepFootprints` / `keepPuddles` enabled, the marks persist for the configured lifetime so you can navigate complex dungeons.
4. Right-click while holding a paintball to cycle its local color (Red, Yellow, Green, Blue, Orange, Purple, White, Black, then back to default).
5. With the host's `infinitePaintballs` enabled, paintballs are not consumed when thrown.

## Compatibility

Built for Mimesis 0.4.0 / MelonLoader 0.7.3. Requires MimicAPI.

## Credits / License

Author: **DooDesch**. Released under the **MIT License** (Copyright (c) 2025 DooDesch). Source: [github.com/DooDesch-Mods/Mimesis-MarkerMod](https://github.com/DooDesch-Mods/Mimesis-MarkerMod).

## Images

Screenshots and the package icon are captured in MIMESIS.
