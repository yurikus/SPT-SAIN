using EFT;
using SAIN.Components;
using SAIN.Components.CoverFinder;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public enum CoverFinderState
    {
        off = 0,
        on = 1,
    }

    public enum ECoverSeekingState
    {
        None,
        NoCover,
        MoveTo,
        Shift,
        HoldInCover,
    }

    public class SAINCoverClass : BotComponentClassBase
    {
        public ECoverSeekingState CoverSeekingState { get; private set; }

        public event Action OnSpottedInCover;

        public CoverPoint CoverInUse { get; private set; }

        private float _spottedTime;
        public bool SpottedInCover => _spottedTime > Time.time;

        public bool HasCover => CoverInUse != null;
        public CoverFinderState CurrentCoverFinderState { get; private set; }
        public List<CoverPoint> CoverPoints => CoverFinder.CoverPoints;
        public CoverFinderComponent CoverFinder { get; private set; }
        public float LastHitInCoverTime { get; private set; }
        public float TimeSinceLastHitInCover => Time.time - LastHitInCoverTime;

        public SAINCoverClass(BotComponent bot) : base(bot)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            CoverFinder = bot.GetOrAddComponent<CoverFinderComponent>();
        }

        public override void Init()
        {
            CoverFinder.Init(Bot);
            base.Init();
        }

        public override void ManualUpdate()
        {
            base.ManualUpdate();
            bool active = Bot.HasEnemy;
            ActivateCoverFinder(active);
            if (active)
            {
                createDebug();
            }
        }

        private readonly List<CoverPoint> _coverPoints = [];
        private CoverPoint FindCoverPoint()
        {
            if (CoverPoints.Count == 0)
            {
                return null;
            }
            GetGoodCover(CoverPoints, _coverPoints, Bot.Transform.NavData.Position);
            if (_coverPoints.Count == 0)
            {
                return null;
            }

            _coverPoints.Sort((x, y) => x.DistanceToBot.CompareTo(y.DistanceToBot));
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint coverPoint = CoverPoints[i];
                if (coverPoint.PathDistanceStatus >= CoverStatus.CloseToCover &&
                    Bot.Mover.GoToCoverPoint(coverPoint, _shallSprint, Mover.ESprintUrgency.High))
                {
                    return coverPoint;
                }
            }


            if (CoverPoints.Count == 1)
            {
                CoverPoint coverPoint = CoverPoints[0];
                if (coverPoint != null && Bot.Mover.GoToCoverPoint(coverPoint, _shallSprint, Mover.ESprintUrgency.High))
                {
                    return coverPoint;
                }
                return null;
            }
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint coverPoint = CoverPoints[i];
                if (coverPoint == null || coverPoint.CoverData.IsBad)
                {
                    continue;
                }
                if (coverPoint.PathDistanceStatus >= CoverStatus.CloseToCover &&
                    Bot.Mover.GoToCoverPoint(coverPoint, _shallSprint, Mover.ESprintUrgency.High))
                {
                    return coverPoint;
                }
            }
            CoverPoints.Sort((x, y) => x.PathData.PathLength.CompareTo(y.PathData.PathLength));
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint coverPoint = CoverPoints[i];
                if (coverPoint == null)
                {
                    continue;
                }
                if (coverPoint.CoverData.IsBad)
                {
                    continue;
                }
                if (Bot.Mover.GoToCoverPoint(coverPoint, _shallSprint, Mover.ESprintUrgency.High))
                {
                    return coverPoint;
                }
            }
            CoverPoint lastResort = CoverPoints[0];
            if (lastResort != null && Bot.Mover.GoToCoverPoint(lastResort, _shallSprint, Mover.ESprintUrgency.High))
            {
                return lastResort;
            }
            Logger.LogWarning($"[{Bot.name}] No Cover Point found, even though there are {CoverPoints.Count} cover points available.");
            return null;
        }

        /// <summary>
        /// Filter coverpoints, only add good ones to the local list, and calc bot distance to them.
        /// </summary>
        private static void GetGoodCover(List<CoverPoint> coverPoints, List<CoverPoint> localList, Vector3 navPos)
        {
            localList.Clear();
            foreach (CoverPoint point in coverPoints)
            {
                if (point == null || point.CoverData.IsBad) continue;
                point.DistanceToBot = point.GetDistance(navPos);
                localList.Add(point);
            }
        }

        private bool CheckStartRun(Enemy enemy, out string reason)
        {
            if (CoverInUse != null)
            {
                reason = "inCover";
                return false;
            }
            if (!Player.MovementContext.CanSprint)
            {
                reason = "cantSprint";
                return false;
            }
            if (Bot.Cover.CoverPoints.Count == 0)
            {
                reason = "noCoverPoints";
                return false;
            }

            if (enemy.IsSniper && GlobalSettings.Mind.ENEMYSNIPER_ALWAYS_SPRINT_COVER)
            {
                reason = "EnemySniperRun";
                return true;
            }

            if (Bot.Decision.CurrentSelfDecision != ESelfActionType.None)
            {
                reason = "doing self operation";
                return true;
            }

            if (StartRunCoverTimer < Time.time)
            {
                reason = "timeToRun";
                return true;
            }

            reason = "dontRunYet";
            return false;
        }

        public bool SprintingToCover => _shallSprint && Bot.Mover.Moving;
        private bool _shallSprint;

        public void UpdateCover(Enemy enemy)
        {
            if (Time.time - _timeLastUpdate < GetInterval()) return;
            _timeLastUpdate = Time.time;

            _shallSprint = CheckStartRun(enemy, out _);

            if (CoverInUse != null)
            {
                if (!CoverInUse.Spotted && !CoverInUse.CoverData.IsBad)
                {
                    Bot.Mover.GoToCoverPoint(CoverInUse, false, Mover.ESprintUrgency.High);
                    _shallSprint = false;
                    SetCoverSeekingState(ECoverSeekingState.HoldInCover);
                    return;
                }
                CoverInUse = null;
            }

            if (CoverPoint_MovingTo != null)
            {
                if (_shallSprint)
                {
                    Bot.Mover.ActivePath?.RequestStartSprint(Mover.ESprintUrgency.High, "runToCover");
                }
                Bot.Mover.ActivePath?.RequestEndSprint(Mover.ESprintUrgency.High, "noRunToCover");

                var pathStatus = CoverPoint_MovingTo.PathDistanceStatus;
                var straightStatus = CoverPoint_MovingTo.StraightDistanceStatus;
                if (straightStatus == CoverStatus.InCover || pathStatus == CoverStatus.InCover)
                {
                    CoverInUse = CoverPoint_MovingTo;
                    CoverPoint_MovingTo = null;
                    _shallSprint = false;
                    SetCoverSeekingState(ECoverSeekingState.HoldInCover);
                    return;
                }
                else
                {
                    switch (pathStatus)
                    {
                        case CoverStatus.InCover:
                        case CoverStatus.CloseToCover:
                            break;

                        default:
                            if (CoverPoint_MovingTo.Spotted || CoverPoint_MovingTo.CoverData.IsBad)
                            {
                                CoverPoint_MovingTo = null;
                            }
                            break;
                    }
                }
                if (CoverPoint_MovingTo != null && Bot.Mover.GoToCoverPoint(CoverPoint_MovingTo, false, Mover.ESprintUrgency.High))
                {
                    SetCoverSeekingState(ECoverSeekingState.MoveTo);
                    return;
                }
            }

            if (CoverPoint_MovingTo == null)
            {
                CoverPoint_MovingTo = FindCoverPoint();
                if (CoverPoint_MovingTo == null)
                {
                    Bot.Mover.DogFight.DogFightMove(false, enemy);
                    SetCoverSeekingState(ECoverSeekingState.NoCover);
                    _shallSprint = false;
                }
                else
                {
                    SetCoverSeekingState(ECoverSeekingState.MoveTo);
                }
            }
        }

        private float GetInterval()
        {
            if (CoverInUse == null && CoverPoint_MovingTo == null)
            {
                return 0f;
            }
            if (CoverPoint_MovingTo != null)
            {
                return Bot.Mover.Moving ? 0.5f : 0f;
            }
            if (CoverInUse != null)
            {
                return 0.5f;
            }
            return 0.1f;
        }

        private float _timeLastUpdate;

        private void SetCoverSeekingState(ECoverSeekingState behavior)
        {
            if (CoverSeekingState == behavior) return;
            switch (behavior)
            {
                case ECoverSeekingState.Shift:
                    throw new NotImplementedException();

                case ECoverSeekingState.MoveTo:
                    OnStartMoveToCover();
                    break;

                case ECoverSeekingState.None:
                    CoverInUse = null;
                    CoverPoint_MovingTo = null;
                    _shallSprint = false;
                    return;

                default:
                    break;
            }
            CoverSeekingState = behavior;
        }

        public void StopSeekingCover()
        {
            SetCoverSeekingState(ECoverSeekingState.None);
        }

        private void OnStartMoveToCover()
        {
            StartRunCoverTimer = Time.time + RunToCoverTime * UnityEngine.Random.Range(RunToCoverTimeRandomMin, RunToCoverTimeRandomMax);
        }

        private const float RunToCoverTime = 1.5f;
        private const float RunToCoverTimeRandomMin = 0.66f;
        private const float RunToCoverTimeRandomMax = 1.33f;
        private float StartRunCoverTimer;

        public CoverPoint CoverPoint_MovingTo { get; private set; }

        private bool CheckMoveToCover(CoverPoint coverPoint, bool sprint)
        {
            if (coverPoint != null &&
                !coverPoint.CoverData.IsBad &&
                Bot.Mover.GoToCoverPoint(coverPoint, sprint, Mover.ESprintUrgency.High))
            {
                return true;
            }
            return false;
        }

        public override void Dispose()
        {
            try
            {
                CoverFinder?.Dispose();
            }
            catch { }
            if (debugCoverObject != null)
            {
                DebugGizmos.DestroyLabel(debugCoverObject);
                debugCoverObject = null;
            }
            base.Dispose();
        }

        public CoverPoint FindPointInDirection(Vector3 direction, float dotThreshold = 0.33f, float minDistance = 8f)
        {
            Vector3 botPosition = Bot.Position;
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint point = CoverPoints[i];
                if (point != null &&
                    !point.Spotted &&
                    !point.CoverData.IsBad)
                {
                    Vector3 coverPosition = point.Position;
                    Vector3 directionToPoint = botPosition - coverPosition;

                    if (directionToPoint.sqrMagnitude > minDistance * minDistance
                        && Vector3.Dot(directionToPoint.normalized, direction.normalized) > dotThreshold)
                    {
                        return point;
                    }
                }
            }
            return null;
        }

        private void createDebug()
        {
            if (SAINPlugin.DebugMode && CoverInUse != null)
            {
                DebugGizmos.DrawSphere(CoverInUse.Position, 0.1f, Color.cyan, 0.02f, $"[{Bot.name}] Cover In Use");
                DebugGizmos.DrawLine(CoverInUse.Position, Bot.Position + Vector3.up, Color.cyan, 0.075f, 0.02f, true);
            }
        }

        public void GetHit(DamageInfoStruct DamageInfoStruct, EBodyPart bodyPart, float floatVal)
        {
            if (CoverInUse != null)
            {
                bool wasSpotted = CoverInUse.Spotted;
                LastHitInCoverTime = Time.time;
                CoverInUse.GetHit(DamageInfoStruct, bodyPart, Bot.GoalEnemy);
                if (CoverInUse.Spotted && !wasSpotted)
                {
                    _spottedTime = Time.time + SpottedCoverPoint.SPOTTED_PERIOD;
                    OnSpottedInCover?.Invoke();
                }
            }
        }

        public void ActivateCoverFinder(bool value)
        {
            if (value)
            {
                CoverFinder?.LookForCover();
                CurrentCoverFinderState = CoverFinderState.on;
            }
            if (!value)
            {
                CoverFinder?.StopLooking();
                CurrentCoverFinderState = CoverFinderState.off;
            }
        }

        //public void CheckResetCoverInUse()
        //{
        //    if (CoverInUse != null && CoverInUse.CoverData.IsBad)
        //    {
        //        CoverInUse = null;
        //        return;
        //    }
        //
        //    ECombatDecision decision = Bot.Decision.CurrentCombatDecision;
        //    if (decision != ECombatDecision.MoveToCover
        //        && decision != ECombatDecision.RunToCover
        //        && decision != ECombatDecision.Retreat
        //        && decision != ECombatDecision.HoldInCover
        //        && decision != ECombatDecision.ShiftCover)
        //    {
        //        CoverInUse = null;
        //    }
        //}

        public void SortPointsByPathDist()
        {
            CoverFinderComponent.OrderPointsByPathDist(CoverPoints);
        }

        public bool TrySetProneConditional(Enemy enemy)
        {
            const float minCoverHeightToProne = 0.5f;
            var myMoveSettings = Bot.Info.FileSettings.Move;

            bool shallProne = myMoveSettings.PRONE_TOGGLE && GlobalSettingsClass.Instance.Move.PRONE_TOGGLE && Bot.Mover.Prone.ShallProneHide(enemy);
            if (shallProne && (Bot.Decision.CurrentSelfDecision != ESelfActionType.None || (myMoveSettings.PRONE_SUPPRESS_TOGGLE && Bot.Suppression.IsHeavySuppressed)))
            {
                Bot.Mover.Prone.SetProne(true);
                return true;
            }

            var point = CoverInUse;
            if (point != null && shallProne && point.Collider.bounds.size.y < minCoverHeightToProne)
            {
                Bot.Mover.Prone.SetProne(true);
                return true;
            }
            return false;
        }

        public bool CheckLimbsForCover(Enemy enemy)
        {
            var target = enemy.EnemyTransform.WeaponData.WeaponRoot;
            const float rayDistance = 3f;
            if (CheckLimbForCover(BodyPartType.leftLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.leftArm, target, rayDistance))
            {
                return true;
            }
            else if (CheckLimbForCover(BodyPartType.rightLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.rightArm, target, rayDistance))
            {
                return true;
            }
            return false;
        }

        private bool CheckLimbForCover(BodyPartType bodyPartType, Vector3 target, float dist = 2f)
        {
            var position = BotOwner.MainParts[bodyPartType].Position;
            Vector3 direction = target - position;
            return Physics.Raycast(position, direction, dist, LayerMaskClass.HighPolyWithTerrainMask);
        }

        private DebugLabel debugCoverObject;
    }
}