using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Linq;
using static SAIN.Helpers.EnumValues;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public class DefaultBrainsClass
    {
        static DefaultBrainsClass()
        {
            foreach (Layer layer in GetEnum<Layer>())
            {
                var layerInfo = new LayerInfoClass();

                Dictionary<Brain, int> usedByBrains = new();
                List<WildSpawnType> usedByWST = new();
                foreach (var brain in BrainInfos)
                {
                    var usedLayers = brain.Value.Layers;
                    if (usedLayers.ContainsKey(layer))
                    {
                        int priority = usedLayers[layer];
                        usedByBrains.Add(brain.Key, priority);

                        usedByWST.AddRange(from wildSpawn in brain.Value.UsedByWildSpawns
                                           where !usedByWST.Contains(wildSpawn)
                                           select wildSpawn);
                    }
                }

                layerInfo.UsedByBrains = usedByBrains;
                layerInfo.UsedByWildSpawns = usedByWST.ToArray();

                if (LayersNames.ContainsKey(layer))
                {
                    layerInfo.Name = LayersNames[layer];
                }
                else
                {
                    layerInfo.Name = layer.ToString();
                    layerInfo.ConvertedToString = true;
                }
                LayerInfos.Add(layer, layerInfo);
            }

            Save(BrainInfos, BrainInfosFile);
            Save(LayerInfos, LayerInfosFile);
            Save(LayersNames, LayersFile);
        }

        static bool Load<K, V>(string filename, out Dictionary<K, V> result)
        {
            return JsonUtility.Load.LoadObject(out result, filename, BigBrainFolder);
        }

        static void Save<K, V>(Dictionary<K, V> obj, string filename)
        {
            SaveObjectToJson(obj, filename, BigBrainFolder);
        }

        const string BrainInfosFile = "BrainInfos";
        const string LayerInfosFile = "LayerInfos";
        const string LayersFile = "LayersNames";
        const string BigBrainFolder = "BigBrain - DO NOT TOUCH";


        /*
        GClass219 BotBaseBrainClass     return "ArenaFighter";
        GClass220 BotBaseBrainClass     return "BossBully";
        GClass238 GClass237		        return "BossGluhar";
        BossKnightBrainClass GClass227	return "Knight";
        GClass247 GClass246		        return "BossKojaniy";
        GClass221 BotBaseBrainClass		return "BossSanitar";
        GClass222 BotBaseBrainClass		return "Tagilla";
        GClass223 BotBaseBrainClass		return "BossTest";
        GClass224 BotBaseBrainClass		return "BossZryachiy";
        GClass225 BotBaseBrainClass		return "CursAssault";
        GClass226 BotBaseBrainClass		return "Obdolbs";
        ExUsecBrainClass GClass227		return "ExUsec";
        GClass230 BotBaseBrainClass		return "BigPipe";
        GClass231 BotBaseBrainClass		return "BirdEye";
        GClass232 BotBaseBrainClass		return "FollowerBully";
        GClass239 GClass237		        return "FollowerGluharAssault";
        GClass240 GClass237		        return "FollowerGluharProtect";
        GClass241 GClass237		        return "FollowerGluharScout";
        BossKojaniyBrainClass GClass246	return "FollowerKojaniy";
        GClass233 BotBaseBrainClass		return "FollowerSanitar";
        GClass234 BotBaseBrainClass		return "TagillaFollower";
        GClass235 BotBaseBrainClass		return "Fl_Zraychiy";
        GClass236 BotBaseBrainClass		return "Gifter";
        GClass242 BotBaseBrainClass		return "Killa";
        GClass243 BotBaseBrainClass		return "Marksman";
        GClass244 BotBaseBrainClass		return "PMC";
        GClass251 GClass246		        return "SectantPriest";
        GClass250 GClass249		        return "SectantWarrior";
        GClass245 BotBaseBrainClass		return "Assault";
        */

        public static readonly Dictionary<Layer, string> LayersNames = new()
        {
            { Layer.Kojaniy_Target, "Kojaniy Target" },
            { Layer.Follower_bully, "Follower bully" },
            { Layer.AdvAssaultTarget, "AdvAssaultTarget" },
            { Layer.Simple_Target , "Simple Target"},
            { Layer.ObdolbosFight , "ObdolbosFight"},
            { Layer.Leave_Map , "Leave Map"},
            { Layer.Pursuit , "Pursuit"},
            { Layer.Assault_Building , "Assault Building"},
            { Layer.Enemy_Building , "Enemy Building"},
            { Layer.AssaultHaveEnemy , "AssaultHaveEnemy"},
            { Layer.Bully_Layer , "Bully Layer"},
            { Layer.Kill_logic , "Kill logic"},
            { Layer.Debug , "Debug"},
            { Layer.FollowPlayer , "Follow Player"},
            { Layer.Khorovod , "Khorovod"},
            { Layer.Obd_Patrol , "Obd Ptrl"},
            { Layer.BirdHold , "BirdHold"},
            { Layer.PtrlBirdEye , "PtrlBirdEye"},
            { Layer.KnightFight , "KnightFight"},
            { Layer.StationaryWS , "StationaryWS"},
            { Layer.ExURequest , "ExURequest"},
            { Layer.FRequest , "FRequest"},
            { Layer.PatrolFollower , "PatrolFollower"},
            { Layer.Help , "Help"},
            { Layer.Gifter , "Gifter"},
            { Layer.FlGlPrTarget , "FlGlPrTarget"},
            { Layer.GlGoal , "GlGoal"},
            { Layer.GrenadeDanger , "GrenadeDanger"},
            { Layer.HoldOrCover , "HoldOrCover"},
            { Layer.KojaniyB_Enemy , "KojaniyB_Enemy"},
            { Layer.FolKojEnemy , "FolKojEnemy"},
            { Layer.Malfunction , "Malfunction"},
            { Layer.MarksmanEnemy , "MarksmanEnemy"},
            { Layer.MarksmanTarget , "MarksmanTarget"},
            { Layer.Panic , "Panic"},
            { Layer.Utility_peace , "Utility peace"},
            { Layer.BossSanitarFight , "BossSanitarFight"},
            { Layer.FlSanFight , "FlSanFight"},
            { Layer.SanitarGoal , "SanitarGoal"},
            { Layer.GrenSuicide , "GrenSuicide"},
            { Layer.RH_IN , "R&H_IN"},
            { Layer.RH_OUT , "R&H_OUT"},
            { Layer.MeleeS_IN , "MeleeS_IN"},
            { Layer.RunandStrike , "Run&Strike"},
            { Layer.SupShootSect_IN , "SupShootSect_IN"},
            { Layer.SupShootSect_OUT , "SupShootSect_OUT"},
            { Layer.CheckZryachiy , "CheckZryachiy"},
            { Layer.FlZryachFight , "FlZryachFight"},
            { Layer.LayingPatrol , "LayingPatrol"},
            { Layer.CloseZrFight , "CloseZrFight"},
            { Layer.ZryachiyFight , "ZryachiyFight"},
            { Layer.TestLayer , "TestLayer"},
            { Layer.TagillaAmbush , "TagillaAmbush"},
            { Layer.TagillaFollower , "TagillaFollower"},
            { Layer.TagillaMain , "TagillaMain"},
            { Layer.BoarGrenadeDanger, "BoarGrenadeDanger" },
            { Layer.BossBoarFight, "BossBoarFight" },
            { Layer.BsBoarPatrol, "BsBoarPatrol" },
            { Layer.AvoidDanger, "AvoidDanger" },
            { Layer.Kln_NIMH, "Kln_NIMH" },
            { Layer.KlnSolo, "KlnSolo" },
            { Layer.KolontayFight, "KolontayFight" },
            { Layer.KlnTrg, "KlnTrg" },
            { Layer.BoarStationary, "BoarStationary" },
            { Layer.FBoarFght, "FBoarFght" },
            { Layer.BoarSnEn, "BoarSnEn" },
            { Layer.Boar_sn_tg, "Boar sn tg" },
            { Layer.KlnForceAtk, "KlnForceAtk" },
            { Layer.KolontayAP, "KolontayAP" },
            { Layer.SecurityKln, "SecurityKln" },
            { Layer.PrtFMN, "PrtFMN" },
            { Layer.PrtPst, "PrtPst" },
            { Layer.PrtZrSvg, "PrtZrSvg" },
            { Layer.PrtFight, "PrtFight" },
            { Layer.PrtBadTrg, "PrtBadTrg" },
            { Layer.PrtMany, "PrtMany" },
            { Layer.PrtStalk, "PrtStalk" },
            { Layer.PartisanMine, "PartisanMine" },
            { Layer.PartMineAll, "PartMineAll" },
            { Layer.HoldOrCoverT, "HoldOrCoverT" }
        };

        public static readonly Dictionary<Brain, BrainInfoClass> BrainInfos = new()
        {
            {
                Brain.Marksman, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.MarksmanEnemy, 30},
                        { Layer.MarksmanTarget, 20},
                        { Layer.StandBy, 3},
                        { Layer.PatrolAssault , 1},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.marksman,
                    ],
                }
            },
            {
                Brain.BossTest, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.TestLayer, 100},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossTest,
                    ],
                }
            },
            {
                Brain.BossBully, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Bully_Layer, 60},
                        { Layer.AssaultHaveEnemy , 50},
                        { Layer.AdvAssaultTarget , 9},
                        { Layer.PatrolAssault , 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerBully,
                    ],
                }
            },
            {
                Brain.FollowerBully, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 70},
                        { Layer.Follower_bully, 60 },
                        { Layer.AssaultHaveEnemy , 50},
                        { Layer.Request , 30},
                        { Layer.AdvAssaultTarget , 9},
                        { Layer.PatrolFollower , 2},
                        { Layer.PatrolAssault , 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerBully,
                    ],
                }
            },
            {
                Brain.Killa, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78 },
                        { Layer.Kill_logic, 60 },
                        { Layer.Simple_Target, 9 },
                        { Layer.PatrolAssault , 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossKilla,
                    ],
                }
            },
            {
                Brain.BossKojaniy, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction , 78 },
                        { Layer.KojaniyB_Enemy, 60 },
                        { Layer.Kojaniy_Target , 40 },
                        { Layer.StayAtPos, 11 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossKojaniy,
                    ],
                }
            },
            {
                Brain.FollowerKojaniy, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction , 78 },
                        { Layer.FolKojEnemy, 60},
                        { Layer.AssaultHaveEnemy, 50},
                        { Layer.Kojaniy_Target, 40},
                        { Layer.StayAtPos , 11},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerKojaniy,
                    ],
                }
            },
            {
                Brain.PMC, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        {Layer.FRequest, 70 },
                        {Layer.Pmc, 60 },
                        {Layer.AssaultHaveEnemy, 50 },
                        {Layer.Request, 30 },
                        {Layer.Pursuit, 25 },
                        {Layer.AdvAssaultTarget, 9 },
                        {Layer.Utility_peace, 3 },
                        {Layer.PatrolFollower, 2 },
                        {Layer.PatrolAssault, 0 }
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.pmcBot,
                        WildSpawnType.arenaFighterEvent,
                        WildSpawnType.assaultGroup,
                    ],
                }
            },
            {
                Brain.CursAssault, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Help, 65},
                        { Layer.GroupForce, 50 },
                        { Layer.Pursuit , 45},
                        { Layer.Request , 30},
                        { Layer.Leave_Map , 4},
                        { Layer.StandBy , 3},
                        { Layer.Utility_peace , 2},
                        { Layer.PatrolAssault , 1},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.cursedAssault,
                    ],
                }
            },
            {
                Brain.BossGluhar, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 90 },
                        { Layer.Malfunction, 88},
                        { Layer.BossGlFight, 65},
                        { Layer.SecurityGluhar , 60},
                        { Layer.AssaultHaveEnemy, 50 },
                        { Layer.AdvAssaultTarget , 9},
                        { Layer.PatrolAssault , 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossGluhar,
                    ],
                }
            },
            {
                Brain.FollowerGluharAssault, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 70},
                        { Layer.GluhAssKilla, 70},
                        { Layer.GluharKilla , 65},
                        { Layer.AssaultHaveEnemy , 55},
                        { Layer.HoldOrCover, 47},
                        { Layer.Request, 30},
                        { Layer.GlGoal, 14},
                        { Layer.Simple_Target , 9},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerGluharAssault,
                    ],
                }
            },
            {
                Brain.FollowerGluharProtect, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 70},
                        { Layer.SecurityGluhar, 60},
                        { Layer.AssaultHaveEnemy , 50},
                        { Layer.FlGlPrTarget , 46},
                        { Layer.Request, 30},
                        { Layer.Simple_Target , 9},
                        { Layer.PatrolFollower, 2},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerGluharSecurity,
                        WildSpawnType.followerGluharSnipe,
                    ],
                }
            },
            {
                Brain.FollowerGluharScout, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 70},
                        { Layer.FlGlScout , 65},
                        { Layer.AssaultHaveEnemy , 50},
                        { Layer.Request, 30},
                        { Layer.Simple_Target , 9},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerGluharScout,
                    ],
                }
            },
            {
                Brain.FollowerSanitar, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 70},
                        { Layer.FlSanFight, 62},
                        { Layer.AssaultHaveEnemy, 50},
                        { Layer.Request, 30},
                        { Layer.SanitarGoal , 22},
                        { Layer.Simple_Target, 9},
                        { Layer.PatrolFollower, 2},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerSanitar,
                    ],
                }
            },
            {
                Brain.BossSanitar, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.BossSanitarFight, 62},
                        { Layer.AssaultHaveEnemy, 50},
                        { Layer.SanitarGoal , 22},
                        { Layer.Simple_Target, 9},
                        { Layer.Utility_peace, 2},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossSanitar,
                    ],
                }
            },
            {
                Brain.BossBoar, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.BoarGrenadeDanger, 100 },
                        { Layer.Malfunction, 60},
                        { Layer.BossBoarFight, 50},
                        { Layer.BsBoarPatrol, 40},
                        { Layer.Simple_Target, 9},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.bossBoar,
                    },
                }
            },
            {
                Brain.FollowerBoar, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.BoarGrenadeDanger, 120 },
                        { Layer.ExURequest, 80},
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 74},
                        { Layer.BoarStationary, 70},
                        { Layer.FBoarFght, 50},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.followerBoar,
                    },
                }
            },
            {
                Brain.FollowerBoarClose1, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.BoarGrenadeDanger, 120 },
                        { Layer.ExURequest, 80},
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 74},
                        { Layer.BoarStationary, 70},
                        { Layer.FBoarFght, 50},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.followerBoarClose1,
                    },
                }
            },
            {
                Brain.FollowerBoarClose2, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.BoarGrenadeDanger, 120 },
                        { Layer.ExURequest, 80},
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 74},
                        { Layer.BoarStationary, 70},
                        { Layer.FBoarFght, 50},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.followerBoarClose2,
                    },
                }
            },
            {
                Brain.BossBoarSniper, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.AvoidDanger, 80 },
                        { Layer.BoarSnEn, 30},
                        { Layer.Boar_sn_tg, 20},
                        { Layer.PatrolAssault, 1},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.bossBoarSniper,
                    },
                }
            },
            {
                Brain.BossKolontay, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.AvoidDanger, 90 },
                        { Layer.Malfunction, 88 },
                        { Layer.Kln_NIMH, 77 },
                        { Layer.KlnSolo, 75 },
                        { Layer.KolontayFight, 65 },
                        { Layer.KlnTrg, 45 },
                        { Layer.PatrolAssault, 0 },
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.bossKolontay,
                    },
                }
            },
            {
                Brain.FollowerKolontayAssault, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.AvoidDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Kln_NIMH, 77},
                        { Layer.FRequest, 70},
                        { Layer.KlnForceAtk, 60},
                        { Layer.KolontayAP, 59},
                        { Layer.Utility_peace, 30},
                        { Layer.KlnTrg, 9},
                        { Layer.PatrolFollower, 2},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.followerKolontayAssault,
                    },
                }
            },
            {
                Brain.FollowerKolontaySecurity, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.AvoidDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Kln_NIMH, 77},
                        { Layer.FRequest, 70},
                        { Layer.SecurityKln, 60},
                        { Layer.Utility_peace, 30},
                        { Layer.KlnTrg, 9},
                        { Layer.PatrolFollower, 2},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.followerKolontaySecurity,
                    },
                }
            },
            {
                Brain.BossPartisan, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.AvoidDanger, 130 },
                        { Layer.Malfunction, 120 },
                        { Layer.PrtFMN, 110 },
                        { Layer.PrtPst, 100 },
                        { Layer.PrtZrSvg, 95 },
                        { Layer.PrtFight, 90 },
                        { Layer.PrtBadTrg, 85 },
                        { Layer.PrtMany, 80 },
                        { Layer.PrtStalk, 70 },
                        { Layer.PartisanMine, 60 },
                        { Layer.PartMineAll, 50 },
                        { Layer.HoldOrCoverT, 40 },
                        { Layer.StayAtPos, 11 },
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.bossPartisan,
                    },
                }
            },
            {
                Brain.SectantWarrior, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 200 },
                        { Layer.Malfunction, 198 },
                        { Layer.Kill_logic, 100 },
                        { Layer.RunandStrike, 90 },
                        { Layer.SupShootSect_IN, 80 },
                        { Layer.SupShootSect_OUT, 80 },
                        { Layer.MeleeS_IN, 70 },
                        { Layer.MeleeS_OUT, 70 },
                        { Layer.HoldOrCover, 47 },
                        { Layer.Utility_peace, 13 },
                        { Layer.Leave_Map, 12 },
                        { Layer.StayAtPosOpt, 11 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.sectantWarrior,
                    ],
                }
            },
            {
                Brain.SectantPriest, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenSuicide, 150 },
                        { Layer.GrenadeDanger, 130 },
                        { Layer.Malfunction, 128 },
                        { Layer.RH_OUT, 120 },
                        { Layer.HoldOrCover, 47 },
                        { Layer.Leave_Map, 12 },
                        { Layer.StayAtPos, 11 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.sectantPriest,
                    ],
                }
            },
            {
                Brain.Tagilla, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78 },
                        { Layer.FRequest, 70 },
                        { Layer.TagillaAmbush, 60 },
                        { Layer.TagillaMain, 50 },
                        { Layer.Request, 40 },
                        { Layer.Simple_Target, 9 },
                        { Layer.PatrolAssault, 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossTagilla,
                    ],
                }
            },
            {
                Brain.TagillaFollower, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78 },
                        { Layer.FRequest, 70 },
                        { Layer.TagillaFollower, 65 },
                        { Layer.TagillaAmbush, 60 },
                        { Layer.TagillaMain, 50 },
                        { Layer.Request, 40 },
                        { Layer.Simple_Target, 9 },
                        { Layer.PatrolAssault, 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerTagilla,
                    ],
                }
            },
            {
                Brain.ExUsec, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 100 },
                        { Layer.GroupForce, 95},
                        { Layer.ExURequest, 80},
                        { Layer.StationaryWS, 75},
                        { Layer.Malfunction , 73},
                        { Layer.FRequest , 70},
                        { Layer.Pmc, 60},
                        { Layer.AssaultHaveEnemy, 50},
                        { Layer.Request, 30},
                        { Layer.AdvAssaultTarget , 9},
                        { Layer.Utility_peace, 3},
                        { Layer.PatrolFollower, 2},
                        { Layer.PatrolAssault, 0},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.exUsec,
                    ],
                }
            },
            {
                Brain.Gifter, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Gifter, 70},
                        { Layer.FRequest, 65},
                        { Layer.AssaultHaveEnemy , 55},
                        { Layer.Request , 30},
                        { Layer.Simple_Target, 20},
                        { Layer.Leave_Map, 4},
                        { Layer.StandBy, 3},
                        { Layer.Utility_peace , 2},
                        { Layer.PatrolAssault, 1},
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.gifter,
                    ],
                }
            },
            {
                Brain.Knight, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Assault_Building, 72 },
                        { Layer.Enemy_Building, 70 },
                        { Layer.KnightFight, 62 },
                        { Layer.AssaultHaveEnemy, 50 },
                        { Layer.Request, 30},
                        { Layer.PtrlBirdEye, 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.bossKnight,
                    ],
                }
            },
            {
                Brain.BigPipe, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Assault_Building, 72 },
                        { Layer.Enemy_Building, 70 },
                        { Layer.Kill_logic, 60 },
                        { Layer.AssaultHaveEnemy, 50 },
                        { Layer.Request, 30},
                        { Layer.PtrlBirdEye, 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerBigPipe,
                    ],
                }
            },
            {
                Brain.BirdEye, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Assault_Building, 72 },
                        { Layer.Enemy_Building, 70 },
                        { Layer.BirdHold, 55 },
                        { Layer.BirdEyeFight, 50 },
                        { Layer.AssaultHaveEnemy, 50 },
                        { Layer.Request, 30},
                        { Layer.PtrlBirdEye, 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.followerBirdEye,
                    ],
                }
            },
            /*
            {
                Brain.BossZryachiy, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.CloseZrFight, 50},
                        { Layer.ZryachiyFight, 40 },
                        { Layer.CheckZryachiy, 30 },
                        { Layer.LayingPatrol, 0 },
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.bossZryachiy,
                    },
                }
            },
            {
                Brain.Fl_Zraychiy, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.CloseZrFight, 50},
                        { Layer.FlZryachFight, 40 },
                        { Layer.CheckZryachiy, 30 },
                        { Layer.LayingPatrol, 0 },
                    },
                    UsedByWildSpawns = new WildSpawnType[]
                    {
                        WildSpawnType.followerZryachiy,
                    },
                }
            },
            */
            {
                Brain.ArenaFighter, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.FRequest, 70 },
                        { Layer.Pmc, 60 },
                        { Layer.AssaultHaveEnemy, 50 },
                        { Layer.Request, 30 },
                        { Layer.AdvAssaultTarget, 9 },
                        { Layer.Utility_peace, 3 },
                        { Layer.PatrolFollower, 2 },
                        { Layer.PatrolAssault, 0 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.arenaFighter,
                    ],
                }
            },
            {
                Brain.Obdolbs, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.ObdolbosFight, 72 },
                        { Layer.Obd_Patrol, 10 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.crazyAssaultEvent,
                    ],
                }
            },
            {
                Brain.Assault, new BrainInfoClass()
                {
                    Layers = new Dictionary<Layer, int>
                    {
                        { Layer.GrenadeDanger, 80 },
                        { Layer.Malfunction, 78},
                        { Layer.Help, 70 },
                        { Layer.FRequest, 65 },
                        { Layer.AssaultHaveEnemy, 55 },
                        { Layer.Request, 30 },
                        { Layer.Pursuit, 25 },
                        { Layer.Simple_Target, 20 },
                        { Layer.Leave_Map, 4 },
                        { Layer.StandBy, 3 },
                        { Layer.Utility_peace, 2 },
                        { Layer.PatrolAssault, 1 },
                    },
                    UsedByWildSpawns =
                    [
                        WildSpawnType.assault,
                    ],
                }
            },
        };

        public static readonly Dictionary<Layer, LayerInfoClass> LayerInfos = new();
    }
}