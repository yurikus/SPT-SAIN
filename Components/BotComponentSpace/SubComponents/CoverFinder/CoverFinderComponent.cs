using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverFinderComponent : BotComponentBase
    {
        private const int COLLIDER_ARRAY_SIZE = 300;

        private const int TARGET_COVER_COUNT_AI = 4;
        private const int TARGET_COVER_COUNT_AI_PERF_MODE = 2;
        private const int TARGET_COVER_COUNT_HUMAN = 8;
        private const int TARGET_COVER_COUNT_HUMAN_PERF_MODE = 5;

        private const float UPDATE_TARGET_FREQUENCY = 0.25f;
        private const float SAMPLE_POINT_ORIGIN_RANGE = 0.5f;
        private const float SAMPLE_POINT_TARGET_RANGE = 0.5f;

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
        private const int COLLIDERS_TO_CHECK_PER_FRAME = 3;
        private const int COLLIDERS_TO_CHECK_PER_FRAME_NO_COVER = 5;

        public ECoverFinderStatus CurrentStatus { get; private set; }

        public TargetData TargetData
        {
            get
            {
                return _targetData;
            }
            private set
            {
                var oldEnemy = _targetData?.TargetEnemy;

                if (value == null)
                {
                    if (_targetData == null)
                        return;

                    subOrUnsub(false, oldEnemy);
                    _targetData = null;
                    return;
                }

                // we previously had no target, subscribe and assign the value
                var newEnemy = value.TargetEnemy;
                if (_targetData == null)
                {
                    _targetData = value;
                    subOrUnsub(true, newEnemy);
                    return;
                }

                // we have an old target, check if the new target is the same or not
                if (oldEnemy.IsDifferent(newEnemy))
                {
                    _targetData = value;
                    subOrUnsub(false, oldEnemy);
                    subOrUnsub(true, newEnemy);
                }
            }
        }

        public Vector3 OriginPoint
        {
            get
            {
                var data = TargetData;
                if (data == null)
                {
                    return Vector3.zero;
                }
                return data.BotPosition;
            }
        }

        public Vector3 TargetPoint
        {
            get
            {
                var data = TargetData;
                if (data == null)
                {
                    return Vector3.zero;
                }
                return data.TargetPosition;
            }
        }

        public BotComponent Bot { get; private set; }
        public List<CoverPoint> CoverPoints { get; } = new List<CoverPoint>();
        private CoverAnalyzer CoverAnalyzer { get; set; }
        private ColliderFinder ColliderFinder { get; set; }
        public bool ProcessingLimited { get; private set; }
        public CoverPoint FallBackPoint { get; private set; }
        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = new List<SpottedCoverPoint>();

        public void Init(BotComponent bot)
        {
            base.Init(bot.Person);
            Bot = bot;

            ColliderFinder = new ColliderFinder(this);
            CoverAnalyzer = new CoverAnalyzer(bot, this);

            bot.BotActivation.BotActiveToggle.OnToggle += botEnabled;
            bot.BotActivation.BotStandByToggle.OnToggle += botInStandBy;
            bot.OnDispose += botDisposed;
            bot.CurrentTarget.OnNewTargetEnemy += calcTargetPoint;
            bot.CurrentTarget.OnLoseTarget += clearTarget;
        }

        public void Update()
        {
            updateTarget();
            if (DebugCoverFinder)
            {
                if (CoverPoints.Count > 0)
                {
                    DebugGizmos.Line(CoverPoints.PickRandom().Position, Bot.Transform.HeadPosition, Color.yellow, 0.035f, true, 0.1f);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            StopLooking();
            StopAllCoroutines();
            if (Bot != null)
            {
                Bot.OnDispose -= botDisposed;
                Bot.BotActivation.BotActiveToggle.OnToggle -= botEnabled;
                Bot.BotActivation.BotStandByToggle.OnToggle -= botInStandBy;
                Bot.CurrentTarget.OnNewTargetEnemy -= calcTargetPoint;
                Bot.CurrentTarget.OnLoseTarget -= clearTarget;
            }
            Destroy(this);
        }

        private void updateTarget()
        {
            if (TargetData == null)
            {
                return;
            }
            var targetClass = Bot.CurrentTarget;
            Enemy targetEnemy = targetClass.CurrentTargetEnemy;
            Vector3? target = targetClass.CurrentTargetPosition;
            if (target == null || targetEnemy == null)
            {
                TargetData = null;
                return;
            }
            if (_updateTargetTime < Time.time)
            {
                calcTargetPoint(targetEnemy, target.Value);
            }
        }

        private void clearTarget()
        {
            TargetData = null;
        }

        private void calcTargetPoint(Enemy enemy, Vector3 target)
        {
            _updateTargetTime = Time.time + UPDATE_TARGET_FREQUENCY;

            if (NavMesh.SamplePosition(target, out var targetHit, SAMPLE_POINT_TARGET_RANGE, -1))
            {
                target = targetHit.position;
            }
            Vector3 botPosition = Bot.Position;
            if (NavMesh.SamplePosition(botPosition, out var botHit, SAMPLE_POINT_ORIGIN_RANGE, -1))
            {
                botPosition = botHit.position;
            }

            if (TargetData == null || TargetData.TargetEnemy.IsDifferent(enemy))
            {
                TargetData = new TargetData(enemy);
            }

            TargetData.Update(target, botPosition);
        }

        private int targetCoverCount(TargetData targetData)
        {
            int targetCount;
            bool isAI = targetData.TargetEnemy.IsAI;
            if (PerformanceMode)
                targetCount = isAI ? TARGET_COVER_COUNT_AI_PERF_MODE : TARGET_COVER_COUNT_HUMAN_PERF_MODE;
            else
                targetCount = isAI ? TARGET_COVER_COUNT_AI : TARGET_COVER_COUNT_HUMAN;
            return targetCount;
        }

        private void targetEnemyPosUpdated(Enemy enemy, EnemyPlace place)
        {
            if (place == null || enemy == null)
            {
                return;
            }
            calcTargetPoint(enemy, place.Position);
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
            //if (TargetData == null)
            //{
            //    Logger.LogError($"Cant start cover finder when target data is null!");
            //    return;
            //}
            if (_findCoverPointsCoroutine == null)
            {
                _findCoverPointsCoroutine = StartCoroutine(findCoverLoop());
            }
            if (_recheckCoverPointsCoroutine == null)
            {
                _recheckCoverPointsCoroutine = StartCoroutine(recheckCoverLoop());
            }
        }

        public void StopLooking()
        {
            if (_findCoverPointsCoroutine != null)
            {
                CurrentStatus = ECoverFinderStatus.None;
                StopCoroutine(_findCoverPointsCoroutine);
                _findCoverPointsCoroutine = null;

                StopCoroutine(_recheckCoverPointsCoroutine);
                _recheckCoverPointsCoroutine = null;

                CoverPoints.Clear();

                if (Bot != null)
                {
                    Bot.Cover.CoverInUse = null;
                }

                FallBackPoint = null;
                clearTarget();
            }
        }

        private IEnumerator recheckCoverPoints(List<CoverPoint> tempList, bool limit = true)
        {
            if (!havePositionsChanged(TargetData))
            {
                yield break;
            }

            bool shallLimit = limit && shallLimitProcessing();
            WaitForSeconds wait = shallLimit ? _recheckWait : null;

            ECoverFinderStatus lastStatus = CurrentStatus;
            CurrentStatus = shallLimit ? ECoverFinderStatus.RecheckingPointsWithLimit : ECoverFinderStatus.RecheckingPointsNoLimit;

            foreach (var coverPoint in tempList)
            {
                var data = TargetData;
                if (data != null && coverPoint != null)
                    yield return checkCoverPoint(coverPoint, data, wait);
            }

            CurrentStatus = lastStatus;
        }

        private IEnumerator checkCoverPoint(CoverPoint coverPoint, TargetData data, WaitForSeconds wait)
        {
            if (!PointStillGood(coverPoint, data, out bool updated, out _))
            {
                //Logger.LogWarning(reason);
                coverPoint.CoverData.IsBad = true;
            }
            if (updated)
                yield return wait;
        }

        private bool havePositionsChanged(TargetData targetData)
        {
            if (targetData == null)
            {
                return false;
            }

            float recheckThresh = PerformanceMode ? RECHECK_POSITION_CHANGE_PERF_MODE_SQR : RECHECK_POSITION_CHANGE_SQR;
            float targetDifference = (_lastRecheckTargetPosition - targetData.TargetPosition).sqrMagnitude;
            float botDifference = (_lastRecheckBotPosition - targetData.BotPosition).sqrMagnitude;

            if (targetDifference < recheckThresh &&
                botDifference < recheckThresh)
            {
                return false;
            }

            _lastRecheckTargetPosition = targetData.TargetPosition;
            _lastRecheckBotPosition = targetData.BotPosition;

            return true;
        }

        private bool shallLimitProcessing()
        {
            ProcessingLimited =
                Bot.Enemy?.IsAI == true ||
                limitProcessingFromDecision(Bot.Decision.CurrentCombatDecision);

            return ProcessingLimited;
        }

        private static bool limitProcessingFromDecision(ECombatDecision decision)
        {
            switch (decision)
            {
                case ECombatDecision.MoveToCover:
                case ECombatDecision.RunToCover:
                case ECombatDecision.Retreat:
                case ECombatDecision.RunAway:
                    return false;

                case ECombatDecision.HoldInCover:
                case ECombatDecision.Search:
                    return true;

                default:
                    return PerformanceMode;
            }
        }

        private bool colliderAlreadyUsed(Collider collider)
        {
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                if (collider == CoverPoints[i].Collider)
                {
                    return true;
                }
            }
            return false;
        }

        private bool filterColliderByName(Collider collider)
        {
            return collider != null &&
                _excludedColliderNames.Contains(collider.transform?.parent?.name);
        }

        private IEnumerator recheckCoverLoop()
        {
            WaitForSeconds wait = new(RECHECK_COVER_WAIT_FREQ);
            while (true)
            {
                clearSpotted();

                if (TargetData != null)
                {
                    _tempRecheckList.AddRange(CoverPoints);
                    yield return StartCoroutine(recheckCoverPoints(_tempRecheckList, false));
                    yield return StartCoroutine(clearAndSortPoints(_tempRecheckList));
                    _tempRecheckList.Clear();
                }
                yield return wait;
            }
        }

        private IEnumerator clearAndSortPoints(List<CoverPoint> tempList)
        {
            foreach (var point in tempList)
                if (point == null || point.CoverData.IsBad)
                    CoverPoints.Remove(point);

            OrderPointsByPathDist(CoverPoints);
            yield return null;
        }

        private bool needToFindCover(int coverCount, out int max)
        {
            max = 0;

            if (!isTargetValid(out TargetData targetData))
                return false;

            max = targetCoverCount(targetData);

            if (coverCount == 0)
                return true;

            if (coverCount < max / 2)
                return true;

            if (coverCount <= 1 &&
                coverCount < max)
                return true;

            if ((_lastPositionChecked - OriginPoint).sqrMagnitude >= FIND_COVER_DISTANCE_THRESHOLD_SQR)
                return true;

            return false;
        }

        private IEnumerator findCoverLoop()
        {
            WaitForSeconds wait = new(FIND_COVER_WAIT_FREQ);
            while (true)
            {
                int coverCount = CoverPoints.Count;
                if (needToFindCover(coverCount, out int max))
                {
                    CurrentStatus = ECoverFinderStatus.SearchingColliders;
                    _lastPositionChecked = OriginPoint;

                    bool debug = DebugCoverFinder;
                    Stopwatch fullStopWatch = debug ? Stopwatch.StartNew() : null;
                    Stopwatch findFirstPointStopWatch = coverCount == 0 && debug ? Stopwatch.StartNew() : null;

                    Collider[] colliders = _colliderArray;
                    yield return StartCoroutine(ColliderFinder.GetNewColliders(colliders));
                    yield return null;
                    ColliderFinder.SortArrayBotDist(colliders);
                    yield return null;
                    yield return StartCoroutine(findNewCoverPoints(colliders, ColliderFinder.HitCount, max, findFirstPointStopWatch));

                    coverCount = CoverPoints.Count;
                    sort(coverCount, CoverPoints);
                    log(coverCount, findFirstPointStopWatch, fullStopWatch);
                }
                CurrentStatus = ECoverFinderStatus.None;
                yield return wait;
            }
        }

        private void sort(int coverCount, List<CoverPoint> points)
        {
            if (coverCount == 0)
            {
                FallBackPoint = null;
                return;
            }
            if (coverCount < 2)
            {
                FallBackPoint = points.First();
                return;
            }
            FallBackPoint = FindFallbackPoint(points);
            OrderPointsByPathDist(points);
        }

        private void log(int coverCount, params Stopwatch[] watches)
        {
            foreach (var watch in watches)
                watch?.Stop();

            if (!DebugCoverFinder)
            {
                return;
            }

            if (_debugLogTimer < Time.time)
            {
                _debugLogTimer = Time.time + 1f;
                if (coverCount > 0)
                    Logger.LogInfo($"[{BotOwner.name}] - Found [{coverCount}] CoverPoints. Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
                else
                    Logger.LogWarning($"[{BotOwner.name}] - No Cover Found! Valid Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
            }
            if (_debugTimer2 < Time.time)
            {
                _debugTimer2 = Time.time + 5;
                //Logger.LogAndNotifyDebug($"Time to Complete Coverfinder Loop: [{b.ElapsedMilliseconds}ms]");
            }
        }

        private IEnumerator findNewCoverPoints(Collider[] colliders, int hits, int max, Stopwatch debugStopWatch)
        {
            _totalChecked = 0;
            int waitCount = 0;
            int coverCount = CoverPoints.Count;

            for (int i = 0; i < hits; i++)
            {
                if (coverCount >= max)
                    break;

                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                // Main Optimization, scales with the amount of points a bot currently has, and slows down the rate as it grows.
                if (coverCount >= COVERCOUNT_TO_START_DELAY)
                    yield return null;
                else if (coverCount > 0)
                {
                    // How long did it take to find at least 1 point?
                    endStopWatch(debugStopWatch);

                    if (waitCount >= COLLIDERS_TO_CHECK_PER_FRAME || shallLimitProcessing())
                    {
                        waitCount = 0;
                        yield return null;
                    }
                }
                else if (waitCount >= COLLIDERS_TO_CHECK_PER_FRAME_NO_COVER)
                {
                    waitCount = 0;
                    yield return null;
                }

                _totalChecked++;

                if (!isTargetValid(out TargetData data))
                    break;

                if (filterColliderByName(collider))
                    continue;
                if (colliderAlreadyUsed(collider))
                    continue;
                // The main Calculations
                if (CoverAnalyzer.CheckCollider(collider, data, out CoverPoint newPoint, out _))
                {
                    CoverPoints.Add(newPoint);
                    coverCount++;
                }

                waitCount++;
            }
        }

        private bool isTargetValid(out TargetData data)
        {
            data = TargetData;
            return data != null && data.TargetEnemy.WasValid;
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
            points.Sort((x, y) => x.PathData.RoundedPathLength.CompareTo(y.PathData.RoundedPathLength));
        }

        private CoverPoint FindFallbackPoint(List<CoverPoint> points)
        {
            points.Sort((x, y) => x.HardData.Height.CompareTo(y.HardData.Height));
            return points.Last();
        }

        public bool PointStillGood(CoverPoint coverPoint, TargetData targetData, out bool updated, out string reason)
        {
            updated = false;
            if (coverPoint.CoverData.IsBad)
            {
                reason = "badPoint";
                return false;
            }
            // if we are checking against the same enemy, and the delay for updating the coverpoint hasn't elapsed, this point is still good.
            if (!coverPoint.ShallUpdate(targetData.TargetProfileID))
            {
                reason = "notTimeToUpdate";
                return true;
            }
            if (PointIsSpotted(coverPoint))
            {
                reason = "spotted";
                return false;
            }

            updated = true;
            if (!CoverAnalyzer.RecheckCoverPoint(coverPoint, targetData, out reason))
            {
                return false;
            }
            return true;
        }

        private void subOrUnsub(bool value, Enemy enemy)
        {
            if (value)
            {
                enemy.Events.OnPositionUpdated += targetEnemyPosUpdated;
                return;
            }
            enemy.Events.OnPositionUpdated -= targetEnemyPosUpdated;
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
                Vector3 spottedPointPos = spottedPoint.CoverPoint.Position;
                if (spottedPoint.TooClose(spottedPointPos, point.Position))
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

        private readonly WaitForSeconds _recheckWait = new(RECHECK_COVER_WAIT_FOREACH_FREQ);
        private TargetData _targetData;
        private float _updateTargetTime;
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

        private static readonly List<string> _excludedColliderNames = new()
        {
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
        };

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
                float minHeight = CoverFinderComponent.CoverMinHeight;
                const float minX = 0.1f;
                const float minZ = 0.1f;

                Collider[] allColliders = new Collider[500000];
                int hits = Physics.OverlapSphereNonAlloc(Vector3.zero, 1000f, allColliders);

                int hitReduction = 0;
                for (int i = 0; i < hits; i++)
                {
                    Vector3 size = allColliders[i].bounds.size;
                    if (size.y < CoverFinderComponent.CoverMinHeight
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