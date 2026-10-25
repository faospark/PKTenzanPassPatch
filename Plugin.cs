using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKTenzanPassPatch.Patches;

namespace PKTenzanPassPatch;

[BepInPlugin("faospark.pktenzanpasspatch", "PK Tenzan Pass Patch", "1.0.0")]
[BepInDependency("faospark.pkcore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public static Plugin Instance { get; private set; }
    public static new ManualLogSource Log;
    public static new ModConfiguration Config;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        Log.LogInfo("Loading PKTenzanPassPatch (Tenzan Pass Encounter & Chimera Restoration Mod)...");

        Config = new ModConfiguration(base.Config);
        Config.Init();

        Harmony harmony = new Harmony("faospark.pktenzanpasspatch");

        // Tenzan Pass Chimera Encounters Restoration
        if (Config.EnableTenzanChimeraFix.Value)
        {
            Log.LogInfo("Applying Tenzan Pass Chimera Encounters Restoration patch...");
            TenzanPassEncounterPatch.Initialize(harmony);
        }

        Log.LogInfo("PKTenzanPassPatch loaded successfully!");
    }
}
