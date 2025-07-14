using EFT;
using SAIN.Components;
using SAIN.Components.CoverFinder;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Mover;
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

        /// <summary>
        /// Toggles between walking and running to cover without interupting the coroutine
        /// </summary>
        /// <param name="value"></param>
        private void SetShallSprint(bool value)
        {
            CoverSeekingState = value ? ECoverSeekingState.RunningTo : ECoverSeekingState.WalkingTo;
        }

        private CoverPoint FindCoverPoint()
        {
            CoverPoints.Sort((x, y) => x.PathData.PathLength.CompareTo(y.PathData.PathLength));
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint coverPoint = CoverPoints[i];
                if (CheckMoveToCover(coverPoint, CoverSeekingState == ECoverSeekingState.RunningTo))
                {
                    return coverPoint;
                }
            }
            return null;
        }

        public void UpdateCover()
        {
            if (CoverInUse == null && CoverPoint_MovingTo == null)
            {
                CoverPoint_MovingTo = FindCoverPoint();
                if (CoverPoint_MovingTo == null)
                {
                    Bot.Mover.DogFight.DogFightMove(false, Bot.GoalEnemy);
                    return;
                }
            }

            if (Time.time - _timeLastUpdate > GetInterval()) return;

            if (CoverSeekingState == ECoverSeekingState.None)
            {
                Logger.LogDebug("cover status none");
                return;
            }
            if (CoverInUse != null)
            {
                if (CheckMoveToCover(CoverInUse, false))
                {
                    return;
                }
                CoverInUse = null;
            }
            if (CoverPoint_MovingTo != null)
            {
                if (!Bot.Mover.Moving)
                {
                    CoverPoint_MovingTo = null;
                }
                else
                {
                    var status = CoverPoint_MovingTo.StraightDistanceStatus;
                    if (status == CoverStatus.InCover)
                    {
                        CoverInUse = CoverPoint_MovingTo;
                        CoverPoint_MovingTo = null;
                    }
                    else if (status == CoverStatus.CloseToCover)
                    {
                        return;
                    }
                    else if (CheckMoveToCover(CoverPoint_MovingTo, CoverSeekingState == ECoverSeekingState.RunningTo))
                    {
                        return;
                    }
                    else
                    {
                        CoverPoint_MovingTo = null;
                    }
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
                return Bot.Mover.Moving ? 0.5f : 0.1f;
            }
            if (CoverInUse != null)
            {
                return 0.5f;
            }
            return 0.1f;
        }

        private float _timeLastUpdate;

        public void SetCoverSeekingState(ECoverSeekingState behavior)
        {
            switch (behavior)
            {
                case ECoverSeekingState.RunningTo:
                    SetShallSprint(true);
                    return;

                case ECoverSeekingState.WalkingTo:
                    SetShallSprint(false);
                    return;

                case ECoverSeekingState.None:
                    CoverSeekingState = ECoverSeekingState.None;
                    CoverInUse = null;
                    CoverPoint_MovingTo = null;
                    return;

                default:
                    Logger.LogError("invalid input state");
                    return;
            }
        }

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