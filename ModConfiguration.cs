using BepInEx.Configuration;

namespace PKTenzanPassPatch;

public sealed class ModConfiguration
{
    private readonly ConfigFile _config;

    // Suikoden 2 Tenzan Pass Chimera Encounters Restoration
    public ConfigEntry<bool> EnableTenzanChimeraFix { get; private set; }
    public ConfigEntry<bool> ForceTenzanChimeraEncounter { get; private set; }
    public ConfigEntry<int> TenzanChimeraCycloneDropRate { get; private set; }

    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {
        EnableTenzanChimeraFix = _config.Bind(
            "Suikoden 2 Tenzan Pass Chimera",
            "EnableTenzanChimeraFix",
            true,
            "Restores the missing Chimera monster encounters in Tenzan Pass (30% encounter rate, dropping the rare Cyclone Rune) as originally intended in the PS1 era."
        );

        ForceTenzanChimeraEncounter = _config.Bind(
            "Suikoden 2 Tenzan Pass Chimera",
            "ForceTenzanChimeraEncounter",
            false,
            "Debug/Testing option: Forces all random encounters in Tenzan Pass to be Chimera formations."
        );

        TenzanChimeraCycloneDropRate = _config.Bind(
            "Suikoden 2 Tenzan Pass Chimera",
            "TenzanChimeraCycloneDropRate",
            100,
            new ConfigDescription(
                "Drop rate percentage (1-100) for the Cyclone Rune when defeating a Chimera in Tenzan Pass. Default is 100% for guaranteed drop.",
                new AcceptableValueRange<int>(1, 100)
            )
        );
    }
}
