# Tenzan Pass Chimera Encounters & Cyclone Rune Drop Restoration (`va11`)

## Overview
In **Suikoden II** and the **Suikoden I & II HD Remaster**, the mountain trail of **Tenzan Pass** (map ID prefix `va11`) was originally intended to feature the elusive **Chimera** monster, dropping the ultra-rare **Cyclone Rune** (Item ID `28`, Category `0x50` / Rune).

In the original PS1 code (`VA11.BIN`), Chimera was fully animated with combat parameters and three dedicated party formations (Parties 6, 7, and 8). However, due to a developer oversight:
1. The 10-slot encounter table only referenced parties 0 through 5 with redundant duplicate entries (`[0, 0, 1, 1, 2, 3, 3, 4, 4, 5]`), completely omitting the three Chimera party formations.
2. The classic community bugfix by **Pyriel** (`tenzan.asm`) restored Chimera by substituting duplicate encounter slots 1, 3, and 6 with the 3 omitted Chimera formations, giving Chimera an authentic ~30% encounter rate.
3. In the HD Remaster, the porting team retained the bug and packed the monster definitions into a 3-element array. Spawning Chimera (MID 3) caused an `IndexOutOfRangeException` black screen unless patched.

This document details:
1. **The Script & Asset Architecture** behind Tenzan Pass (`va11`).
2. **Manual Asset/JSON Bundle Editing** via UABEA in `D:\Modding\suikodum\Mobobehaviour\va11.json` and `va11_00.json`.
3. **Mod-Based Runtime Overrides (PKCore)** following the **Sajah Shop Unlock** (`vj17`) architecture (`MACHICON.EventInit`).

---

## 1. Asset & Script Analysis (`va11`)

- **Map ScriptableObject**: `MACHIDAT` asset `va11.json`
- **Battle Data Asset**: `va11_00.json` (referenced in `va11.json.battle_dat[1..3]`)
- **Map Encounter Areas**: `mapeventdat[1]`, `mapeventdat[2]`, `mapeventdat[4]` all reference `batlno: 1` (`batltim: 30`, `batlpro: 8`).
- **Encounter Slots**: 10 slots in `va11_00.json` `monster_party`.
  - Vanilla Slot 0 & 1: Minotaur x 4 (duplicate)
  - Vanilla Slot 2 & 3: Minotaur x 5 (duplicate)
  - Vanilla Slot 4: Magus x 1
  - Vanilla Slot 5 & 6: Magus x 2 (duplicate)
  - Vanilla Slot 7 & 8: Minotaur x 3 + Magus x 2 (duplicate)
  - Vanilla Slot 9: Minotaur x 4 + Magus x 1
- **Monster IDs in `monster_data`**:
  - `0`: Empty / placeholder
  - `1`: Minotaurus (`va11_00_00`)
  - `2`: Magus (`va11_00_01`)
  - `3`: Chimera (`va11_00_03` / PathID `4066340165870708371`)

---

## 2. PS1 Ground Truth & Pyriel's `tenzan.asm` Breakdown

From [Pyriel's `tenzan.asm`](https://github.com/pyriell/gs2-bugfixes/blob/master/patch/NA/src/tenzan.asm):
```asm
; Add Chimera Parties to Encounter Table
.org 0x801363D4
.area 0x801363FC-.
encounters:
    .word 0x801362D4 ; [0] Party 0 (Minotaur x 4)
    .word 0x8013637C ; [1] Party 6 (Chimera Solo)       <-- Restored Slot 1
    .word 0x801362F0 ; [2] Party 1 (Minotaur x 5)
    .word 0x80136398 ; [3] Party 7 (Chimera + 2 Magus)   <-- Restored Slot 3
    .word 0x8013630C ; [4] Party 2 (Magus x 1)
    .word 0x80136328 ; [5] Party 3 (Magus x 2)
    .word 0x801363B4 ; [6] Party 8 (Chimera + Minotaur)  <-- Restored Slot 6
    .word 0x80136344 ; [7] Party 4 (Minotaur x 3 + Magus x 2)
    .word 0x80136344 ; [8] Party 4 (duplicate)
    .word 0x80136360 ; [9] Party 5 (Minotaur x 4 + Magus x 1)
.endarea
```

### Key Discoveries from the Disassembly:
1. **Parties 6, 7, and 8 were never fan-created**: They were defined by Konami in the original `VA11.BIN` at offsets `0x8013637C`, `0x80136398`, and `0x801363B4`. Konami simply forgot to point the encounter table to them (leaving slots 1, 3, and 6 pointing to duplicates of 0, 1, and 3).
2. **Standard 6-Position Battle Grid**:
   In Suikoden 2, enemies occupy a 3x2 grid (Front Row: 0, 1, 2 / Back Row: 3, 4, 5):
   * `Slot 0`: `(50, -60, 0)` (Front Top)
   * `Slot 1`: `(50, 0, 0)` (Front Center)
   * `Slot 2`: `(50, 60, 0)` (Front Bottom)
   * `Slot 3`: `(-50, -60, 0)` (Back Top)
   * `Slot 4`: `(-50, 0, 0)` (Back Center)
   * `Slot 5`: `(-50, 60, 0)` (Back Bottom)

3. **Authentic Formations**:
   * **Party 6 (Slot 1 - Chimera Solo)**:
     * `monster_kazu`: 1
     * `monster_id`: `[3, 0, 0, 0, 0, 0]`
     * `monster_code`: `[0, 3, 0, 0, 0, 0]` (Centered in front row position 1)
   * **Party 7 (Slot 3 - Chimera + Magus x 2)**:
     * `monster_kazu`: 3
     * `monster_id`: `[3, 2, 0, 0, 0, 0]`
     * `monster_code`: `[2, 3, 2, 0, 0, 0]` (Chimera centered at pos 1, flanked by Magus at pos 0 and 2)
   * **Party 8 (Slot 6 - Chimera + Minotaurus)**:
     * `monster_kazu`: 2
     * `monster_id`: `[3, 1, 0, 0, 0, 0]`
     * `monster_code`: `[1, 3, 0, 0, 0, 0]` (Chimera at pos 1, Minotaur at pos 0)

### 2. Configure Cyclone Rune Drop on Chimera
On Chimera's `MONSTER_DATA` (`monster_data[3]`):
- `item_data`:
  - `item_no`: `6` (Cyclone Orb / Rune ID in `embl_data`)
  - `use_cnt`: `0x20` (Category 2 = `embl_data` / Runes; 32 decimal)
- `item_prob`:
  - `[0]`: `255` (100% guaranteed drop) or authentic rate (`0x10`–`0x20`)

---

## 3. Mod-Based Runtime Overrides (PKCore)

Rather than manually repacking gigabyte-sized asset bundles with UABEA after every official game patch, **PKCore** restores Chimera encounters in memory using the same proven pattern as the **Sajah Village Rare Shop Unlock** (`SajahShopUnlockPatch`).

### The Architecture: `MACHICON.EventInit` Hook
When the player transitions into Tenzan Pass (`va11`), the engine executes `MACHICON.EventInit()`. At this exact moment, `EVENTCON.mdat` contains the deserialized `MACHIDAT` and its `battle_dat` array.

```csharp
[HarmonyPatch(typeof(MACHICON), "EventInit")]
public static class EventInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(MACHICON __instance, EVENTCON __result)
    {
        if (__result == null || __result.mdat == null) return;

        string mapId = MACHICON_OnMapInitPatch.CurrentMapId;
        bool isVa11 = (!string.IsNullOrEmpty(mapId) && mapId.StartsWith("va11", StringComparison.OrdinalIgnoreCase))
            || string.Equals(__result.mdat.name, "va11", StringComparison.OrdinalIgnoreCase);

        if (!isVa11) return;

        var battleDatList = __result.mdat.battle_dat;
        if (battleDatList == null || battleDatList.Length == 0) return;

        foreach (var battleData in battleDatList)
        {
            if (battleData != null)
                ConfigureTenzanBattleData(battleData, "EventInit");
        }
    }
}
```

### What `ConfigureTenzanBattleData` Does Automatically:
1. **Ensures 4-Slot `monster_data`**: Expands `monster_data` so Chimera (MID 3) is always safely addressed without `IndexOutOfRangeException`.
2. **Guarantees Cyclone Rune Drop**: Sets `item_no = 6`, `use_cnt = 0x20` (Category 2 = `embl_data` / Runes), and `item_prob[0] = 255`.
3. **Applies Pyriel's Formations**: Injects the 3 Chimera parties into encounter slots 1, 3, and 6 giving an intended ~30% encounter rate.
4. **Valid Position Coordinates**: Populates valid 3D battlefield coordinates (`monster_position`) for every formation, preventing camera or position lockups.
5. **Post-Battle Loot Award Hook (`BattleManager.get_monster_okane`)**:
   - In vanilla Suikoden 2, Rare drops have only a 6/255 (~2.35%) drop rate, meaning fighting 10 Chimeras gives a ~79% chance of getting no drop.
   - At battle victory, `BattleManager.get_monster_okane` determines drops and writes to `battle_work.exit_work.monster_item`. If `item_no == 0`, `exit_sub02` skips the Item Get screen entirely.
   - PKCore hooks `get_monster_okane` Postfix: when Chimera is defeated, it awards the Cyclone Rune (`item_no = 6`, `use_cnt = 0x20`) into `battle_work.exit_work.monster_item` before `exit_sub02` checks the drop, seamlessly triggering the item fanfare, dialogue, and inventory addition.
6. **Multi-Tier Failsafes**: Maintains `BattleManager.BattleDataSetup`, `InitNewMonster`, `get_monster_okane`, and `b_OverlayAddItem2Init` guards for seamless gameplay under all conditions.

### Configuration (`BepInEx/config/PKCore.cfg`):
- `EnableTenzanChimeraFix = true`: Master toggle to restore Chimera encounters and drops.
- `ForceTenzanChimeraEncounter = false`: Debug/testing toggle to force all random encounters in Tenzan Pass to be Chimera formations.
- `TenzanChimeraCycloneDropRate = 100`: Drop rate percentage (1–100%) for the Cyclone Rune when defeating a Chimera. Defaults to 100% for a guaranteed drop upon victory.

### Advantages:
- **Zero Asset Bundle Modification**: No UABEA imports, no risk of corrupting bundle headers.
- **Steam Verification Proof**: Survives official Steam updates and file integrity checks.
- **Clean Configuration**: Configurable in `BepInEx/config/PKCore.cfg`.
