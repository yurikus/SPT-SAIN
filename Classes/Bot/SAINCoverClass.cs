using EFT;
using SAIN.Components;
using SAIN.Components.CoverFinder;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
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
        None = 0,
        WalkingTo,
        RunningTo,
        HoldInCover,
    }

    public class SAINCoverClass : BotComponentClassBase
    {
        public ECoverSeekingState CoverSeekingState { get; private set; }

        public event Action OnSpottedInCover;

        public CoverPoint CoverInUse { get; private set; }

        private float _spottedTime;
        public bool SpottedInCover => _spottedTime > Time.time;

        public bool HasCover => CoverSeekingState == ECoverSeekingState.HoldInCover;
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
            bool active = Bot.SAINLayersActive;
            ActivateCoverFinder(active);
            if (active)
            {
                createDebug();
            }
        }

        /// <summary>
        /// Toggles between walking and running to cover without interupting the coroutine
        /// </summary>
        /// <param name="value"></param>
        private void SetShallSprint(bool value)
        {
            CoverSeekingState = value ? ECoverSeekingState.RunningTo : ECoverSeekingState.WalkingTo;
        }

        private IEnumerator SeekCover()
        {
            WaitForSeconds wait = new(1f / 30);
            WaitForSeconds holdInCoverWait = new(1 / 8f);
            while (CoverSeekingState != ECoverSeekingState.None)
            {
                CoverInUse = null;
                CoverPoint_MovingTo = null;

                CoverPoints.Sort((x, y) => x.PathData.TimeSinceLastUpdated.CompareTo(y.PathData.TimeSinceLastUpdated));
                _localCoverList.Clear();
                _localCoverList.AddRange(CoverPoints);
                Action<OperationResult> onComplete = new(PathComplete);
                for (int i = 0; i < _localCoverList.Count; i++)
                {
                    CoverPoint coverPoint = _localCoverList[i];
                    if (CheckMoveToCover(coverPoint, CoverSeekingState == ECoverSeekingState.RunningTo, onComplete))
                    {
                        CoverPoint_MovingTo = coverPoint;
                        Logger.LogDebug($"Found Cover [{coverPoint.HardData.Id}]");
                        break;
                    }
                    yield return null; // wait 1 frame between path checks
                }
                _localCoverList.Clear();
                if (CoverPoint_MovingTo == null)
                {
                    Bot.Mover.DogFight.DogFightMove(false, Bot.CurrentTarget.CurrentTargetEnemy);
                    yield return wait;
                    continue;
                }
                // We are in the process of moving to cover, wait and check to make sure the cover is good but otherwise hold
                while (CoverPoint_MovingTo != null && !CoverPoint_MovingTo.CoverData.IsBad && Bot.Mover.Moving)
                {
                    //Logger.LogDebug($"Movin");
                    yield return wait;
                }
                // We completed movement, if it was successful CoverInUse gets set OnComplete
                while (CoverInUse != null && !CoverInUse.CoverData.IsBad)
                {
                    //Logger.LogDebug($"Hold");
                    CheckMoveToCover(CoverInUse, false, null);
                    yield return holdInCoverWait;
                }
                yield return wait;
            }
        }

        private void PathComplete(OperationResult result)
        {
            if (_seekCoverCoroutine == null)
                return;
            if (result.Success)
            {
                CoverInUse = CoverPoint_MovingTo;
                CoverPoint_MovingTo = null;
                CoverSeekingState = ECoverSeekingState.HoldInCover;
                return;
            }
            CoverInUse = null;
            CoverPoint_MovingTo = null;
            Logger.LogDebug($"Fail : {result.Error}");
        }

        private readonly List<CoverPoint> _localCoverList = [];

        public void SetCoverSeekingState(ECoverSeekingState behavior)
        {
            switch (behavior)
            {
                case ECoverSeekingState.RunningTo:
                    _seekCoverCoroutine ??= Bot.StartCoroutine(SeekCover());
                    SetShallSprint(true);
                    return;

                case ECoverSeekingState.WalkingTo:
                    _seekCoverCoroutine ??= Bot.StartCoroutine(SeekCover());
                    SetShallSprint(false);
                    return;

                case ECoverSeekingState.None:
                    if (_seekCoverCoroutine != null)
                    {
                        Bot.StopCoroutine(_seekCoverCoroutine);
                        _seekCoverCoroutine = null;
                    }
                    CoverInUse = null;
                    CoverPoint_MovingTo = null;
                    return;

                default:
                    //Logger.LogError("invalid state");
                    return;
            }
        }

        private Coroutine _seekCoverCoroutine;

        public CoverPoint CoverPoint_MovingTo { get; private set; }

        private bool CheckMoveToCover(CoverPoint coverPoint, bool sprint, Action<OperationResult> onComplete)
        {
            if (coverPoint != null &&
                !coverPoint.CoverData.IsBad &&
                Bot.Mover.GoToCoverPoint(coverPoint, sprint, Mover.ESprintUrgency.High, false, onComplete))
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
                if (debugCoverObject == null)
                {
                    debugCoverObject = DebugGizmos.CreateLabel(CoverInUse.Position, "Cover In Use");
                    debugCoverLine = DebugGizmos.DrawLine(CoverInUse.Position, Bot.Position + Vector3.up, Color.cyan, 0.075f, -1, true);
                }
                debugCoverObject.WorldPos = CoverInUse.Position;
                DebugGizmos.UpdateLinePosition(CoverInUse.Position, Bot.Position + Vector3.up, debugCoverLine);
            }
            else if (debugCoverObject != null)
            {
                DebugGizmos.DestroyLabel(debugCoverObject);
                debugCoverObject = null;
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

        public bool DuckInCover(Enemy enemy)
        {
            const float minCoverHeightToProne = 0.5f;

            var point = CoverInUse;
            if (point != null)
            {
                var move = Bot.Mover;
                var prone = move.Prone;
                var myMoveSettings = Bot.Info.FileSettings.Move;
                var globalMoveSettings = GlobalSettingsClass.Instance.Move;

                bool shallProne = myMoveSettings.PRONE_TOGGLE && globalMoveSettings.PRONE_TOGGLE && prone.ShallProneHide(enemy);
                if (shallProne && (Bot.Decision.CurrentSelfDecision != ESelfDecision.None || (myMoveSettings.PRONE_SUPPRESS_TOGGLE && Bot.Suppression.IsHeavySuppressed)))
                {
                    prone.SetProne(true);
                    return true;
                }
                if (move.Pose.SetPoseToCover())
                {
                    return true;
                }
                if (shallProne &&
                    point.Collider.bounds.size.y < minCoverHeightToProne)
                {
                    prone.SetProne(true);
                    return true;
                }
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

        public bool BotIsAtCoverInUse()
        {
            return CoverInUse != null;
        }

        private DebugLabel debugCoverObject;
        private GameObject debugCoverLine;
    }
}