using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class BotPathFollowerClass(BotComponent sain) : BotBase(sain)
    {
        private Coroutine _moveToPointCoroutine;
        private static MoveSettings _moveSettings => SAINPlugin.LoadedPreset.GlobalSettings.Move;
        private bool positionMoving;
        private Vector3 lastCheckPos;
        private float nextCheckPosTime;
        private float timeSinceNotMoving => positionMoving ? 0f : Time.time - _timeNotMoving;
        private float _timeNotMoving;

        public IBotMoveData MoveData {
            get
            {
                return _moveData;
            }
        }

        public bool Moving => _moveToPointCoroutine != null;
        public bool Running => _moveToPointCoroutine != null && MoveData.WantToSprint;

        private readonly BotMoveDataClass _moveData = new();

        public void Pause(float duration)
        {
            if (duration > 0f)
            {
                _moveData.CurrentMoveStatus = EBotMoveStatus.Paused;
                _moveData.PauseTime = Time.time + duration;
            }
        }

        public void Unpause()
        {
            _moveData.PauseTime = -1;
        }

        public void Cancel(float afterTime = -1f)
        {
            if (Moving)
            {
                if (afterTime <= 0)
                {
                    StopMoveCoroutine();
                    return;
                }
                if (_moveData.CurrentMoveStatus != EBotMoveStatus.Canceling)
                {
                    _moveData.CurrentMoveStatus = EBotMoveStatus.Canceling;
                    _moveData.CancelTime = Time.time + afterTime;
                }
            }
        }

        private void StopMoveCoroutine()
        {
            if (_moveToPointCoroutine != null)
            {
                Bot.StopCoroutine(_moveToPointCoroutine);
                _moveData.Dispose();
                //Logger.LogDebug($"[{BotOwner.name}] Move Stopped: [{Time.time}]");
            }
        }

        public bool RunToPoint(Vector3 point, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, bool mustHaveCompletePath = true, System.Action callback = null)
        {
            if (checkSameWay && TryUpdatePath(point, true))
            {
                _moveData.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
                return true;
            }

            if (Bot.Mover.CanGoToPoint(point, out NavMeshPath path, mustHaveCompletePath))
            {
                TriggerNewMove(path.corners, point, true, urgency, callback);
                _moveData.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
                return true;
            }
            return false;
        }

        public bool RunToPointByWay(Vector3[] way, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (way == null)
                return false;
            if (way.Length <= 1)
                return false;
            Vector3 lastCorner = way[way.Length - 1];
            if (checkSameWay && TryUpdatePath(lastCorner, true))
            {
                _moveData.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
                return true;
            }
            TriggerNewMove(way, lastCorner, true, urgency, callback);
            _moveData.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            return true;
        }

        public bool RunToPointByWay(NavMeshPath way, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            return RunToPointByWay(way?.corners, urgency, stopSprintEnemyVisible, checkSameWay, callback);
        }

        public bool WalkToPoint(Vector3 point, bool checkSameWay = true, bool mustHaveCompletePath = true, System.Action callback = null)
        {
            if (checkSameWay && TryUpdatePath(point, false))
            {
                return true;
            }
            if (Bot.Mover.CanGoToPoint(point, out NavMeshPath path, mustHaveCompletePath))
            {
                TriggerNewMove(path.corners, point, false, ESprintUrgency.None, callback);
                return true;
            }
            return false;
        }

        public bool WalkToPointByWay(Vector3[] way, bool checkSameWay = true, System.Action callback = null)
        {
            if (way == null) return false;
            if (way.Length <= 1) return false;
            Vector3 lastCorner = way[way.Length - 1];
            if (checkSameWay && TryUpdatePath(lastCorner, false)) return true;
            TriggerNewMove(way, lastCorner, false, ESprintUrgency.None, callback);
            return true;
        }

        public bool WalkToPointByWay(NavMeshPath way, bool checkSameWay = true, System.Action callback = null)
        {
            return WalkToPointByWay(way.corners, checkSameWay, callback);
        }

        private bool TryUpdatePath(Vector3 point, bool shallSprint)
        {
            if (Moving && _moveData.TryUpdatePath(point))
            {
                PrepareBot(shallSprint);
                return true;
            }
            return false;
        }

        private void TriggerNewMove(Vector3[] path, Vector3 point, bool shallSprint, ESprintUrgency urgency, System.Action callback)
        {
            const float SHORT_CORNER_LENGTH = 0.25f;
            StopMoveCoroutine();
            PrepareBot(shallSprint);
            _moveData.ActivateNewPath(point, shallSprint, urgency, path, SHORT_CORNER_LENGTH);
            _moveToPointCoroutine = Bot.StartCoroutine(GoToPointCoRoutine(_moveData, callback));
        }

        private void PrepareBot(bool sprinting)
        {
            _moveData.WantToSprint = sprinting;
            BotOwner.Mover.Stop();
            if (sprinting)
            {
                Bot.Aim.LoseAimTarget();
                Bot.AimDownSightsController.SetADS(false);
                Bot.Mover.Prone.SetProne(false);
            }
        }

        public bool RecalcPath()
        {
            if (_moveData.WantToSprint)
                return RunToPoint(_moveData.Destination.Position, _moveData.SprintUrgency, false);
            return WalkToPoint(_moveData.Destination.Position, false);
        }

        private IEnumerator GoToPointCoRoutine(BotMoveDataClass MoveData, System.Action callback = null)
        {
            //Logger.LogDebug($"[{BotOwner.name}] start Move Time: [{Time.time}]");
            positionMoving = true;
            // Start running!
            yield return ExecutePath(MoveData);

            callback?.Invoke();
            StopMoveCoroutine();
            //Logger.LogDebug($"[{BotOwner.name}] end Move Time: [{Time.time}]");
        }

        private IEnumerator ExecutePath(BotMoveDataClass MoveData)
        {
            bool canTryVault = Bot.Info.FileSettings.Move.VAULT_TOGGLE && GlobalSettingsClass.Instance.Move.VAULT_TOGGLE;

            for (int i = 0; i <= MoveData.CornerCount; i++)
            {
                BotCornerDetails corner;
                MoveData.CurrentIndex = i;

                if (i == MoveData.CornerCount)
                {
                    corner = MoveData.Destination;
                    corner.SetStarted(Time.time);
                    MoveData.Destination = corner;
                }
                else
                {
                    corner = MoveData.PathCornerDetails[i];
                    corner.SetStarted(Time.time);
                    MoveData.PathCornerDetails[i] = corner;
                }

                MoveData.CurrentCorner = corner;
                MoveData.CurrentCornerDistanceSqr = (corner.Position - Bot.Position).sqrMagnitude;
                while (Bot != null && MoveData.CurrentCornerDistanceSqr > ReachDist(_moveData.WantToSprint))
                {
                    if (!Bot.SAINLayersActive)
                    {
                        StopMoveCoroutine();
                        yield break;
                    }
                    BotOwner botOwner = Bot.BotOwner;
                    Player botPlayer = Bot.Player;
                    PersonTransformClass botTransform = Bot.Transform;
                    BotMover botOwnerMover = botOwner.Mover;
                    SAINMoverClass sainMover = Bot.Mover;
                    DoorOpener doorOpener = Bot.DoorOpener;
                    if (doorOpener.Interacting)
                    {
                        _timeNotMoving = -1f;
                        positionMoving = true;
                        sainMover.Prone.SetProne(false);
                        sainMover.EnableSprintPlayer(false);
                        if (doorOpener.BreachingDoor)
                        {
                            yield return null;
                            continue;
                        }
                    }
                    if (MoveData.Paused && MoveData.CheckPaused())
                    {
                        _timeNotMoving = -1f;
                        if (MoveData.WantToSprint)
                        {
                            Bot.Steering.SteerByPriority(null, true, true);
                            Bot.Mover.EnableSprintPlayer(false);
                        }
                        yield return null;
                        continue;
                    }
                    MoveToCurrentCorner(MoveData, out bool recalcPath, Bot, botPlayer, botTransform, botOwnerMover, sainMover, doorOpener, canTryVault);
                    if (recalcPath)
                    {
                        RecalcPath();
                        yield break;
                    }
                    if (MoveData.Canceling && MoveData.CancelTime < Time.time)
                    {
                        StopMoveCoroutine();
                        yield break;
                    }
                    yield return null;
                }

                //corner.SetComplete(Time.time);
                //if (corner.Type == EBotCornerType.Destination)
                //{
                //    if (MoveData.Destination.Index != corner.Index)
                //    {
                //        corner.Type = corners[corner.Index].Type;
                //        corners[corner.Index] = corner;
                //    }
                //    else
                //    {
                //        MoveData.Destination = corner;
                //    }
                //    continue;
                //}
                //if (i < corners.Count)
                //{
                //    corners[i] = corner;
                //}
            }
        }

        private static float ReachDist(bool sprinting)
        {
            return sprinting ? SAINPlugin.LoadedPreset.GlobalSettings.Move.BotSprintCornerReachDist : SAINPlugin.LoadedPreset.GlobalSettings.Move.BotWalkCornerReachDist;
        }

        private void MoveToCurrentCorner(BotMoveDataClass MoveData, out bool recalcPath, BotComponent bot, Player botPlayer, PersonTransformClass botTransform, BotMover botOwnerMover, SAINMoverClass sainMover, DoorOpener doorOpener, bool canTryVault)
        {
            BotCornerDetails activeCorner = _moveData.CurrentCorner;
            Enemy enemy = bot.CurrentTarget.CurrentTargetEnemy;
            Vector3 botPosition = botTransform.Position;

            Vector3 botNavPosition = botPosition;
            if (NavMesh.SamplePosition(botPosition, out NavMeshHit hit, 0.5f, -1))
                botNavPosition.y = hit.position.y;

            _moveData.CurrentCornerDistanceSqr = (activeCorner.Position - botNavPosition).sqrMagnitude;

            if (botOwnerMover.IsMoving)
                botOwnerMover.Stop(); // Backwards sprint / moonwalking fix

            sainMover.Prone.SetProne(!MoveData.WantToSprint && sainMover.Crawling);

            if (SAINPlugin.DebugMode)
                DrawMoverDebug(botPosition, activeCorner.Position);

            if (MoveData.WantToSprint)
            {
                HandleSprinting(MoveData, botPlayer.MovementContext, botTransform, enemy, doorOpener, botPlayer.Physical.Stamina.NormalValue);
            }
            else
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.None;
                MoveData.ShallSprintNow = false;
            }
            bot.Mover.EnableSprintPlayer(MoveData.ShallSprintNow);

            TrackMovement(botPosition);
            float timeSinceNoMove = timeSinceNotMoving;
            if (timeSinceNoMove > _moveSettings.BotSprintRecalcTime && Time.time - MoveData.TimeStarted > 2f)
            {
                recalcPath = true;
                return;
            }
            //else if (timeSinceNoMove > _moveSettings.BotSprintTryJumpTime)
            //{
            //    SAINBot.Mover.TryJump();
            //}
            else if (canTryVault && timeSinceNoMove > _moveSettings.BotSprintTryVaultTime)
            {
                bot.Mover.TryVault();
            }

            Bot.Mover.MovePlayerCharacterToPoint(activeCorner.Position);
            SetPlayerSteering(MoveData, activeCorner.Position, bot, enemy);
            recalcPath = false;
        }

        private static void DrawMoverDebug(Vector3 BotPos, Vector3 destination)
        {
            Vector3 debugOffset = Vector3.up * 0.6f;
            DebugGizmos.Sphere(destination, 0.2f, Color.white, 0.02f);
            DebugGizmos.Line(destination, destination + debugOffset, Color.white, 0.075f, 0.02f);
            DebugGizmos.Line(destination + debugOffset, BotPos + debugOffset, Color.white, 0.075f, 0.02f);
        }

        private static float FindStartSprintStamina(ESprintUrgency urgency)
        {
            return urgency switch {
                ESprintUrgency.None or ESprintUrgency.Low => 0.75f,
                ESprintUrgency.Middle => 0.5f,
                ESprintUrgency.High => 0.2f,
                _ => 0.5f,
            };
        }

        private static float FindEndSprintStamina(ESprintUrgency urgency)
        {
            return urgency switch {
                ESprintUrgency.None or ESprintUrgency.Low => 0.4f,
                ESprintUrgency.Middle => 0.2f,
                ESprintUrgency.High => 0.01f,
                _ => 0.25f,
            };
        }

        private static bool SprintCheck1(BotMoveDataClass MoveData, Enemy enemy)
        {
            if (MoveData.Canceling)
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.Canceling;
                MoveData.ShallSprintNow = false;
                return false;
            }
            //if (MoveData.CurrentCorner.ShortCorner)
            //{
            //    MoveData.CurrentSprintStatus = EBotSprintStatus.ShortCorner;
            //    MoveData.ShallSprintNow = false;
            //    return false;
            //}
            if (MoveData.ShallStopSprintWhenSeeEnemy && enemy?.IsVisible == true)
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.LookAtEnemyNoSprint;
                MoveData.ShallSprintNow = false;
                return false;
            }
            return true;
        }

        private static void HandleSprinting(BotMoveDataClass MoveData, MovementContext movementContext, PersonTransformClass botTransform, Enemy enemy, DoorOpener doorOpener, float staminaNormal)
        {
            if (!SprintCheck1(MoveData, enemy))
            {
                return;
            }
            // Were messing with a door, dont sprint
            if (doorOpener.ShallPauseSprintForOpening())
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.InteractingWithDoor;
                MoveData.ShallSprintNow = false;
                return;
            }

            // I cant sprint :(
            if (!movementContext.CanSprint)
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.CantSprint;
                MoveData.ShallSprintNow = false;
                return;
            }

            // We are out of stamina, stop sprinting.
            if (ShallPauseSprintStamina(staminaNormal, MoveData.SprintUrgency))
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.NoStamina;
                MoveData.ShallSprintNow = false;
                return;
            }

            bool sprintingNow = movementContext.IsSprintEnabled;
            //HandleDumbShit(ref movementContext, sprintingNow);
            if (CheckArrivingAtDestination(MoveData, sprintingNow))
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.ArrivingAtDestination;
                MoveData.ShallSprintNow = false;
                return;
            }

            // If we are not looking in the direction of the corner we are moving toward, dont sprint.
            if (FindHorizontalAngleFromLookDir(botTransform.WeaponRoot, MoveData.CurrentCorner.Position, botTransform.LookDirection) >= _moveSettings.BotSprintCurrentCornerAngleMax)
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.Turning;
                MoveData.ShallSprintNow = false;
                return;
            }

            // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
            if (MoveData.CurrentCornerDistanceSqr > 0.5f && ShallStartSprintStamina(staminaNormal, MoveData.SprintUrgency))
            {
                MoveData.CurrentSprintStatus = EBotSprintStatus.Running;
                MoveData.ShallSprintNow = true;
            }
        }

        private void HandleDumbShit(ref MovementContext movementContext, bool sprintingNow)
        {
            if (sprintingNow)
            {
                if (Bot.IsCheater)
                {
                    movementContext.SprintSpeed = 50f;
                }
                else if (_moveSettings.EditSprintSpeed)
                {
                    movementContext.SprintSpeed = 1.5f;
                }
            }
        }

        private static bool CheckArrivingAtDestination(BotMoveDataClass MoveData, bool sprintingNow)
        {
            // We are arriving to our destination, stop sprinting when you get close.
            switch (MoveData.CurrentCorner.Type)
            {
                case EBotCornerType.PathEnd:
                case EBotCornerType.Destination:
                    break;

                default:
                    return false;
            }
            float StopSprintDistSqr = _moveSettings.BotSprintDistanceToStopSprintDestination.Sqr();
            float maxSprintDist = sprintingNow ? StopSprintDistSqr : StopSprintDistSqr * 1.1f;
            return MoveData.CurrentCornerDistanceSqr <= maxSprintDist;
        }

        private static bool ShallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= FindEndSprintStamina(urgency);

        private static bool ShallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= FindStartSprintStamina(urgency);

        private static float FindHorizontalAngleFromLookDir(Vector3 start, Vector3 end, Vector3 lookDirection)
        {
            Vector3 direction = end - start;
            lookDirection.y = 0;
            direction.y = 0;
            return Vector3.Angle(lookDirection, direction.normalized);
        }

        private void TrackMovement(Vector3 botPos)
        {
            if (nextCheckPosTime < Time.time)
            {
                nextCheckPosTime = Time.time + _moveSettings.BotSprintNotMovingCheckFreq;
                positionMoving = (botPos - lastCheckPos).sqrMagnitude > _moveSettings.BotSprintNotMovingThreshold;
                if (positionMoving)
                {
                    _timeNotMoving = -1f;
                    lastCheckPos = botPos;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
            }
        }

        private static void SetPlayerSteering(BotMoveDataClass MoveData, Vector3 target, BotComponent bot, Enemy enemy)
        {
            if (MoveData.WantToSprint)
            {
                if (MoveData.ShallStopSprintWhenSeeEnemy && enemy?.IsVisible == true)
                {
                    bot.Steering.LookToEnemy(enemy);
                }
                else if (!ShallSteerbyPriority(MoveData) || !bot.Steering.SteerByPriority(enemy, false, true))
                {
                    bot.Steering.LookToPoint(target + bot.Steering.WeaponRootOffset);
                }
            }
        }

        private static bool ShallSteerbyPriority(BotMoveDataClass MoveData)
        {
            if (MoveData.ShallSprintNow)
            {
                return false;
            }
            return MoveData.CurrentSprintStatus switch {
                EBotSprintStatus.Turning or
                EBotSprintStatus.Running or
                EBotSprintStatus.ShortCorner => false,
                _ => true,
            };
        }
    }
}