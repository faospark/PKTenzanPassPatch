# Changelog - PKTenzanPassPatch

All notable changes to PKTenzanPassPatch will be documented in this file.

---

## [1.0.1] - 2026-11-03

### Changed
- **Encounter Logging & Config Guards ([TenzanPassEncounterPatch.cs](Patches/TenzanPassEncounterPatch.cs))**:
  - Removed verbose runtime encounter logging (`InitNewMonster` Postfix chatter and `battle_work.monster_data` reinforcement logs) to keep `LogOutput.log` clean.
  - Added strict check on `Plugin.Config.EnableTenzanChimeraFix.Value` before evaluating battle data setups and drops.

### Added
- **Deep Technical Documentation**:
  - Added [tenzan_pass_chimera_va11.md](Documentation/tenzan_pass_chimera_va11.md) detailing the complete bytecode analysis, encounter rate mathematics, Pyriel formation mappings, and drop rates.

---

## [1.0.0] - 2026-10-26

### Added
- **Tenzan Pass Chimera Encounter Restoration ([TenzanPassEncounterPatch.cs](Patches/TenzanPassEncounterPatch.cs))**:
  - Restores the omitted Chimera monster encounters in Tenzan Pass (`va11`) across Suikoden II.
  - Ported Pyriel's classic community fix (`tenzan.asm`) to a 100% in-memory Harmony patch hooking `MACHICON.EventInit`:
    - Injects Chimera party formations (Parties 6, 7, and 8) into redundant duplicate encounter table slots 1, 3, and 6.
    - Restores the authentic ~30% Chimera encounter frequency on the mountain trail.
- **Battlefield Grid & Memory Protection**:
  - Dynamically reinforces `battle_work.monster_data` to 4 slots to prevent `IndexOutOfRangeException` when referencing Chimera (`monster_id = 3`).
  - Populates valid 3D battlefield coordinates (`monster_position`) for all Chimera parties to prevent camera lockups.
- **Guaranteed / Configurable Cyclone Rune Drop**:
  - Hooks `BattleManager.get_monster_okane` to ensure defeating a Chimera awards the legendary **Cyclone Rune** (`item_no = 6`, `use_cnt = 0x20`), bypassing the harsh vanilla 6/255 (2.35%) drop rate.
- **Configuration Options ([ModConfiguration.cs](ModConfiguration.cs))**:
  - `EnableTenzanChimeraFix` (default: `true`): Master toggle for Chimera encounter and drop restoration.
  - `ForceTenzanChimeraEncounter` (default: `false`): Debug/testing toggle forcing 100% of Tenzan Pass encounters to Chimera formations.
  - `TenzanChimeraCycloneDropRate` (default: `100`): Configurable drop rate percentage (1–100%) for the Cyclone Rune.
