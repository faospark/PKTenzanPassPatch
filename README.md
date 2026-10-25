# PKTenzanPassPatch (Suikoden II Tenzan Pass Encounter & Chimera Restoration)

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

---

## Requirements

1. **Suikoden I & II HD Remaster** (Steam edition, 64-bit Windows)
2. **BepInEx 6.0 (Unity IL2CPP x64)**
   - Download **[BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)**.

---

## Installation Guide (Steam)

### 1. Locate Game Root Directory
In your Steam client:
1. Open your **Library** and right-click **Suikoden I & II HD Remaster**.
2. Select **Manage** → **Browse local files**.
3. A file explorer window will open at your game's root directory:
   ```text
   ...\steamapps\common\Suikoden I and II HD Remaster\
   ```
   *(This folder contains `Suikoden I and II HD Remaster.exe` and game files).*

---

### 2. Install BepInEx 6 (If not already installed)
1. Download **[BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)**.
2. Extract the contents of the ZIP archive directly into your **`Suikoden I and II HD Remaster`** root folder.
3. You should see `doorstop_config.ini`, `winhttp.dll` (or `version.dll`), and the `BepInEx/` directory alongside the game executable.
4. **Launch the game once via Steam**, then quit to desktop.
   - This generates necessary IL2CPP interop assemblies and configuration directories.

---

### 3. Install PKTenzanPassPatch
The release package is bundled with the complete directory structure for instant drag-and-drop installation.

1. Download **`PKTenzanPassPatch-vX.X.X.zip`** from [Releases](https://github.com/faospark/PKTenzanPassPatch/releases) (or grab `PKTenzanPassPatch.dll`).
2. Open the ZIP archive. You will see a `BepInEx` folder.
3. **Drag and drop the `BepInEx` folder directly into your game's root directory**:
   ```text
   Suikoden I and II HD Remaster/
   ├── BepInEx/
   │   ├── config/
   │   │   └── faospark.pktenzanpasspatch.cfg (created automatically on first run)
   │   └── plugins/
   │       └── PKTenzanPassPatch.dll
   ├── doorstop_config.ini
   └── Suikoden I and II HD Remaster.exe
   ```
4. If prompted by Windows, choose **Replace the files in the destination**.

---

### 4. Verify Installation
1. Start the game through Steam.
2. Check `BepInEx/LogOutput.log` (or console if enabled). You will see:
   ```text
   [Info   :   BepInEx] Loading [PK Tenzan Pass Patch 1.0.0]
   [Info   :PK Tenzan Pass Patch] Loading PKTenzanPassPatch (Tenzan Pass Encounter & Chimera Restoration Mod)...
   [Info   :PK Tenzan Pass Patch] PKTenzanPassPatch loaded successfully!
   ```
3. When you travel to Tenzan Pass (`va11`), you will see:
   ```text
   [Info   :PK TenzanPassPatch] Tenzan Pass (va11) encounter data successfully patched at EventInit! (X battle_dat sets updated with Chimera & Cyclone Rune drop).
   ```

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

## Building from Source

Prerequisites:
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
- BepInEx 6.x IL2CPP installed in your game directory

To compile:
```bash
dotnet build -c Release
```

---

## Credits & Acknowledgments

- **Pyriel**: Original creator of the definitive PS1 Suikoden II Tenzan Pass bugfix (`tenzan.asm`). His landmark reverse-engineering work originally uncovered the omitted Chimera party formations and established the elegant substitution of duplicate slots 1, 3, and 6 to restore Chimera's authentic ~30% spawn rate and Cyclone Rune drop.
- **faospark**: Porting, IL2CPP in-memory byte-patching implementation (`MACHICON.EventInit`), battlefield position coordinate restoration, and Remaster engine safeguards.
- **Framework**: [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX).
- **Suikoden Community**: For keeping the history and spirit of Suikoden alive.
