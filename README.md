# P.K. - Tenzan Pass Patch

**By [faospark](https://github.com/faospark)**

A **BepInEx 6 (IL2CPP)** mod for **Suikoden I & II HD Remaster** that restores the missing **Chimera** monster encounters and rare **Cyclone Rune** drops in Tenzan Pass (`va11`).

In the original PS1 release (and ported into the HD Remaster), Chimera's stats, animations, and 3 party formations existed in the game data, but an encounter-table oversight left them unused. Based on Pyriel's classic `tenzan.asm` fix, this mod restores all 3 Chimera formations (~30% encounter rate) and safely patches the battle data in memory.

## Features

- **Chimera Restoration (`EnableTenzanChimeraFix`)**: Restores the 3 omitted Chimera party formations into Tenzan Pass encounter slots (~30% encounter rate).
- **Configurable Cyclone Rune Drop (`TenzanChimeraCycloneDropRate`)**: Sets Chimera's Cyclone Rune drop rate from `1` to `100%` (default: `100%`).
- **Force Chimera Encounter (`ForceTenzanChimeraEncounter`)**: Optional testing toggle to make all Tenzan Pass encounters Chimera formations.
- **Crash-Safe In-Memory Patching**: Patches encounter and battlefield grid data in memory without modifying game archive files.

> For bytecode analysis, encounter rates, and party formation tables, see [Documentation/tenzan_pass_chimera_va11.md](Documentation/tenzan_pass_chimera_va11.md).

## Configuration (`BepInEx/config/faospark.pktenzanpasspatch.cfg`)

```ini
[Suikoden 2 Tenzan Pass Chimera]
EnableTenzanChimeraFix = true        # Restore Chimera encounters in Tenzan Pass (~30% rate)
ForceTenzanChimeraEncounter = false  # Debug/testing: force all Tenzan Pass encounters to Chimera
TenzanChimeraCycloneDropRate = 100   # Cyclone Rune drop rate percentage (1-100)
```

## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster**
- **Recommended:** [Suikoden Fix](https://github.com/d3xMachina/SuikodenFix) by d3xMachina

## Installation

1. Install [BepInEx 6.0 (Unity IL2CPP x64)](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip) into your game root directory.
2. Place `PKTenzanPassPatch.dll` into `BepInEx/plugins/`.
3. Launch the game once to generate `BepInEx/config/faospark.pktenzanpasspatch.cfg`.

## Credits & License

- **[faospark](https://github.com/faospark)** — Primary Author & Lead Researcher
- **Pyriel** — Original creator of the PS1 Suikoden II Tenzan Pass bugfix (`tenzan.asm`)
- **[d3xMachina](https://github.com/d3xMachina/Suikoden-Fix)** — Creator of Suikoden-Fix & BepInEx IL2CPP foundation
- **[asilverthorn](https://github.com/asilverthorn/suikoden_ref)** — Reference tables and internal IDs
- **Mod Suite:** [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)
- **Framework:** [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX)

Distributed under the [MIT License](LICENSE).
