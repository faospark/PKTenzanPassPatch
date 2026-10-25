extern alias GSD2;

using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PKTenzanPassPatch.Patches
{
    /// <summary>
    /// Restores the missing Chimera monster encounters in Tenzan Pass (Suikoden 2 / GSD2).
    ///
    /// Background:
    /// In the original Suikoden II PS1 release (VA11.BIN), Chimera was fully defined with stats,
    /// drops (including the rare Cyclone Rune), animations, and 3 party formations (Parties 6, 7, 8).
    /// However, due to a developer oversight, the 10-slot encounter table only referenced Parties 0 to 5,
    /// leaving Chimera inaccessible in normal gameplay.
    ///
    /// The classic community bugfix by Pyriel (tenzan.asm) restored Chimera by substituting duplicate
    /// slots 1, 3, and 6 in the 10-slot encounter table with the 3 omitted Chimera formations,
    /// giving Chimera an intended ~30% encounter rate in Tenzan Pass.
    ///
    /// The HD Remaster ported the original encounter table without fixing this bug. This patch
    /// intercepts BattleDataSetup when in Tenzan Pass (map prefix 'va11'), verifies the monster data,
    /// ensures valid battlefield position arrays are provided for multi-monster parties,
    /// and safely places the 3 Chimera party formations in slots 1, 3, and 6.
    /// </summary>
    public static class TenzanPassEncounterPatch
    {
        public static string CurrentMapId = "";
        private static bool _loggedTenzanInfo = false;
        internal static bool _battleHadChimera = false;
        internal static GSD2::MONSTER_DATA _cachedMinotaur = null;
        internal static GSD2::MONSTER_DATA _cachedMagus = null;
        internal static GSD2::MONSTER_DATA _cachedChimera = null;

        public static void Initialize(Harmony harmony)
        {
            try
            {
                harmony.PatchAll(typeof(EventInitPatch));
                harmony.PatchAll(typeof(BattleDataSetupPatch));
                harmony.PatchAll(typeof(InitNewMonsterPatch));
                harmony.PatchAll(typeof(GetMonsterOkanePatch));
                harmony.PatchAll(typeof(OverlayAddItem2InitPatch));
                Plugin.Log.LogInfo("[TenzanPassEncounterPatch] Registered Tenzan Pass encounter patches (EventInit, BattleDataSetup & Loot) successfully.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[TenzanPassEncounterPatch] Failed to register patch: {ex}");
            }
        }

        public static bool IsTenzanPass(string currentMapId, GSD2::BATTLE_DATA battleData)
        {
            if (!string.IsNullOrEmpty(currentMapId) && currentMapId.StartsWith("va11", StringComparison.OrdinalIgnoreCase))
                return true;

            if (battleData != null && !string.IsNullOrEmpty(battleData.id))
            {
                if (battleData.id.IndexOf("va11", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    battleData.id.IndexOf("tenzan", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Map initialization hook following the SajahShopUnlockPatch pattern (MACHICON.EventInit).
        /// When the player enters Tenzan Pass (map prefix 'va11' / MACHIDAT name 'va11'),
        /// __result.mdat.battle_dat is directly inspected and patched ahead of time,
        /// ensuring Chimera (MID 3), its 3 party formations, and guaranteed Cyclone Rune drop
        /// are established cleanly before any battle coroutine ever runs.
        /// </summary>
        [HarmonyPatch(typeof(MACHICON), "EventInit")]
        public static class EventInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MACHICON __instance, EVENTCON __result)
            {
                try
                {
                    if (Plugin.Config == null || !Plugin.Config.EnableTenzanChimeraFix.Value)
                        return;

                    string mapId = __result?.mdat?.name;
                    if (string.IsNullOrEmpty(mapId) && __instance != null)
                    {
                        string raw = __instance.GetMapName();
                        if (!string.IsNullOrEmpty(raw))
                        {
                            mapId = raw.Split(' ')[0];
                        }
                    }

                    CurrentMapId = mapId ?? "";

                    bool isVa11 = CurrentMapId.StartsWith("va11", StringComparison.OrdinalIgnoreCase);

                    if (!isVa11)
                        return;

                    if (__result == null || __result.mdat == null)
                        return;

                    var battleDatList = __result.mdat.battle_dat;
                    if (battleDatList == null || battleDatList.Length == 0)
                        return;

                    int modifiedCount = 0;
                    for (int b = 0; b < battleDatList.Length; b++)
                    {
                        var battleData = battleDatList[b];
                        if (battleData == null) continue;

                        if (ConfigureTenzanBattleData(battleData, $"EventInit (battle_dat[{b}])"))
                        {
                            modifiedCount++;
                        }
                    }

                    if (modifiedCount > 0)
                    {
                        Plugin.Log.LogInfo($"[PKTenzanPassPatch] Tenzan Pass (va11) encounter data successfully patched at EventInit! ({modifiedCount} battle_dat sets updated with Chimera & Cyclone Rune drop).");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] EventInit Postfix error: {ex}");
                }
            }
        }

        /// <summary>
        /// Configures BATTLE_DATA for Tenzan Pass:
        /// 1. Verifies/expands monster_data so Chimera (MID 3) is present and accessible without IndexOutOfRangeException.
        /// 2. Configures guaranteed Cyclone Rune drop (Item ID 6, Category 0x20 in embl_data, 100% prob) on Chimera.
        /// 3. Injects Pyriel's 3 Chimera party formations into encounter slots 1, 3, and 6 (~30% encounter rate).
        /// 4. Provides complete, safe battlefield position coordinate arrays for all multi-monster formations.
        /// </summary>
        public static bool ConfigureTenzanBattleData(GSD2::BATTLE_DATA battleData, string source)
        {
            if (battleData == null) return false;

            var parties = battleData.monster_party;
            var monsters = battleData.monster_data;

            if (parties == null || parties.Length == 0 || monsters == null || monsters.Length == 0)
            {
                Plugin.Log.LogWarning($"[TenzanPassEncounterPatch] [{source}] Encountered Tenzan Pass battle data with empty parties or monsters.");
                return false;
            }

            // 1. Identify monsters in Tenzan Pass monster_data
            int chimeraIndex = -1;
            int minotaurIndex = -1;
            int magusIndex = -1;

            for (int i = 0; i < monsters.Length; i++)
            {
                var m = monsters[i];
                if (m == null) continue;
                string mId = m.id ?? "";

                // In HD Remaster, monsters are named va11_00_00 (Minotaur), va11_00_01 (Magus), va11_00_02 (Chimera)
                if (mId.EndsWith("_02", StringComparison.OrdinalIgnoreCase) ||
                    mId.EndsWith("02", StringComparison.OrdinalIgnoreCase) ||
                    mId.IndexOf("cim", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    mId.IndexOf("chimera", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    chimeraIndex = i;
                }
                else if (mId.EndsWith("_00", StringComparison.OrdinalIgnoreCase) ||
                         mId.EndsWith("00", StringComparison.OrdinalIgnoreCase) ||
                         mId.IndexOf("mino", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         mId.IndexOf("mao", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    minotaurIndex = i;
                }
                else if (mId.EndsWith("_01", StringComparison.OrdinalIgnoreCase) ||
                         mId.EndsWith("01", StringComparison.OrdinalIgnoreCase) ||
                         mId.IndexOf("mag", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    magusIndex = i;
                }
            }

            // Fallback to standard 3-monster layout (1 = Minotaurus, 2 = Magus, 3 = Chimera or 0, 1, 2)
            if (chimeraIndex == -1 && monsters.Length >= 4)
            {
                minotaurIndex = 1;
                magusIndex = 2;
                chimeraIndex = 3;
            }
            else if (chimeraIndex == -1 && monsters.Length >= 3)
            {
                minotaurIndex = 0;
                magusIndex = 1;
                chimeraIndex = 2;
            }

            if (chimeraIndex == -1)
            {
                Plugin.Log.LogWarning($"[TenzanPassEncounterPatch] [{source}] Could not identify Chimera in monster_data.");
                return false;
            }

            var minotaurObj = (minotaurIndex >= 0 && minotaurIndex < monsters.Length) ? monsters[minotaurIndex] : monsters[0];
            var magusObj = (magusIndex >= 0 && magusIndex < monsters.Length) ? monsters[magusIndex] : (monsters.Length > 1 ? monsters[1] : monsters[0]);
            var chimeraObj = (chimeraIndex >= 0 && chimeraIndex < monsters.Length) ? monsters[chimeraIndex] : (monsters.Length > 2 ? monsters[2] : monsters[0]);

            _cachedMinotaur = minotaurObj;
            _cachedMagus = magusObj;
            _cachedChimera = chimeraObj;

            // Guarantee Cyclone Rune drop for Chimera (use_cnt = 0x20 [Category 2 = embl_data/Runes], item_no = 6 [Cyclone Orb/Rune])
            try
            {
                if (chimeraObj.item_data == null || chimeraObj.item_data.Length < 3)
                {
                    var items = new Il2CppReferenceArray<GSD2::PARTY_ITEM>(3);
                    for (int i = 0; i < 3; i++)
                    {
                        var rune = new GSD2::PARTY_ITEM();
                        rune.use_cnt = 0x20; // Category 2 = embl_data (Runes / Orbs)
                        rune.item_no = 6;    // Index 6 in embl_data = Cyclone Orb
                        items[i] = rune;
                    }
                    chimeraObj.item_data = items;
                }
                else
                {
                    for (int i = 0; i < chimeraObj.item_data.Length; i++)
                    {
                        var rune = chimeraObj.item_data[i] ?? new GSD2::PARTY_ITEM();
                        rune.use_cnt = 0x20; // Category 2 = embl_data (Runes / Orbs)
                        rune.item_no = 6;    // Index 6 in embl_data = Cyclone Orb
                        chimeraObj.item_data[i] = rune;
                    }
                }

                if (chimeraObj.item_prob == null || chimeraObj.item_prob.Length < 3)
                {
                    var probs = new Il2CppStructArray<byte>(3);
                    probs[0] = 255; // 100% drop probability
                    probs[1] = 0;
                    probs[2] = 0;
                    chimeraObj.item_prob = probs;
                }
                else
                {
                    chimeraObj.item_prob[0] = 255;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[TenzanPassEncounterPatch] [{source}] Failed to set guaranteed Chimera drop: {ex}");
            }

            // Expand monster_data to 4 slots (1-based indexing) if needed
            if (monsters.Length < 4 || monsters[3] == null)
            {
                var expanded = new Il2CppReferenceArray<GSD2::MONSTER_DATA>(Math.Max(4, monsters.Length));
                for (int i = 0; i < monsters.Length; i++)
                    expanded[i] = monsters[i];

                expanded[0] = minotaurObj; // placeholder
                expanded[1] = minotaurObj; // MID 1 = Minotaurus
                expanded[2] = magusObj;    // MID 2 = Magus
                expanded[3] = chimeraObj;  // MID 3 = Chimera

                battleData.monster_data = expanded;
                monsters = expanded;
            }

            sbyte minotaurMid = 1;
            sbyte magusMid = 2;
            sbyte chimeraMid = 3;

            // Extract authentic battlefield monster position templates from untouched parties before modifying
            short[] pos1Monster = null;
            short[] pos2Monster = null;
            short[] pos3Monster = null;

            for (int i = 0; i < parties.Length; i++)
            {
                var p = parties[i];
                if (p == null || p.data == null || p.data.monster_position == null) continue;
                var pos = p.data.monster_position;

                if (p.monster_kazu == 1 && pos.Length >= 3 && pos1Monster == null)
                {
                    pos1Monster = new short[pos.Length];
                    for (int k = 0; k < pos.Length; k++) pos1Monster[k] = pos[k];
                }
                else if (p.monster_kazu == 2 && pos.Length >= 6 && pos2Monster == null)
                {
                    pos2Monster = new short[pos.Length];
                    for (int k = 0; k < pos.Length; k++) pos2Monster[k] = pos[k];
                }
                else if (p.monster_kazu == 3 && pos.Length >= 9 && pos3Monster == null)
                {
                    pos3Monster = new short[pos.Length];
                    for (int k = 0; k < pos.Length; k++) pos3Monster[k] = pos[k];
                }
            }

            // Check if Chimera is already present in any encounter party
            bool chimeraAlreadyInTable = false;
            for (int i = 0; i < parties.Length; i++)
            {
                var p = parties[i];
                if (p == null || p.monster_id == null) continue;
                for (int s = 0; s < p.monster_id.Length; s++)
                {
                    if (p.monster_id[s] == chimeraMid)
                    {
                        chimeraAlreadyInTable = true;
                        break;
                    }
                }
                if (chimeraAlreadyInTable) break;
            }

            if (!chimeraAlreadyInTable)
            {
                // Apply Pyriel's bugfix layout to the 10 encounter slots:
                // Replace duplicate slots 1, 3, and 6 with the 3 Chimera formations.
                if (parties.Length >= 7)
                {
                    // Slot 1 (Party 6): Chimera Solo (1 Chimera, centered at front row position 1)
                    ConfigurePartyFormation(
                        parties[1],
                        count: 1,
                        uniqueMonsterIds: new sbyte[] { chimeraMid, 0, 0, 0, 0, 0 },
                        gridCodes: new sbyte[] { 0, chimeraMid, 0, 0, 0, 0 }
                    );

                    // Slot 3 (Party 7): Chimera + Magus x 2 (Chimera centered, flanked by 2 Magus at positions 0 and 2)
                    ConfigurePartyFormation(
                        parties[3],
                        count: 3,
                        uniqueMonsterIds: new sbyte[] { chimeraMid, magusMid, 0, 0, 0, 0 },
                        gridCodes: new sbyte[] { magusMid, chimeraMid, magusMid, 0, 0, 0 }
                    );

                    // Slot 6 (Party 8): Chimera + Minotaurus (Chimera at position 1, Minotaurus at position 0)
                    ConfigurePartyFormation(
                        parties[6],
                        count: 2,
                        uniqueMonsterIds: new sbyte[] { chimeraMid, minotaurMid, 0, 0, 0, 0 },
                        gridCodes: new sbyte[] { minotaurMid, chimeraMid, 0, 0, 0, 0 }
                    );

                    Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] [{source}] Restored 3 Chimera parties into encounter slots 1, 3, and 6 (~30% spawn rate) with authentic 6-position battlefield grid & Cyclone Rune drop.");
                }
                else
                {
                    ConfigurePartyFormation(
                        parties[parties.Length > 1 ? 1 : 0],
                        count: 1,
                        uniqueMonsterIds: new sbyte[] { chimeraMid, 0, 0, 0, 0, 0 },
                        gridCodes: new sbyte[] { 0, chimeraMid, 0, 0, 0, 0 }
                    );
                    Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] [{source}] Restored Chimera party into slot 1.");
                }
                return true;
            }
            else if (!_loggedTenzanInfo)
            {
                _loggedTenzanInfo = true;
                Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] [{source}] Chimera encounters already confirmed active in Tenzan Pass.");
            }

            return false;
        }

        // Canonical 6-slot battlefield coordinates in Suikoden 2 HD Remaster (Front: 0,1,2 / Back: 3,4,5)
        public static readonly short[] CanonicalBattleGrid = new short[]
        {
             50, -60, 0,   // Position 0: Front Top
             50,   0, 0,   // Position 1: Front Center
             50,  60, 0,   // Position 2: Front Bottom
            -50, -60, 0,   // Position 3: Back Top
            -50,   0, 0,   // Position 4: Back Center
            -50,  60, 0    // Position 5: Back Bottom
        };

        public static void ConfigurePartyFormation(
            GSD2::MONSTER_PARTY_DATA party,
            int count,
            sbyte[] uniqueMonsterIds,
            sbyte[] gridCodes,
            short[] positionTemplate = null)
        {
            if (party == null) return;

            party.monster_kazu = count;

            if (party.monster_id == null || party.monster_id.Length < 6)
            {
                party.monster_id = new Il2CppStructArray<sbyte>(6);
            }
            for (int i = 0; i < 6; i++)
            {
                party.monster_id[i] = (uniqueMonsterIds != null && i < uniqueMonsterIds.Length) ? uniqueMonsterIds[i] : (sbyte)0;
            }

            // In the HD Remaster engine, BattleManager.MonsterInit reads party.monster_code (offset 0x30)
            // for each of the 6 battlefield grid positions and passes it as monster_id to InitNewMonster.
            if (party.monster_code == null || party.monster_code.Length < 6)
            {
                party.monster_code = new Il2CppStructArray<sbyte>(6);
            }
            for (int i = 0; i < 6; i++)
            {
                party.monster_code[i] = (gridCodes != null && i < gridCodes.Length) ? gridCodes[i] : (sbyte)0;
            }

            if (party.data == null)
            {
                party.data = new GSD2::MONSTER_PARTY_SUB_DATA();
            }

            // Ensure complete 18-element canonical battle grid coordinates (6 slots x 3 coordinates)
            short[] coords = (positionTemplate != null && positionTemplate.Length >= 18) ? positionTemplate : CanonicalBattleGrid;

            int requiredShorts = Math.Max(18, coords.Length);
            var posArray = new Il2CppStructArray<short>(requiredShorts);
            for (int i = 0; i < coords.Length; i++)
            {
                posArray[i] = coords[i];
            }
            party.data.monster_position = posArray;
        }

        [HarmonyPatch(typeof(GSD2::BattleManager), "BattleDataSetup")]
        public static class BattleDataSetupPatch
        {
            [HarmonyPrefix]
            public static void Prefix(GSD2::BattleManager __instance)
            {
                try
                {
                    if (Plugin.Config == null || !Plugin.Config.EnableTenzanChimeraFix.Value)
                        return;

                    var sysWork = GSD2::OldSrcBase.sys_work;
                    if (sysWork == null || sysWork.battle_data == null)
                        return;

                    var battleData = sysWork.battle_data;
                    string currentMapId = CurrentMapId ?? "";

                    if (!IsTenzanPass(currentMapId, battleData))
                        return;

                    _battleHadChimera = false;

                    // Ensure Tenzan battle data is fully configured as fallback/safeguard
                    ConfigureTenzanBattleData(battleData, "BattleDataSetup");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] Prefix error: {ex}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(GSD2::BattleManager __instance)
            {
                try
                {
                    if (Plugin.Config == null || !Plugin.Config.EnableTenzanChimeraFix.Value)
                        return;

                    if (__instance == null || __instance.battle_work == null)
                        return;

                    string currentMapId = CurrentMapId ?? "";
                    var battleData = __instance.battle_work.battle_data ?? GSD2::OldSrcBase.sys_work?.battle_data;

                    if (!IsTenzanPass(currentMapId, battleData))
                        return;

                    // Crucial safety reinforcement: Ensure battle_work.monster_data has at least 4 slots
                    // so InitNewMonster never throws IndexOutOfRangeException on monster_id = 3
                    var bwMonsters = __instance.battle_work.monster_data;
                    var srcMonsters = battleData?.monster_data;
                    if (bwMonsters == null || bwMonsters.Length < 4)
                    {
                        var bwExpanded = new Il2CppReferenceArray<GSD2::MONSTER_DATA>(4);
                        if (bwMonsters != null)
                        {
                            for (int i = 0; i < bwMonsters.Length && i < 4; i++)
                                bwExpanded[i] = bwMonsters[i];
                        }
                        if (srcMonsters != null && srcMonsters.Length >= 4)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (bwExpanded[i] == null)
                                    bwExpanded[i] = srcMonsters[i];
                            }
                        }
                        __instance.battle_work.monster_data = bwExpanded;
                        Plugin.Log.LogInfo("[TenzanPassEncounterPatch] Reinforced battle_work.monster_data to 4 slots in Postfix.");
                    }

                    var parties = battleData?.monster_party;

                    // Support Debug / Force Chimera Encounter testing
                    if (Plugin.Config.ForceTenzanChimeraEncounter.Value && parties != null && parties.Length > 1)
                    {
                        // Pick randomly between the 3 restored Chimera slots (1, 3, 6)
                        int[] chimeraSlots = parties.Length >= 7 ? new[] { 1, 3, 6 } : new[] { 1 };
                        int chosenSlot = chimeraSlots[UnityEngine.Random.Range(0, chimeraSlots.Length)];

                        __instance.battle_work.monster_party_data = parties[chosenSlot];
                        __instance.battle_work.monster_party_index = chosenSlot + 1;

                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] ForceTenzanChimeraEncounter active: Forced battle to Chimera slot {chosenSlot}.");
                    }

                    var selectedParty = __instance.battle_work.monster_party_data;
                    int partyIndex = __instance.battle_work.monster_party_index;

                    if (selectedParty != null)
                    {
                        string formationInfo = "";
                        if (selectedParty.monster_id != null)
                        {
                            for (int i = 0; i < selectedParty.monster_id.Length; i++)
                            {
                                sbyte mid = selectedParty.monster_id[i];
                                if (mid != 0)
                                    formationInfo += $"[Slot {i}: ID {mid}] ";
                            }
                        }
                        int posCount = selectedParty.data?.monster_position?.Length ?? 0;
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] Tenzan Battle Initialized -> Party #{partyIndex} (Monsters: {selectedParty.monster_kazu}, PositionShorts: {posCount}): {formationInfo}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] Postfix error: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(GSD2::BattleManager), "InitNewMonster")]
        public static class InitNewMonsterPatch
        {
            [HarmonyPrefix]
            public static void Prefix(GSD2::BattleManager __instance, int chara_no, ref int monster_id, int position)
            {
                try
                {
                    if (Plugin.Config == null || !Plugin.Config.EnableTenzanChimeraFix.Value)
                        return;

                    if (__instance == null || __instance.battle_work == null)
                        return;

                    string currentMapId = CurrentMapId ?? "";
                    var battleData = __instance.battle_work.battle_data ?? GSD2::OldSrcBase.sys_work?.battle_data;

                    if (!IsTenzanPass(currentMapId, battleData))
                        return;

                    // Remap negative/macro values if any slipped through from legacy code
                    if (monster_id == -79 || monster_id == 177 || (sbyte)monster_id == (sbyte)GSD2::b_read_h.MF_CIM_0)
                    {
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] InitNewMonster: Sanitizing chimera monster_id {monster_id} -> 3");
                        monster_id = 3;
                    }
                    else if (monster_id == (sbyte)GSD2::b_read_h.MF_MAG_0)
                    {
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] InitNewMonster: Sanitizing magus monster_id {monster_id} -> 2");
                        monster_id = 2;
                    }
                    else if (monster_id == (sbyte)GSD2::b_read_h.MF_MAO_0)
                    {
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] InitNewMonster: Sanitizing minotaur monster_id {monster_id} -> 1");
                        monster_id = 1;
                    }

                    var bw = __instance.battle_work;

                    Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] InitNewMonster Prefix: chara_no={chara_no}, monster_id={monster_id}, position={position}, mdLen={bw.monster_data?.Length}, posLen={bw.monster_position?.Length}");

                    // 1. Guard against monster_data array bounds (InitNewMonster checks battle_work.monster_data.Length <= monster_id)
                    bool needsMdExpand = false;
                    if (bw.monster_data == null)
                    {
                        needsMdExpand = true;
                    }
                    else if (monster_id < 0 || monster_id >= bw.monster_data.Length)
                    {
                        needsMdExpand = true;
                    }
                    else if (bw.monster_data[monster_id] == null)
                    {
                        needsMdExpand = true;
                    }

                    if (needsMdExpand)
                    {
                        int targetSize = Math.Max(Math.Max(monster_id + 1, 4), bw.monster_data != null ? bw.monster_data.Length : 4);
                        var newArr = new Il2CppReferenceArray<GSD2::MONSTER_DATA>(targetSize);
                        if (bw.monster_data != null)
                        {
                            for (int i = 0; i < bw.monster_data.Length && i < targetSize; i++)
                                newArr[i] = bw.monster_data[i];
                        }
                        if (_cachedMinotaur != null)
                        {
                            newArr[0] = _cachedMinotaur;
                            if (newArr.Length > 1 && newArr[1] == null) newArr[1] = _cachedMinotaur;
                        }
                        if (_cachedMagus != null && newArr.Length > 2 && newArr[2] == null)
                        {
                            newArr[2] = _cachedMagus;
                        }
                        if (_cachedChimera != null && newArr.Length > 3 && newArr[3] == null)
                        {
                            newArr[3] = _cachedChimera;
                        }
                        bw.monster_data = newArr;
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] Dynamically protected InitNewMonster: expanded monster_data to length {targetSize} for monster_id {monster_id}.");
                    }

                    // 2. Guard against monster_position array bounds (position is 1-based, uses position * 3)
                    int requiredShorts = Math.Max(0, position) * 3;
                    if (bw.monster_position == null || bw.monster_position.Length < requiredShorts)
                    {
                        int targetPosSize = Math.Max(18, requiredShorts);
                        var newPos = new Il2CppStructArray<short>(targetPosSize);
                        if (bw.monster_position != null)
                        {
                            for (int i = 0; i < bw.monster_position.Length && i < targetPosSize; i++)
                                newPos[i] = bw.monster_position[i];
                        }
                        bw.monster_position = newPos;
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] Dynamically protected InitNewMonster: expanded monster_position to {targetPosSize} shorts for position {position}.");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] InitNewMonster Prefix error: {ex}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(GSD2::BattleManager __instance, int chara_no, int monster_id, int position)
            {
                try
                {
                    if (monster_id == 3 || (_cachedChimera != null && __instance?.battle_work?.monster_data != null && monster_id < __instance.battle_work.monster_data.Length && __instance.battle_work.monster_data[monster_id] == _cachedChimera))
                    {
                        _battleHadChimera = true;
                    }

                    if (__instance?.battle_work?.b_chara != null && chara_no >= 0 && chara_no < __instance.battle_work.b_chara.Length)
                    {
                        var chara = __instance.battle_work.b_chara[chara_no];
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] InitNewMonster Postfix: chara_no={chara_no} initialized -> {(chara != null ? "SUCCESS" : "NULL!")}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] InitNewMonster Postfix error: {ex}");
                }
            }
        }

        /// <summary>
        /// Post-battle loot award hook on BattleManager.get_monster_okane.
        /// This method executes right after battle victory when monster exp, money,
        /// and drops are calculated and assigned to battle_work.exit_work.monster_item.
        /// Hooking here guarantees that when a Chimera is defeated, the Cyclone Rune
        /// is safely set before exit_sub02 branches to BattleItemGet!
        /// </summary>
        [HarmonyPatch(typeof(GSD2::BattleManager), "get_monster_okane")]
        public static class GetMonsterOkanePatch
        {
            [HarmonyPostfix]
            public static void Postfix(GSD2::BattleManager __instance, GSD2::BATTLE_WORK battle_work)
            {
                try
                {
                    if (Plugin.Config == null || !Plugin.Config.EnableTenzanChimeraFix.Value)
                        return;

                    var bw = battle_work ?? __instance?.battle_work;
                    if (bw == null || bw.exit_work == null)
                        return;

                    string currentMapId = CurrentMapId ?? "";
                    var battleData = bw.battle_data ?? GSD2::OldSrcBase.sys_work?.battle_data;
                    if (!IsTenzanPass(currentMapId, battleData))
                        return;

                    // Check if Chimera was in this battle and defeated
                    bool chimeraDefeated = _battleHadChimera;
                    if (!chimeraDefeated && bw.dead_monster_data != null)
                    {
                        for (int i = 0; i < bw.dead_monster_data.Length; i++)
                        {
                            var dm = bw.dead_monster_data[i];
                            if (dm != null && dm.chara_id == 3)
                            {
                                chimeraDefeated = true;
                                break;
                            }
                        }
                    }

                    if (!chimeraDefeated)
                        return;

                    int dropRate = Plugin.Config.TenzanChimeraCycloneDropRate.Value;
                    int roll = UnityEngine.Random.Range(1, 101);

                    if (roll <= dropRate)
                    {
                        var monsterItem = bw.exit_work.monster_item;
                        if (monsterItem == null)
                        {
                            monsterItem = new GSD2::PARTY_ITEM();
                            bw.exit_work.monster_item = monsterItem;
                        }

                        // Set Cyclone Rune (Category 0x20 = embl_data/Runes, Index 6 = Cyclone Orb)
                        monsterItem.item_no = 6;
                        monsterItem.use_cnt = 0x20;

                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] get_monster_okane Postfix: Awarded Cyclone Rune (item_no=6, use_cnt=0x20) from defeated Chimera! (Roll: {roll}/{dropRate}%)");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[TenzanPassEncounterPatch] get_monster_okane Postfix: Cyclone Rune drop roll missed (Roll: {roll}/{dropRate}%).");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] get_monster_okane Postfix error: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(GSD2::BattleManager), "b_OverlayAddItem2Init")]
        public static class OverlayAddItem2InitPatch
        {
            [HarmonyPrefix]
            public static void Prefix(GSD2::PARTY_ITEM get_item)
            {
                try
                {
                    if (Plugin.Config == null || !Plugin.Config.EnableTenzanChimeraFix.Value)
                        return;

                    string currentMapId = CurrentMapId ?? "";
                    if (!currentMapId.StartsWith("va11", StringComparison.OrdinalIgnoreCase))
                        return;

                    if (_battleHadChimera && get_item != null)
                    {
                        if (get_item.item_no == 0)
                        {
                            get_item.item_no = 6;    // Index 6 in embl_data = Cyclone Orb
                            get_item.use_cnt = 0x20; // Category 2 = embl_data (Runes / Orbs)
                            Plugin.Log.LogInfo("[TenzanPassEncounterPatch] Guaranteed Cyclone Rune awarded via b_OverlayAddItem2Init failsafe!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[TenzanPassEncounterPatch] OverlayAddItem2Init Prefix error: {ex}");
                }
            }
        }
    }
}
