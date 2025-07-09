using EFT;
using SAIN.Components;
using SAIN.Components.BotComponentSpace.Classes.EnemyClasses;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UIElements.UIR.Implementation.UIRStylePainter;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public enum EEnemyTag
    {
        EnemyKnown,
        CurrentEnemy,
        ShallDogFight,
    }

    public class Enemy : BotBase
    {
        public override void ManualUpdate()
        {
            IsCurrentEnemy = Bot.CurrentTarget.CurrentTargetEnemy == this;

            CalcFrequencyCoef();
            UpdateDistAndDirection();
            UpdateSniperStatus();

            KnownChecker.ManualUpdate();
            ActiveThreatChecker.ManualUpdate();
            UpdateActiveState();
            Vision.ManualUpdate();
            KnownPlaces.ManualUpdate();
            Path.ManualUpdate();
            Status.ManualUpdate();

            if (IsCurrentEnemy && GetVisibilePathPoint(out Vector3 point))
            {
                DebugGizmos.Sphere(point, 0.06f, Color.red, 0.02f);
                DebugGizmos.Line(point, Bot.Transform.HeadPosition, Color.red, 0.015f, 0.02f);
            }

            base.ManualUpdate();
        }

        public event Action OnEnemyDisposed;

        public string EnemyName { get; }
        public string EnemyProfileId { get; }
        public PlayerComponent EnemyPlayerComponent { get; }
        public PersonClass EnemyPerson { get; }
        public IPlayer EnemyIPlayer { get; private set; }
        public Player EnemyPlayer { get; private set; }
        public PersonTransformClass EnemyTransform { get; }
        public OtherPlayerData EnemyPlayerData { get; }
        public bool IsAI => EnemyPlayer.IsAI;
        public bool IsZombie => EnemyPlayer.UsedSimplifiedSkeleton;

        public EnemyEvents Events { get; }
        public EnemyKnownPlaces KnownPlaces { get; private set; }
        public SAINEnemyStatus Status { get; }
        public EnemyVisionClass Vision { get; }
        public SAINEnemyPath Path { get; }
        public EnemyInfo EnemyInfo { get; }
        public EnemyAim Aim { get; }
        public EnemyHearing Hearing { get; }

        public bool IsCurrentEnemy { get; private set; }
        public float RealDistance => EnemyPlayerData.DistanceData.Distance;
        public bool IsSniper { get; private set; }
        public Vector3? VisiblePathPoint { get; private set; }
        public float VisiblePathPointDistanceToBot { get; private set; }
        public float VisiblePathPointDistanceToEnemyLastKnown { get; private set; }
        public int? VisiblePathCornerIndex { get; private set; }
        public float? VisiblePathPointSignedAngle { get; private set; }

        public Vector3? SuppressionTarget {
            get
            {
                if (!GlobalSettingsClass.Instance.Mind.TARGET_SUPPRESS_TOGGLE)
                {
                    return null;
                }
                Vector3? enemyLastKnown = KnownPlaces.LastKnownPosition;
                if (enemyLastKnown == null)
                {
                    return null;
                }
                if (GetVisibilePathPoint(out Vector3 point) && IsTargetInSuppRange(enemyLastKnown.Value, point))
                {
                    return point;
                }
                return null;
            }
        }

        public bool EnemyKnown => Events.OnEnemyKnownChanged.Value;
        public bool EnemyNotLooking => IsVisible && !Status.EnemyLookingAtMe && !Status.ShotAtMeRecently;
        public bool WasValid => ValidChecker.WasValid;

        public Vector3? CenterMass {
            get
            {
                if (EnemyIPlayer == null)
                {
                    return null;
                }
                if (_nextGetCenterTime < Time.time)
                {
                    _nextGetCenterTime = Time.time + 0.05f;
                    _centerMass = new Vector3?(FindCenterMass());
                }
                return _centerMass;
            }
        }

        public HashSet<EEnemyTag> Tags { get; } = [];

        public bool FirstContactOccured => Vision.FirstContactOccured;

        public bool FirstContactReported { get; set; }

        public EPathDistance EPathDistance => Path.EPathDistance;
        public Vector3? LastKnownPosition => KnownPlaces.LastKnownPosition;

        public Vector3 EnemyMoveDirection {
            get
            {
                if (_nextCalcMoveDirTime < Time.time)
                {
                    _nextCalcMoveDirTime = Time.time + 0.1f;
                    Vector2 moveDirV2 = EnemyPlayer.MovementContext.MovementDirection;
                    Vector3 moveDirection = new(moveDirV2.x, 0, moveDirV2.y);
                    if (EnemyTransform.VelocityMagnitudeNormal > 0.01f)
                    {
                        LastMoveDirection = moveDirection;
                        if (EnemyPlayer.IsSprintEnabled)
                        {
                            LastSprintDirection = moveDirection;
                        }
                    }
                    _moveDirection = moveDirection;
                }
                return _moveDirection;
            }
        }

        public Vector3 LastMoveDirection { get; private set; }
        public Vector3 LastSprintDirection { get; private set; }
        public Vector3 EnemyPosition => EnemyTransform.Position;
        public Vector3 EnemyDirection => EnemyPlayerData.DistanceData.Direction;
        public Vector3 EnemyDirectionNormal => EnemyPlayerData.DistanceData.DirectionNormal;
        public Vector3 EnemyHeadPosition => EnemyTransform.HeadPosition;

        public float TimeSinceLastKnownUpdated => KnownPlaces.TimeSinceLastKnownUpdated;
        public bool InLineOfSight => Vision.InLineOfSight;
        public bool IsVisible => Vision.IsVisible;
        public bool CanShoot => Vision.CanShoot;
        public bool Seen => Vision.Seen;
        public bool Heard => Hearing.Heard;

        public bool EnemyLookingAtMe => Status.EnemyLookingAtMe;
        public float TimeSinceSeen => Vision.TimeSinceSeen;
        public float TimeSinceHeard => Hearing.TimeSinceHeard;
        public float UpdateFrequencyCoef { get; private set; }
        public float UpdateFrequencyCoefNormal { get; private set; }

        public float TimeSinceCurrentEnemy => _hasBeenActive ? Time.time - _timeLastActive : float.MaxValue;

        private const float ENEMY_UPDATEFREQUENCY_MAX_SCALE = 5f;
        private const float ENEMY_UPDATEFREQUENCY_MAX_DIST = 500f;
        private const float ENEMY_UPDATEFREQUENCY_MIN_DIST = 50f;
        private const float SQUADREPORT_SIGHT_INTERVAL = 0.5f;

        public Enemy(BotComponent bot, PlayerComponent enemyComponent, EnemyInfo enemyInfo) : base(bot)
        {
            EnemyPlayerComponent = enemyComponent;
            EnemyIPlayer = enemyComponent.IPlayer;
            EnemyPlayer = enemyComponent.Player;
            EnemyPerson = enemyComponent.Person;
            EnemyTransform = enemyComponent.Transform;
            EnemyName = $"{enemyComponent.Name} ({enemyComponent.Person.Nickname})";
            EnemyInfo = enemyInfo;
            EnemyProfileId = enemyComponent.ProfileId;

            EnemyPlayerData = bot.PlayerComponent.OtherPlayersData.DataDictionary[enemyComponent.ProfileId];

            Events = new EnemyEvents(this);
            Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
            ActiveThreatChecker = new EnemyActiveThreatChecker(this);
            ValidChecker = new EnemyValidChecker(this);
            KnownChecker = new EnemyKnownChecker(this);
            Status = new SAINEnemyStatus(this);
            Vision = new EnemyVisionClass(this);
            Path = new SAINEnemyPath(this);
            KnownPlaces = new EnemyKnownPlaces(this);
            Aim = new EnemyAim(this);
            Hearing = new EnemyHearing(this);

            UpdateDistAndDirection();
        }

        public override void Init()
        {
            Events.Init();
            ValidChecker.Init();
            KnownChecker.Init();
            ActiveThreatChecker.Init();
            KnownPlaces.Init();
            Vision.Init();
            Path.Init();
            Hearing.Init();
            Status.Init();
            base.Init();
        }

        public override void Dispose()
        {
            OnEnemyDisposed?.Invoke();
            Events.Dispose();
            Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
            ValidChecker.Dispose();
            KnownChecker.Dispose();
            ActiveThreatChecker.Dispose();
            KnownPlaces.Dispose();
            Vision.Dispose();
            Path.Dispose();
            Hearing.Dispose();
            Status.Dispose();
            base.Dispose();
        }

        private void OnEnemyKnownChanged(bool value, Enemy enemy)
        {
            if (!value)
            {
                ClearVisiblePathPoint();
                IsSniper = false;
                FirstContactReported = false;
                IsCurrentEnemy = false;
            }
        }

        private void UpdateVisiblePathPointDist(Vector3 headPosition)
        {
            TryUpdateVisPathDist(headPosition, VisiblePathPoint, LastKnownPosition, out float distToBot, out float distToEnemy);
            VisiblePathPointDistanceToBot = distToBot;
            VisiblePathPointDistanceToEnemyLastKnown = distToEnemy;
        }

        private static void TryUpdateVisPathDist(Vector3 headPosition, Vector3? visPathPoint, Vector3? lastKnown, out float distToBot, out float distToEnemy)
        {
            distToBot = float.MaxValue;
            distToEnemy = float.MaxValue;
            if (visPathPoint == null)
            {
                return;
            }
            if (lastKnown == null)
            {
                return;
            }
            distToBot = (visPathPoint.Value - headPosition).magnitude;
            distToEnemy = (visPathPoint.Value - lastKnown.Value).magnitude;
        }

        public void SetIsCurrentEnemy(bool value)
        {
            IsCurrentEnemy = value;
        }

        public bool GetVisibilePathPoint(out Vector3 pathPoint)
        {
            if (VisiblePathPoint != null)
            {
                pathPoint = VisiblePathPoint.Value;
                if (_visPathPointIsCorner)
                {
                    pathPoint += Bot.Steering.WeaponRootOffset;
                }
                return true;
            }
            pathPoint = Vector3.zero;
            return false;
        }

        public bool AddTag(EEnemyTag tag) => Tags.Add(tag);

        public bool RemoveTag(EEnemyTag tag) => Tags.Remove(tag);

        public bool HasTag(EEnemyTag tag) => Tags.Contains(tag);

        public void ClearVisiblePathPoint()
        {
            _visPathPointIsCorner = false;
            VisiblePathPoint = null;
            VisiblePathPointSignedAngle = null;
            VisiblePathCornerIndex = null;
            VisiblePathPointDistanceToBot = float.MaxValue;
            VisiblePathPointDistanceToEnemyLastKnown = float.MaxValue;
        }

        public void SetLastVisiblePathPoint(Vector3 Point, int CornerIndex)
        {
            _visPathPointIsCorner = false;
            VisiblePathPoint = Point;
            VisiblePathCornerIndex = CornerIndex;
            Vector3? LastKnown = LastKnownPosition;
            UpdateVisiblePathPointDist(Bot.Transform.EyePosition);
            if (LastKnown != null)
            {
                Vector3 botPosition = Bot.Position;
                Vector3 botEyePosition = Bot.Transform.EyePosition;
                botPosition.y = botEyePosition.y;
                VisiblePathPointSignedAngle = Vector.FindFlatSignedAngle(Point, LastKnown.Value, botPosition);
            }
        }

        public void SetLastCornerAsVisiblePathPoint(Vector3 LastCorner, int CornerIndex)
        {
            _visPathPointIsCorner = true;
            VisiblePathPoint = LastCorner;
            VisiblePathCornerIndex = CornerIndex;
            VisiblePathPointSignedAngle = null;
            UpdateVisiblePathPointDist(Bot.Transform.EyePosition);
        }

        public bool FindLookPoint(out Vector3 Position, out EEnemySteerDir EnemySteerDir)
        {
            if (IsVisible)
            {
                EnemySteerDir = EEnemySteerDir.VisibleEnemyPos;
                Position = EnemyPosition + Bot.Steering.WeaponRootOffset;
                return true;
            }
            if (GetVisibilePathPoint(out Position))
            {
                EnemySteerDir = EEnemySteerDir.PathNode;
                return true;
            }
            EnemyKnownPlaces Places = KnownPlaces;
            EnemyPlace lastKnown = Places.LastKnownPlace;
            if (lastKnown == null)
            {
                EnemySteerDir = EEnemySteerDir.NullLastKnown_ERROR;
                return false;
            }
            var lastSeen = Places.LastSeenPlace;
            if (lastSeen != null &&
                (lastSeen == lastKnown || (lastSeen.Position - lastKnown.Position).sqrMagnitude < GlobalSettingsClass.Instance.Steering.STEER_LASTSEEN_TO_LASTKNOWN_DISTANCE.Sqr()))
            {
                EnemySteerDir = EEnemySteerDir.LastSeenPos;
                Position = lastSeen.Position + Bot.Steering.WeaponRootOffset;
                return true;
            }

            EnemySteerDir = EEnemySteerDir.LastKnownPos;
            Position = lastKnown.Position + Bot.Steering.WeaponRootOffset;
            return true;
        }

        public bool CheckValid()
        {
            return ValidChecker.CheckValid();
        }

        private bool IsTargetInSuppRange(Vector3 target, Vector3 suppressPoint)
        {
            var settings = GlobalSettingsClass.Instance.Mind;

            float distSqr = (target - suppressPoint).sqrMagnitude;
            if (distSqr <= settings.TARGET_SUPPRESS_DIST.Sqr())
            {
                return true;
            }
            if (distSqr > settings.TARGET_SUPPRESS_DIST_MAX.Sqr())
            {
                return false;
            }
            Vector3 directionToSuppPoint = suppressPoint - Bot.Position;
            Vector3 directionToTarget = target - Bot.Position;
            float angle = Vector3.Angle(directionToSuppPoint.normalized, directionToTarget.normalized);
            if (angle < settings.MAX_TARGET_SUPPRESS_ANGLE.Sqr())
            {
                return true;
            }
            return false;
        }

        private void UpdateDistAndDirection()
        {
            try
            {
                EnemyInfo.Direction = EnemyDirection;
                EnemyInfo.Distance = RealDistance;
            }
            catch
            { // EFT code loves throwing random errors
            }
        }

        private void UpdateSniperStatus()
        {
            if (IsSniper)
            {
                if (!EnemyKnown)
                {
                    IsSniper = false;
                    return;
                }
                IsSniper = RealDistance < GlobalSettings.Mind.ENEMYSNIPER_DISTANCE_END;
            }
        }

        private void CalcFrequencyCoef()
        {
            if (_nextUpdateCoefTime < Time.time)
            {
                _nextUpdateCoefTime = Time.time + 0.1f;
                UpdateFrequencyCoef = CalcUpdateFrequencyCoef(out float normal);
                UpdateFrequencyCoefNormal = normal;
            }
        }

        private float CalcUpdateFrequencyCoef(out float normal)
        {
            float enemyDist = RealDistance;
            float min = ENEMY_UPDATEFREQUENCY_MIN_DIST;
            if (enemyDist <= min)
            {
                normal = 0;
                return 1f;
            }
            float max = ENEMY_UPDATEFREQUENCY_MAX_DIST;
            if (enemyDist >= max)
            {
                normal = 1f;
                return max;
            }

            float num = max - min;
            float num2 = enemyDist - min;
            normal = num2 / num;
            float result = Mathf.Lerp(1f, ENEMY_UPDATEFREQUENCY_MAX_SCALE, normal);
            return result;
        }

        private void UpdateActiveState()
        {
            if (IsCurrentEnemy &&
                !_hasBeenActive)
            {
                _hasBeenActive = true;
            }

            if (IsCurrentEnemy || IsVisible || Status.HeardRecently)
            {
                _timeLastActive = Time.time;
            }
        }

        private Vector3 FindCenterMass()
        {
            PlayerComponent enemy = EnemyPlayerComponent;
            Vector3 headPos = enemy.Player.MainParts[BodyPartType.head].Position;
            Vector3 floorPos = enemy.Position;
            Vector3 centerMass = Vector3.Lerp(headPos, floorPos, SAINPlugin.LoadedPreset.GlobalSettings.Aiming.CenterMassVal);
            return centerMass;
        }

        public void UpdateLastSeenPosition(Vector3 position)
        {
            var place = KnownPlaces.UpdateSeenPlace(position);
            Bot.Squad.SquadInfo?.ReportEnemyPosition(this, place, true);
        }

        public void UpdateCurrentEnemyPos(Vector3 position)
        {
            var place = KnownPlaces.UpdateSeenPlace(position);
            if (_nextReportSightTime < Time.time)
            {
                _nextReportSightTime = Time.time + SQUADREPORT_SIGHT_INTERVAL;
                Bot.Squad.SquadInfo?.ReportEnemyPosition(this, place, true);
            }
        }

        public void EnemyPositionReported(EnemyPlace place, bool seen)
        {
            if (seen)
            {
                KnownPlaces.UpdateSquadSeenPlace(place);
            }
            else
            {
                KnownPlaces.UpdateSquadHeardPlace(place);
            }
        }

        public void SetEnemyAsSniper(bool isSniper)
        {
            IsSniper = isSniper;
            if (isSniper && Bot.Squad.BotInGroup && Bot.Talk.GroupTalk.FriendIsClose)
            {
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.33f, 0.66f));
            }
        }

        private EnemyKnownChecker KnownChecker { get; }
        private EnemyActiveThreatChecker ActiveThreatChecker { get; }
        private EnemyValidChecker ValidChecker { get; }

        private Vector3 _moveDirection;
        private float _nextCalcMoveDirTime;
        private bool _visPathPointIsCorner;
        public float NextCheckFlashLightTime;
        private float _nextUpdateCoefTime;
        private bool _hasBeenActive;
        private Vector3? _centerMass;
        private float _nextGetCenterTime;
        private float _nextReportSightTime;
        private float _timeLastActive;
    }
}