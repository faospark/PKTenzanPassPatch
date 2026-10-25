# PKTenzanPassPatch (Suikoden II Tenzan Pass Encounter & Chimera Restoration)

Standalone BepInEx mod for **Suikoden I & II HD Remaster** restoring the missing Chimera monster encounters and Cyclone Rune drop in Tenzan Pass.

## Overview & History

In the original Suikoden II PS1 release (`VA11.BIN`), Chimera was fully defined in the monster data with complete stats, animations, drops (including the rare Cyclone Rune), and three monster party formations (Solo Chimera, Chimera + Magus x2, Chimera + Minotaurus). However, due to a developer oversight, the 10-slot encounter table only referenced parties 0 to 5, leaving Chimera completely unobtainable during normal gameplay.

The classic community bugfix by Pyriel (`tenzan.asm`) restored Chimera by substituting duplicate encounter slots 1, 3, and 6 with the 3 omitted Chimera formations, giving Chimera an authentic ~30% encounter rate in Tenzan Pass.

The HD Remaster ported the original encounter table without fixing this bug. **PKTenzanPassPatch** restores Chimera encounters in Tenzan Pass using an in-memory map asset patching approach (`MACHICON.EventInit`) and battle safeguards (`BattleDataSetup`, `InitNewMonster`, `get_monster_okane`).

## Features

- **Chimera Restoration**: Restores Pyriel's 3 Chimera party formations into encounter slots 1, 3, and 6 (~30% encounter rate).
- **Guaranteed/Configurable Cyclone Rune Drop**: Chimera drops the rare Cyclone Rune upon defeat (configurable drop rate 1-100%, defaults to 100%).
- **Battle Grid & Array Protection**: Ensures complete 18-element battlefield position coordinate arrays and reinforces `monster_data` to prevent out-of-bounds exceptions.
- **In-Memory Byte-Patching**: Direct memory modification on map transition (`va11`), making the mod fully compatible across game updates without modifying game archive files.
- **Force Chimera Encounter (Debug/Testing)**: Option to force all encounters in Tenzan Pass to Chimera formations for testing.

## Configuration Options

Config file location: `BepInEx/config/faospark.pktenzanpasspatch.cfg`

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

## Requirements
- BepInEx for Suikoden I & II HD Remaster (IL2CPP)
- .NET 6.0

## Credits & Acknowledgments
- **Pyriel**: Original creator of the definitive PS1 Suikoden II Tenzan Pass bugfix (`tenzan.asm`). His landmark reverse-engineering work originally uncovered the omitted Chimera party formations and established the elegant substitution of duplicate slots 1, 3, and 6 to restore Chimera's authentic ~30% spawn rate and Cyclone Rune drop.
- **faospark**: Porting, IL2CPP in-memory byte-patching implementation (`MACHICON.EventInit`), battlefield position coordinate restoration, and Remaster engine safeguards.
- **Suikoden Community**: For keeping the history and spirit of Suikoden alive.
