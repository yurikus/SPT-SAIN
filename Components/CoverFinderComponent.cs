using EFT;
using SAIN.Components.BotControllerSpace.Classes.Raycasts;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components.CoverFinder
{
    public class CoverFinderComponent : BotComponentBase
    {
        private const int COLLIDER_ARRAY_SIZE = 300;

        private const int TARGET_COVER_COUNT_AI = 20;
        private const int TARGET_COVER_COUNT_AI_PERF_MODE = 4;
        private const int TARGET_COVER_COUNT_HUMAN = 20;
        private const int TARGET_COVER_COUNT_HUMAN_PERF_MODE = 6;

        private const float UPDATE_TARGET_FREQUENCY = 0.25f;
        private const float SAMPLE_POINT_ORIGIN_RANGE = 1f;
        private const float SAMPLE_POINT_TARGET_RANGE = 1.5f;

        private const float RECHECK_POSITION_CHANGE = 0.5f;
        private const float RECHECK_POSITION_CHANGE_SQR = RECHECK_POSITION_CHANGE * RECHECK_POSITION_CHANGE;
        private const float RECHECK_POSITION_CHANGE_PERF_MODE = 1f;
        private const float RECHECK_POSITION_CHANGE_PERF_MODE_SQR = RECHECK_POSITION_CHANGE_PERF_MODE * RECHECK_POSITION_CHANGE_PERF_MODE;

        private const float FIND_COVER_DISTANCE_THRESHOLD = 5f;
        private const float FIND_COVER_DISTANCE_THRESHOLD_SQR = FIND_COVER_DISTANCE_THRESHOLD * FIND_COVER_DISTANCE_THRESHOLD;

        private const float FIND_COVER_WAIT_FREQ = 0.1f;
        private const float RECHECK_COVER_WAIT_FREQ = 0.1f;
        private const float RECHECK_COVER_WAIT_FOREACH_FREQ = 0.05f;
        private const float CLEAR_SPOTTED_FREQ = 0.5f;

        private const int COVERCOUNT_TO_START_DELAY = 1;
        private const int COLLIDERS_TO_CHECK_PER_FRAME = 5;
        private const int COLLIDERS_TO_CHECK_PER_FRAME_NO_COVER = 8;

        public ECoverFinderStatus CurrentStatus { get; private set; }

        public BotComponent Bot { get; private set; }
        public List<CoverPoint> CoverPoints { get; } = [];
        private CoverAnalyzer CoverAnalyzer { get; set; }
        private ColliderFinder ColliderFinder { get; set; }
        public bool ProcessingLimited { get; private set; }
        public CoverPoint FallBackPoint { get; private set; }
        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = [];

        public void Init(BotComponent bot)
        {
            base.Init(bot.PlayerComponent, bot.BotOwner);
            Bot = bot;

            ColliderFinder = new ColliderFinder(this);
            CoverAnalyzer = new CoverAnalyzer(bot, this);

            bot.BotActivation.BotActiveToggle.OnToggle += botEnabled;
            bot.BotActivation.BotStandByToggle.OnToggle += botInStandBy;
            bot.OnDispose += botDisposed;
        }

        public void Update()
        {
            if (SAINPlugin.LoadedPreset.GlobalSettings.General.Cover.DebugCoverFinder &&
                CoverPoints.Count > 0)
            {
                DebugGizmos.DrawLine(CoverPoints.PickRandom().Position, Bot.Transform.EyePosition, Color.yellow, 0.05f, 0.1f);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            StopLooking();
            StopAllCoroutines();
            DisposeJobs();
            if (Bot != null)
            {
                Bot.OnDispose -= botDisposed;
                Bot.BotActivation.BotActiveToggle.OnToggle -= botEnabled;
                Bot.BotActivation.BotStandByToggle.OnToggle -= botInStandBy;
            }
            Destroy(this);
        }

        private void DisposeJobs()
        {
            _coverJobHandle.Complete();
            _coverJob.Dispose();
        }

        public Enemy TargetEnemy => Bot.GoalEnemy;

        private int targetCoverCount(bool isAI)
        {
            int targetCount;
            if (PerformanceMode)
                targetCount = isAI ? TARGET_COVER_COUNT_AI_PERF_MODE : TARGET_COVER_COUNT_HUMAN_PERF_MODE;
            else
                targetCount = isAI ? TARGET_COVER_COUNT_AI : TARGET_COVER_COUNT_HUMAN;
            return targetCount;
        }

        private void botInStandBy(bool value)
        {
            if (value)
            {
                ToggleCoverFinder(false);
            }
        }

        private void botEnabled(bool value)
        {
            if (!value)
                ToggleCoverFinder(false);
        }

        public void ToggleCoverFinder(bool value)
        {
            switch (value)
            {
                case true:
                    LookForCover();
                    break;

                case false:
                    StopLooking();
                    break;
            }
        }

        public void LookForCover()
        {
            _findCoverPointsCoroutine ??= StartCoroutine(FindCoverLoop());
            //if (_recheckCoverPointsCoroutine == null)
            //{
            //    _recheckCoverPointsCoroutine = StartCoroutine(RecheckCoverLoop());
            //}
        }

        public void StopLooking()
        {
            if (_findCoverPointsCoroutine != null)
            {
                CurrentStatus = ECoverFinderStatus.None;
                StopCoroutine(_findCoverPointsCoroutine);
                _findCoverPointsCoroutine = null;

                //StopCoroutine(_recheckCoverPointsCoroutine);
                //_recheckCoverPointsCoroutine = null;

                CoverPoints.Clear();

                FallBackPoint = null;
                DisposeJobs();
            }
        }

        private float _nextGetCollidersTime;
        private readonly SainBotCoverData CoverData = new();

        private IEnumerator FindCoverLoop()
        {
            WaitForSeconds wait = new(FIND_COVER_WAIT_FREQ);
            while (Bot != null)
            {
                Enemy enemy = TargetEnemy;
                if (enemy != null)
                {
                    //int max = targetCoverCount(enemy.IsAI);
                    int max = 5;
                    CurrentStatus = ECoverFinderStatus.SearchingColliders;

                    if (_nextGetCollidersTime < Time.time)
                    {
                        _nextGetCollidersTime = Time.time + 4;
                        SainBotCoverData.BotColliderQueryParams queryParams = new() {
                            origin = Bot.NavMeshPosition + Vector3.up * 0.25f,
                            halfExtents = new Vector3(35, 5, 35),
                            mask = LayerMaskClass.HighPolyWithTerrainNoGrassMask,
                            minColliderSize = new(0.25f, GlobalSettingsClass.Instance.General.Cover.CoverMinHeight, 0.25f),
                            maxColliderSize = new(50f, 50f, 50f)
                        };
                        CoverData.OverlapBoxAndFilter(queryParams);
                    }
                    CoverData.HandleLists(Bot.NavMeshPosition);

                    _totalChecked = 0;

                    CoverPoints.Clear();
                    CurrentStatus = ECoverFinderStatus.SearchingColliders;
                    for (int i = 0; i < CoverData.ValidCollidersList.Count; i++)
                    {
                        SainBotColliderData colliderData = CoverData.ValidCollidersList[i];
                        Collider collider = colliderData.Collider;
                        if (collider == null)
                        {
                            Logger.LogDebug("collider null");
                            continue;
                        }
                        _totalChecked++;
                        if (FilterColliderByName(collider)) continue;
                        enemy = TargetEnemy;
                        if (enemy == null || enemy.LastKnownPosition == null)
                        {
                            Logger.LogDebug("enemy null");
                            break;
                        }
                        Vector3 targetPosition = enemy.LastKnownPosition.Value;
                        Vector3 botPosition = Bot.NavMeshPosition;
                        Vector3 targetDirNormal = (targetPosition - botPosition).normalized;
                        if (colliderData.CoverPoint != null)
                        {
                            if (colliderData.CoverPoint.ShallUpdate(enemy.EnemyProfileId) && 
                                !CoverAnalyzer.RecheckCoverPoint(colliderData.CoverPoint, targetPosition, targetDirNormal, botPosition, out _))
                            {
                                colliderData.CoverPoint.CoverData.IsBad = true;
                            }
                            else
                            {
                                colliderData.CoverPoint.CoverData.IsBad = false;
                                CoverPoints.Add(colliderData.CoverPoint);
                            }
                        }
                        else if (CoverAnalyzer.CheckCreateNewCoverPoint(collider, targetPosition, botPosition, targetDirNormal, out CoverPoint newPoint, out _))
                        {
                            CoverPoints.Add(newPoint);
                            colliderData.CoverPoint = newPoint;
                            CoverData.ValidCollidersList[i] = colliderData;
                        }
                        if (CoverPoints.Count >= max) break;
                        yield return null;
                    }
                    sort(CoverPoints.Count, CoverPoints);
                    log(CoverPoints.Count);
                }
                CurrentStatus = ECoverFinderStatus.None;
                yield return wait;
            }
        }

        private bool HavePositionsChanged(Vector3 targetPosition, Vector3 botPosition)
        {
            float recheckThresh = PerformanceMode ? RECHECK_POSITION_CHANGE_PERF_MODE_SQR : RECHECK_POSITION_CHANGE_SQR;
            float targetDifference = (_lastRecheckTargetPosition - targetPosition).sqrMagnitude;
            float botDifference = (_lastRecheckBotPosition - botPosition).sqrMagnitude;
            if (targetDifference < recheckThresh && botDifference < recheckThresh)
            {
                return false;
            }
            _lastRecheckTargetPosition = targetPosition;
            _lastRecheckBotPosition = botPosition;
            return true;
        }

        private bool shallLimitProcessing()
        {
            ProcessingLimited =
                Bot.GoalEnemy?.IsAI == true ||
                limitProcessingFromDecision(Bot.Decision.CurrentCombatDecision);

            return ProcessingLimited;
        }

        private static bool limitProcessingFromDecision(ECombatDecision decision)
        {
            switch (decision)
            {
                case ECombatDecision.SeekCover:
                case ECombatDecision.Retreat:
                case ECombatDecision.RunAway:
                    return false;

                case ECombatDecision.Search:
                    return true;

                default:
                    return PerformanceMode;
            }
        }

        private static bool FilterColliderByName(Collider collider)
        {
            return collider != null &&
                _excludedColliderNames.Contains(collider.transform?.parent?.name);
        }

        private bool NeedToFindCover(int coverCount, out int max, out Enemy enemy)
        {
            max = 0;
            enemy = TargetEnemy;
            if (enemy == null)
                return false;
            max = targetCoverCount(enemy.IsAI);
            if (coverCount == 0)
                return true;
            if (coverCount < max / 2)
                return true;
            if (coverCount <= 1 && coverCount < max)
                return true;
            if ((_lastPositionChecked - Bot.NavMeshPosition).sqrMagnitude >= FIND_COVER_DISTANCE_THRESHOLD_SQR)
                return true;
            return false;
        }

        private IEnumerator CheckDistanceToAllColliders(Collider[] colliders, Enemy enemy)
        {
            if (colliders == null || colliders.Length == 0)
                yield break;

            ColliderCoverDataList.Clear();
            DirCalcData botToTargetData = new() {
                Point = enemy.LastKnownPosition.Value,
                Dir = enemy.EnemyDirection,
                DirNormal = enemy.EnemyDirectionNormal,
                Magnitude = enemy.KnownPlaces.BotDistanceFromLastKnown
            };
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null)
                {
                    ColliderCoverData coverData = new(i, collider, botToTargetData.Point, Bot.NavMeshPosition, botToTargetData);
                    ColliderCoverDataList.Add(coverData);
                }
            }
            int count = ColliderCoverDataList.Count;
            if (count > 0)
            {
                _coverJob = new CheckCoverJob {
                    Input = new NativeArray<ColliderCoverData>(count, Allocator.TempJob),
                    Output = new NativeArray<ColliderCoverData>(count, Allocator.TempJob),
                };
                for (int i = 0; i < count; i++)
                    _coverJob.Input[i] = ColliderCoverDataList[i];

                _coverJobHandle = _coverJob.Schedule(count, new JobHandle());
                yield return null;
                _coverJobHandle.Complete();

                // Retrieve the results from the job, assign members in cover data.
                var outputData = _coverJob.Output;
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine($"[{BotOwner?.name}] Completed Cover Job Count: [{count}]");
                for (int i = 0; i < count; i++)
                {
                    ColliderCoverData coverData = outputData[i];
                    GameWorldComponent.Instance.CoverManager.CreateCover(coverData.Collider);
                    stringBuilder.AppendLine($"[{i}:{coverData.Index}]:[{coverData.BotToCoverDirectionData.Magnitude}]:[{coverData.TargetToCoverDirectionData.Magnitude}]");
                    ColliderCoverDataList[i] = coverData;
                }
                Logger.LogDebug(stringBuilder.ToString());
                _coverJob.Dispose();
            }
        }

        private readonly List<ColliderCoverData> ColliderCoverDataList = [];

        private CheckCoverJob _coverJob;
        private JobHandle _coverJobHandle;

        private void sort(int coverCount, List<CoverPoint> points)
        {
            if (coverCount == 0)
            {
                FallBackPoint = null;
                return;
            }
            if (coverCount < 2)
            {
                FallBackPoint = points[0];
                return;
            }
            //FallBackPoint = FindFallbackPoint(points);
            OrderPointsByPathDist(points);
        }

        private void log(int coverCount)
        {
            if (!DebugCoverFinder)
            {
                return;
            }

            if (_debugLogTimer < Time.time)
            {
                _debugLogTimer = Time.time + 1f;
                if (coverCount > 0)
                    Logger.LogInfo($"[{BotOwner.name}] - Found [{coverCount}] CoverPoints. Colliders checked: [{_totalChecked}] Collider Array Size = [{CoverData.ValidCollidersList.Count}]");
                else
                    Logger.LogWarning($"[{BotOwner.name}] - No Cover Found! Valid Colliders checked: [{_totalChecked}] Collider Array Size = [{CoverData.ValidCollidersList.Count}]");
            }
            if (_debugTimer2 < Time.time)
            {
                _debugTimer2 = Time.time + 5;
                //Logger.LogAndNotifyDebug($"Time to Complete Coverfinder Loop: [{b.ElapsedMilliseconds}ms]");
            }
        }

        private Enemy UpdateEnemy(ref Vector3 targetPosition, ref Vector3 botPosition, ref Vector3 targetDirNormal)
        {
            Enemy enemy = TargetEnemy;
            if (enemy == null || enemy.LastKnownPosition == null) return null;
            targetPosition = enemy.LastKnownPosition.Value;
            botPosition = Bot.NavMeshPosition;
            targetDirNormal = (targetPosition - botPosition).normalized;
            return enemy;
        }

        private void endStopWatch(Stopwatch debugStopWatch)
        {
            if (debugStopWatch?.IsRunning == true)
            {
                debugStopWatch.Stop();
                if (_debugTimer < Time.time)
                {
                    _debugTimer = Time.time + 5;
                    Logger.LogAndNotifyDebug($"Time to Find First CoverPoint: [{debugStopWatch.ElapsedMilliseconds}ms]");
                }
            }
        }

        public static void OrderPointsByPathDist(List<CoverPoint> points)
        {
            points.Sort((x, y) => x.PathData.PathLength.CompareTo(y.PathData.PathLength));
        }

        public bool PointStillGood(CoverPoint coverPoint, Enemy enemy, Vector3 targetPosition, Vector3 botPosition, Vector3 targetDirectionNormal, out bool updated, out string reason)
        {
            updated = false;
            if (coverPoint.CoverData.IsBad)
            {
                reason = "badPoint";
                return false;
            }
            // if we are checking against the same enemy, and the delay for updating the coverpoint hasn't elapsed, this point is still good.
            if (!coverPoint.ShallUpdate(enemy.EnemyProfileId))
            {
                reason = "notTimeToUpdate";
                return true;
            }
            if (PointIsSpotted(coverPoint))
            {
                reason = "spotted";
                coverPoint.CoverData.IsBad = true;
                return false;
            }

            updated = true;
            if (!CoverAnalyzer.RecheckCoverPoint(coverPoint, targetPosition, targetDirectionNormal, botPosition, out reason))
            {
                coverPoint.CoverData.IsBad = true;
                return false;
            }
            return true;
        }

        private void clearSpotted()
        {
            if (_nextClearSpottedTime < Time.time)
            {
                _nextClearSpottedTime = Time.time + CLEAR_SPOTTED_FREQ;
                SpottedCoverPoints.RemoveAll(x => x.IsValidAgain);
            }
        }

        private bool PointIsSpotted(CoverPoint point)
        {
            if (point == null)
            {
                return true;
            }

            clearSpotted();

            foreach (var spottedPoint in SpottedCoverPoints)
            {
                if (spottedPoint.TooClose(point.Position))
                {
                    return true;
                }
            }
            if (point.Spotted)
            {
                SpottedCoverPoints.Add(new SpottedCoverPoint(point));
            }
            return point.Spotted;
        }

        public void OnDestroy()
        {
            StopLooking();
            StopAllCoroutines();
        }

        private void botDisposed()
        {
            Dispose();
        }

        private readonly Collider[] _colliderArray = new Collider[COLLIDER_ARRAY_SIZE];
        private Vector3 _lastPositionChecked = Vector3.zero;
        private Vector3 _lastRecheckTargetPosition;
        private Vector3 _lastRecheckBotPosition;
        private int _totalChecked;
        private float _debugLogTimer = 0f;
        private float _nextClearSpottedTime;
        private Coroutine _findCoverPointsCoroutine;
        private Coroutine _recheckCoverPointsCoroutine;
        private readonly List<CoverPoint> _tempRecheckList = new();

        public static bool PerformanceMode { get; private set; } = false;
        public static float CoverMinHeight { get; private set; } = 0.5f;
        public static float CoverMinEnemyDist { get; private set; } = 5f;
        public static float CoverMinEnemyDistSqr { get; private set; } = 25f;
        public static bool DebugCoverFinder { get; private set; } = false;

        private static bool AllCollidersAnalyzed;
        private static float _debugTimer;
        private static float _debugTimer2;

        private static readonly HashSet<string> _excludedColliderNames =
        [
            "metall_fence_2",
            "metallstolb",
            "stolb",
            "fonar_stolb",
            "fence_grid",
            "metall_fence_new",
            "ladder_platform",
            "frame_L",
            "frame_small_collider",
            "bump2x_p3_set4x",
            "bytovka_ladder",
            "sign",
            "sign17_lod",
            "ograda1",
            "ladder_metal"
        ];

        static CoverFinderComponent()
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings(SAINPresetClass.Instance);
        }

        private static void updateSettings(SAINPresetClass preset)
        {
            PerformanceMode = SAINPlugin.LoadedPreset.GlobalSettings.General.Performance.PerformanceMode;
            CoverMinHeight = SAINPlugin.LoadedPreset.GlobalSettings.General.Cover.CoverMinHeight;
            CoverMinEnemyDist = SAINPlugin.LoadedPreset.GlobalSettings.General.Cover.CoverMinEnemyDistance;
            CoverMinEnemyDistSqr = CoverMinEnemyDist * CoverMinEnemyDist;
            DebugCoverFinder = SAINPlugin.LoadedPreset.GlobalSettings.General.Cover.DebugCoverFinder;
        }

        private static void AnalyzeAllColliders()
        {
            if (!AllCollidersAnalyzed)
            {
                AllCollidersAnalyzed = true;
                float minHeight = CoverMinHeight;
                const float minX = 0.1f;
                const float minZ = 0.1f;

                Collider[] allColliders = new Collider[500000];
                int hits = Physics.OverlapSphereNonAlloc(Vector3.zero, 1000f, allColliders);

                int hitReduction = 0;
                for (int i = 0; i < hits; i++)
                {
                    Vector3 size = allColliders[i].bounds.size;
                    if (size.y < CoverMinHeight
                        || size.x < minX && size.z < minZ)
                    {
                        allColliders[i] = null;
                        hitReduction++;
                    }
                }
                Logger.LogError($"All Colliders Analyzed. [{hits - hitReduction}] are suitable out of [{hits}] colliders");
            }
        }
    }
}