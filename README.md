# P.K. - Tenzan Pass Patch

A **BepInEx 6 (IL2CPP)** mod for **Suikoden I & II HD Remaster** (Steam) that restores the missing Chimera monster encounters and rare Cyclone Rune drops in Tenzan Pass (`va11`).

---

## Overview & History

In the original Suikoden II PS1 release (`VA11.BIN`), Chimera was fully defined in the monster data with complete stats, animations, drops (including the rare Cyclone Rune), and three monster party formations (Solo Chimera, Chimera + Magus x2, Chimera + Minotaurus). However, due to a developer oversight, the 10-slot encounter table only referenced parties 0 to 5, leaving Chimera completely unobtainable during normal gameplay.

The classic community bugfix by Pyriel (`tenzan.asm`) restored Chimera by substituting duplicate encounter slots 1, 3, and 6 with the 3 omitted Chimera formations, giving Chimera an authentic ~30% encounter rate in Tenzan Pass.

The HD Remaster ported the original encounter table without fixing this bug. **PKTenzanPassPatch** restores Chimera encounters in Tenzan Pass using an in-memory map asset patching approach (`MACHICON.EventInit`) and engine safeguards (`BattleDataSetup`, `InitNewMonster`, `get_monster_okane`), making it fully immune to official game updates without modifying game archive files.

---

## Features

- **Chimera Restoration**: Restores Pyriel's 3 Chimera party formations into encounter slots 1, 3, and 6 (~30% encounter rate).
- **Guaranteed/Configurable Cyclone Rune Drop**: Chimera drops the rare Cyclone Rune upon defeat (configurable drop rate 1-100%, defaults to 100%).
- **Battle Grid & Array Protection**: Ensures complete 18-element battlefield position coordinate arrays and reinforces `monster_data` to prevent out-of-bounds exceptions.
- **In-Memory Byte-Patching**: Direct memory modification on map transition (`va11`), completely eliminating the need for manual asset extraction or repacking.
- **Force Chimera Encounter (Debug/Testing)**: Option to force all encounters in Tenzan Pass to Chimera formations for testing.
- **Deep Technical Specification**: For complete bytecode analysis, encounter rates, and party formation tables, see [Documentation/tenzan_pass_chimera_va11.md](Documentation/tenzan_pass_chimera_va11.md).

---

## Configuration Options

Edit `BepInEx/config/faospark.pktenzanpasspatch.cfg` with Notepad or any text editor:

```ini
[Suikoden 2 Tenzan Pass Chimera]

## Restores the missing Chimera monster encounters in Tenzan Pass (30% encounter rate, dropping the rare Cyclone Rune) as originally intended in the PS1 era.
# Setting type: Boolean
# Default value: true
EnableTenzanChimeraFix = true

## Debug/Testing option: Forces all random encounters in Tenzan Pass to be Chimera formations.
# Setting type: Boolean
# Default value: false
ForceTenzanChimeraEncounter = false

## Drop rate percentage (1-100) for the Cyclone Rune when defeating a Chimera in Tenzan Pass. Default is 100% for guaranteed drop.
# Setting type: Int32
# Default value: 100
# Acceptable value range: From 1 to 100
TenzanChimeraCycloneDropRate = 100
```

---

## Installation

- **Prerequisite**: [BepInEx 6.0 (Unity IL2CPP x64)](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip) installed in your game root directory.
- **Install**: Extract the release ZIP directly into your game's root directory (or place `PKTenzanPassPatch.dll` into `BepInEx/plugins/`).
- **Verify**: Launch the game; `BepInEx/LogOutput.log` will confirm `[PKTenzanPassPatch] loaded successfully!`.

---

## Credits & Acknowledgments

- **[d3xMachina](https://github.com/d3xMachina/Suikoden-Fix)**: Creator of [Suikoden-Fix](https://github.com/d3xMachina/Suikoden-Fix). Bootstrapped the foundational BepInEx IL2CPP implementation and provided early inspiration for Remaster modding.
- **[asilverthorn](https://github.com/asilverthorn/suikoden_ref)**: Creator of [suikoden_ref](https://github.com/asilverthorn/suikoden_ref). Invaluable early reference tables, internal IDs, and save structure research.
- **[faospark](https://github.com/faospark)**: Primary Author & Lead Researcher.
- **Pyriel**: Original creator of the definitive PS1 Suikoden II Tenzan Pass bugfix (`tenzan.asm`). His landmark reverse-engineering work originally uncovered the omitted Chimera party formations and established the elegant substitution of duplicate slots 1, 3, and 6 to restore Chimera's authentic ~30% spawn rate and Cyclone Rune drop.
- **Moogles & Mods Discord Community**: For ongoing support, research, collaboration, and keeping the Suikoden modding scene thriving. Join us on the [Moogles & Mods Discord](https://discord.com/invite/bSnpVBV).
- **Framework**: [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX).
