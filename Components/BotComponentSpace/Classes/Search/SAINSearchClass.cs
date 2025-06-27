using EFT;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.Preset.Personalities;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Search
{
    public class SAINSearchClass : BotBase, IBotClass
    {
        public bool SearchActive { get; private set; }

        public ESearchMove NextState { get; private set; }
        public ESearchMove CurrentState { get; private set; }
        public ESearchMove LastState { get; private set; }

        public Vector3? FinalDestination => PathFinder.FinalDestination;
        public BotPeekPlan? PeekPoints => PathFinder.PeekPoints;

        public SearchDeciderClass SearchDecider { get; private set; }
        public SearchPathFinder PathFinder { get; private set; }

        public SAINSearchClass(BotComponent sain) : base(sain)
        {
            SearchDecider = new SearchDeciderClass(this);
            PathFinder = new SearchPathFinder(this);
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void ToggleSearch(bool value, Enemy target)
        {
            SearchActive = value;
            if (target != null)
            {
                target.Events.OnSearch.CheckToggle(value);
            }
            if (!value)
            {
                Reset();
            }
        }

        public void Search(bool shallSprint, Enemy enemy)
        {
            PathFinder.UpdateSearchDestination(enemy);
            SwitchSearchModes(shallSprint, enemy);
            PeekPoints?.DrawDebug();
        }

        private bool WaitAtPoint()
        {
            if (_waitAtPointTimer < 0)
            {
                float baseTime = 3;
                baseTime *= Bot.Info.PersonalitySettings.Search.SearchWaitMultiplier;
                float waitTime = baseTime * Random.Range(0.25f, 1.25f);
                _waitAtPointTimer = Time.time + waitTime;
                //BotOwner.Mover.MovementPause(waitTime, false);
            }
            if (_waitAtPointTimer < Time.time)
            {
                //BotOwner.Mover.MovementResume();
                _waitAtPointTimer = -1;
                return false;
            }
            return true;
        }

        private bool MoveToPoint(Vector3 destination, bool shallSprint)
        {
            var sprint = Bot.Mover.SprintController;
            if (shallSprint &&
                sprint.RunToPoint(destination, Mover.ESprintUrgency.Middle, true))
            {
                return true;
            }
            if (Bot.Mover.GoToPoint(destination, out _))
            {
                return true;
            }
            return false;
        }

        private void HandleLight(bool stealthy)
        {
            if (_Running || Bot.Mover.SprintController.Running)
            {
                return;
            }
            if (stealthy || _searchSettings.Sneaky)
            {
                Bot.BotLight.ToggleLight(false);
                return;
            }
            if (BotOwner.Mover?.IsMoving == true)
            {
                Bot.BotLight.HandleLightForSearch(BotOwner.Mover.DirCurPoint.magnitude);
                return;
            }
        }

        private void SwitchSearchModes(bool shallSprint, Enemy enemy)
        {
            if (FinalDestination == null)
            {
                Logger.LogWarning($"{BotOwner.name}'s Final Destination is null, cannot search!");
                return;
            }

            if (CheckEndPeek())
            {
                LastState = CurrentState;
                CurrentState = ESearchMove.None;
            }

            bool shallBeStealthy = SearchDecider.ShallBeStealthyDuringSearch(enemy);
            GetSpeedandPose(out float speed, out float pose, shallSprint, shallBeStealthy);
            HandleLight(shallBeStealthy);

            CheckShallWaitandReload();
            if (ShallSwapToSprint(shallSprint, speed, pose))
            {
                return;
            }

            ESearchMove previousState = CurrentState;
            PeekPosition? peekPosition;
            switch (CurrentState)
            {
                case ESearchMove.None:
                    if (ShallStartPeek(shallSprint))
                    {
                        CurrentState = ESearchMove.MoveToStartPeek;
                        break;
                    }

                    if (MoveToPoint(FinalDestination.Value, shallSprint))
                    {
                        CurrentState = ESearchMove.DirectMove;
                        break;
                    }
                    Logger.LogWarning($"{BotOwner.name}'s cannot peek and cannot direct move!");
                    break;

                case ESearchMove.DirectMove:

                    SetSpeedPose(speed, pose);
                    MoveToPoint(FinalDestination.Value, shallSprint);
                    break;

                case ESearchMove.Advance:

                    if (_advanceTime < 0)
                    {
                        _advanceTime = Time.time + 5f;
                    }
                    if (_advanceTime < Time.time)
                    {
                        _advanceTime = -1f;
                        CurrentState = ESearchMove.None;
                        PathFinder.FinishedPeeking = true;
                        break;
                    }

                    SetSpeedPose(speed, pose);
                    MoveToPoint(FinalDestination.Value, shallSprint);
                    break;

                case ESearchMove.MoveToStartPeek:

                    peekPosition = PeekPoints?.PeekStart;
                    if (peekPosition != null &&
                        !BotIsAtPoint(peekPosition.Value.Point))
                    {
                        SetSpeedPose(speed, pose);
                        if (MoveToPoint(peekPosition.Value.Point, shallSprint))
                        {
                            break;
                        }
                    }
                    CurrentState = ESearchMove.MoveToEndPeek;
                    break;

                case ESearchMove.MoveToEndPeek:

                    peekPosition = PeekPoints?.PeekEnd;
                    if (peekPosition != null &&
                        !BotIsAtPoint(peekPosition.Value.Point))
                    {
                        SetSpeedPose(speed, pose);
                        if (MoveToPoint(peekPosition.Value.Point, shallSprint))
                        {
                            break;
                        }
                    }
                    CurrentState = ESearchMove.Wait;
                    NextState = ESearchMove.MoveToDangerPoint;
                    break;

                case ESearchMove.MoveToDangerPoint:

                    Vector3? danger = PeekPoints?.DangerPoint;
                    if (danger != null &&
                        !BotIsAtPoint(danger.Value))
                    {
                        SetSpeedPose(speed, pose);
                        if (MoveToPoint(danger.Value, shallSprint))
                        {
                            break;
                        }
                    }
                    CurrentState = ESearchMove.Advance;
                    break;

                case ESearchMove.Wait:
                    if (WaitAtPoint())
                    {
                        Bot.Mover.SetTargetMoveSpeed(0f);
                        Bot.Mover.SetTargetPose(0.75f);
                        break;
                    }
                    Bot.Mover.SetTargetMoveSpeed(speed);
                    Bot.Mover.SetTargetPose(pose);
                    CurrentState = NextState;
                    break;
            }

            if (previousState != CurrentState)
            {
                LastState = previousState;
            }
        }

        private bool ShallSwapToSprint(bool shallSprint, float speed, float pose)
        {
            if (!shallSprint)
            {
                return false;
            }

            switch (CurrentState)
            {
                case ESearchMove.DirectMove:
                case ESearchMove.None:
                    return false;

                default:
                    break;
            }

            if (!MoveToPoint(FinalDestination.Value, true))
            {
                return false;
            }

            LastState = CurrentState;
            CurrentState = ESearchMove.DirectMove;
            SetSpeedPose(speed, pose);
            return true;
        }

        private bool CheckEndPeek()
        {
            switch (CurrentState)
            {
                case ESearchMove.None:
                case ESearchMove.DirectMove:
                case ESearchMove.Wait:
                case ESearchMove.Advance:
                    return false;

                default:
                    if (PeekPoints == null)
                    {
                        return true;
                    }
                    if (PathFinder.FinishedPeeking)
                    {
                        return true;
                    }
                    return false;
            }
        }

        private void GetSpeedandPose(out float speed, out float pose, bool sprinting, bool stealthy)
        {
            speed = 1f;
            pose = 1f;
            // are we sprinting?
            if (sprinting || Player.IsSprintEnabled || _Running || Bot.Mover.SprintController.Running)
            {
                return;
            }
            // are we indoors?
            if (GetIndoorsSpeedPose(stealthy, out speed, out pose))
            {
                return;
            }
            // we are outside...
            if (_searchSettings.Sneaky &&
                Bot.Cover.CoverPoints.Count > 2 &&
                Time.time - BotOwner.Memory.UnderFireTime > 30f)
            {
                speed = 0.25f;
                pose = 0.6f;
                return;
            }
            if (stealthy)
            {
                speed = 0.5f;
                pose = 0.7f;
                return;
            }
        }

        private bool GetIndoorsSpeedPose(bool stealthy, out float speed, out float pose)
        {
            speed = 1f;
            pose = 1f;
            if (!Bot.Memory.Location.IsIndoors)
            {
                return false;
            }
            var searchSettings = _searchSettings;
            if (searchSettings.Sneaky)
            {
                speed = searchSettings.SneakySpeed;
                pose = searchSettings.SneakyPose;
            }
            else if (stealthy)
            {
                speed = 0.33f;
                pose = 1f;
            }
            return true;
        }

        private void CheckShallWaitandReload()
        {
            if (BotOwner.WeaponManager?.Reload?.Reloading == true &&
                CurrentState != ESearchMove.Wait)
            {
                NextState = CurrentState;
                CurrentState = ESearchMove.Wait;
            }
        }

        private bool ShallStartPeek(bool shallSprint)
        {
            if (shallSprint)
            {
                return false;
            }
            if (PeekPoints != null && MoveToPoint(PeekPoints.Value.PeekStart.Point, shallSprint))
            {
                return true;
            }
            return false;
        }

        private bool ShallDirectMove(bool shallSprint)
        {
            if (shallSprint)
            {
                return true;
            }
            if (PeekPoints == null)
            {
                return true;
            }
            if (MoveToPoint(FinalDestination.Value, shallSprint))
            {
                return true;
            }
            return false;
        }

        private void SetSpeedPose(float speed, float pose)
        {
            Bot.Mover.SetTargetMoveSpeed(speed);
            Bot.Mover.SetTargetPose(pose);
        }

        public void Reset()
        {
            ResetStates();
            PathFinder.Reset();
        }

        public void ResetStates()
        {
            CurrentState = ESearchMove.None;
            LastState = ESearchMove.None;
            NextState = ESearchMove.None;
        }

        public bool BotIsAtPoint(Vector3 point, float reachDist = 0.5f)
        {
            return DistanceToDestination(point) < reachDist;
        }

        public float DistanceToDestination(Vector3 point)
        {
            return (point - Bot.Position).magnitude;
        }

        private bool _Running => Bot.Mover.SprintController.Running;
        private float _waitAtPointTimer = -1;
        private float _advanceTime;
        private PersonalitySearchSettings _searchSettings => Bot.Info.PersonalitySettings.Search;
    }
}