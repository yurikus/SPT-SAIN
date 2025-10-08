using System.Collections.Generic;
using System.Linq;

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
    public static IReadOnlyCollection<string> AllowedPlayerScavBrains
    {
        get
        {
            return AllowedPMCBrains;
        }
    }

    public static IReadOnlyCollection<string> AllowedPMCBrains
    {
        get
        {
            if (_allowedPMCBrains == null)
            {
                List<EBrain> brains = [.. PMCs];
                if (BigBrainHandler.INCLUDE_RAIDER_BRAIN_FOR_PMCS)
                {
                    brains.Add(EBrain.PMC);
                }
                _allowedPMCBrains = brains.ConvertAll(brain => brain.ToString())
                    .AsReadOnly();
            }
            return _allowedPMCBrains;
        }
    }

    private static IReadOnlyCollection<string> _allowedPMCBrains;

    public static IReadOnlyCollection<string> AllowedScavBrains
    {
        get
        {
            if (_allowedScavBrains == null)
            {
                // PMC brain is needed for assaultGroup scavs
                List<EBrain> brains = [EBrain.PMC, .. Scavs];
                _allowedScavBrains = brains.ConvertAll(brain => brain.ToString())
                    .AsReadOnly();
            }
            return _allowedScavBrains;
        }
    }

    private static IReadOnlyCollection<string> _allowedScavBrains;

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