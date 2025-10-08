using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings.Categories;

public enum EBrain
{
    ArenaFighter,
    BossBully,
    BossGluhar,
    BossBoar,
    BossPartisan,
    Knight,
    BossKojaniy,
    BossSanitar,
    BossKolontay,
    Tagilla,
    BossTest,
    //BossZryachiy,
    Obdolbs,
    ExUsec,
    BigPipe,
    BirdEye,
    FollowerBully,
    FollowerGluharAssault,
    FollowerGluharProtect,
    FollowerGluharScout,
    FollowerKojaniy,
    FollowerSanitar,
    FollowerBoar,
    FollowerBoarClose1,
    FollowerBoarClose2,
    BossBoarSniper,
    FollowerKolontayAssault,
    FollowerKolontaySecurity,
    TagillaFollower,
    //Fl_Zraychiy,
    Gifter,
    Killa,
    Marksman,
    PMC,
    SectantPriest,
    SectantWarrior,
    CursAssault,
    Assault,
    PmcBear,
    PmcUsec,
    FlBoarCl,
    FlBoarSt,
    FlKlnAslt,
    KolonSec,
}

public static class AIBrains
{
    public static List<EBrain> GetAllowedScavBrains()
    {
        List<EBrain> ScavBrains = Scavs;

        // Needed for assaultGroup Scavs
        ScavBrains.Add(EBrain.PMC);

        return ScavBrains;
    }

    public static List<EBrain> GetAllowedPlayerScavBrains()
    {
        return GetAllowedPMCBrains();
    }

    public static List<EBrain> GetAllowedPMCBrains()
    {
        List<EBrain> PMCBrains = PMCs;

        if (BigBrainHandler.INCLUDE_RAIDER_BRAIN_FOR_PMCS)
        {
            PMCBrains.Add(EBrain.PMC);
        }

        return PMCBrains;
    }

    public static readonly List<EBrain> PMCs =
    [
        EBrain.PmcBear,
        EBrain.PmcUsec,
    ];

    public static readonly List<EBrain> Scavs =
    [
        EBrain.CursAssault,
        EBrain.Assault,
    ];

    public static readonly List<EBrain> Goons =
    [
        EBrain.Knight,
        EBrain.BirdEye,
        EBrain.BigPipe,
    ];

    public static readonly List<EBrain> Others =
    [
        EBrain.Obdolbs,
    ];

    public static readonly List<EBrain> Bosses =
    [
        EBrain.BossBully,
        EBrain.BossGluhar,
        EBrain.BossKojaniy,
        EBrain.BossSanitar,
        EBrain.Tagilla,
        EBrain.BossTest,
        //Brain.BossZryachiy,
        EBrain.Gifter,
        EBrain.Killa,
        EBrain.SectantPriest,
        EBrain.BossBoar,
        EBrain.BossKolontay,
        EBrain.BossPartisan
    ];

    public static readonly List<EBrain> Followers =
    [
        EBrain.FollowerBully,
        EBrain.FollowerGluharAssault,
        EBrain.FollowerGluharProtect,
        EBrain.FollowerGluharScout,
        EBrain.FollowerKojaniy,
        EBrain.FollowerSanitar,
        EBrain.TagillaFollower,
        //Brain.Fl_Zraychiy,
        EBrain.FollowerBoar,
        EBrain.FollowerBoarClose1,
        EBrain.FollowerBoarClose2,
        EBrain.BossBoarSniper,
        EBrain.FollowerKolontayAssault,
        EBrain.FollowerKolontaySecurity,
        EBrain.FlBoarCl,
        EBrain.FlBoarSt,
    ];
}