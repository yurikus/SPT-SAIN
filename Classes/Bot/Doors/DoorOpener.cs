using EFT;
using EFT.Interactive;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class DoorOpener : BotComponentClassBase
    {
        public bool Interacting { get; private set; }

        public bool BreachingDoor { get; private set; }

        public DoorFinder DoorFinder { get; }

        public DoorOpener(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            DoorFinder = new DoorFinder(this);
        }

        public override void Init()
        {
            DoorFinder.Init();
            base.Init();
        }

        public override void ManualUpdate()
        {
            if (!_debugMode && _debugObjects.Count > 0)
            {
                foreach (var obj in _debugObjects.Values)
                {
                    GameObject.Destroy(obj.link);
                    GameObject.Destroy(obj.midClose);
                    GameObject.Destroy(obj.midOpen);
                }
                _debugObjects.Clear();
            }
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            DoorFinder.Dispose();
            base.Dispose();
        }

        private void drawLink(NavMeshDoorLink link)
        {
            if (_debugMode &&
                SAINPlugin.DebugSettings.Gizmos.DrawDebugGizmos &&
                !_debugObjects.ContainsKey(link))
            {
                Vector3 linkPosition = link.transform.position + Vector3.down;
                var objects = new linkObjects {
                    link = DebugGizmos.DrawLine(linkPosition, linkPosition + Vector3.up * 2f, Color.white, 0.2f),
                    midOpen = DebugGizmos.DrawLine(linkPosition, link.MidOpen + Vector3.down, Color.blue, 0.2f),
                    midClose = DebugGizmos.DrawLine(linkPosition, link.MidClose + Vector3.down, Color.green, 0.2f),
                };
                _debugObjects.Add(link, objects);
            }
        }

        private struct linkObjects
        {
            public GameObject link;
            public GameObject midOpen;
            public GameObject midClose;
        }

        private bool CanInteract(NavMeshDoorLink link)
        {
            if (!link.ShallInteract())
            {
                //Logger.LogDebug($"Link {link.Id} shall not interact!");
                return false;
            }
            if (!link.Door.enabled || !link.Door.gameObject.activeInHierarchy)
            {
                return false;
            }
            if (CheckIfDoorLast(link))
            {
                //Logger.LogDebug($"Link {link.Id} is last!");
                return false;
            }
            if (!link.Door.Operatable || !link.Door.enabled)
            {
                //Logger.LogDebug($"Link {link.Id} door not operable!");
                return false;
            }
            return true;
        }

        public List<DoorData> FindDoorsToInteractWith(Vector3 botPosition)
        {
            findPossibleInteractDoors(DoorFinder.InteractionDoors, botPosition);
            return _possibleInteractDoors;
        }

        public bool InteractWithDoor(DoorData data, Vector3 botPosition, bool shallKick)
        {
            if (data?.Door == null) return false;
            return data.Door.DoorState switch {
                EDoorState.Shut => OpenDoor(data, botPosition, shallKick),
                EDoorState.Open => CloseDoor(data, botPosition),
                _ => false,
            };
        }

        public bool CloseDoor(DoorData data, Vector3 botPosition)
        {
            if (data == null || data.Door.DoorState != EDoorState.Open) return false;
            data.LastInteractTime = Time.time;
            data.LastCloseTime = Time.time;
            Interact(data, EInteractionType.Close, botPosition);
            return true;
        }

        public bool OpenDoor(DoorData data, Vector3 botPosition, bool shallKick)
        {
            if (data == null || data.Door.DoorState != EDoorState.Shut) return false;
            data.LastInteractTime = Time.time;
            data.LastOpenTime = Time.time;
            Interact(data, shallKick ? EInteractionType.Breach : EInteractionType.Open, botPosition);
            return true;
        }

        private void findPossibleInteractDoors(List<DoorData> list, Vector3 botPosition)
        {
            _possibleInteractDoors.Clear();
            foreach (var data in list)
            {
                NavMeshDoorLink link = data.Link;
                if (!CanInteract(link))
                {
                    //Logger.LogDebug($"Cant Interact [{link.Id}]");
                    continue;
                }

                if (!data.CanInteractByTime())
                {
                    //Logger.LogDebug($"Cant Interact by time [{link.Id}]");
                    continue;
                }

                Door door = data.Door;
                drawLink(link);
                float maxDistance;

                switch (door.DoorState)
                {
                    case EDoorState.Open:
                        maxDistance = 3f * 3f;
                        break;

                    case EDoorState.Shut:
                        maxDistance = 3f * 3f;
                        break;

                    default:
                        continue;
                }
                if (data.CurrentSqrMagnitude > maxDistance)
                {
                    //Logger.LogDebug($"Toofar [{link.Id}] Dist: [{data.CurrentSqrMagnitude.Sqrt()}] maxDist: [{maxDistance.Sqrt()}]");
                    continue;
                }
                if (!CheckWantToInteract(data, botPosition))
                {
                    //Logger.LogDebug($"Dont want to interact with [{link.Id}]");
                    continue;
                }
                _possibleInteractDoors.Add(data);
            }
        }

        private readonly List<DoorData> _possibleInteractDoors = [];
        private const float DOOR_CHECK_FREQ = 0.25f;

        private bool CheckIfDoorLast(NavMeshDoorLink link)
        {
            DoorData lastInfo = _activeDoor;
            if (lastInfo == null)
            {
                return false;
            }
            if (lastInfo.Link.Id != link.Id)
            {
                return false;
            }
            return true;
        }

        public bool ShallKickOpen(Door door, EInteractionType Etype)
        {
            if (Etype != EInteractionType.Open)
            {
                return false;
            }
            if (!WantToKick())
            {
                return false;
            }
            var breakInParameters = door.GetBreakInParameters(Bot.Position);
            return door.BreachSuccessRoll(breakInParameters.InteractionPosition);
        }

        private bool WantToKick()
        {
            var enemy = Bot.GoalEnemy;
            if (enemy != null)
            {
                if (Bot.Info.PersonalitySettings.General.KickOpenAllDoors)
                {
                    return true;
                }
                if (BotOwner.Memory.IsUnderFire)
                {
                    return true;
                }
                float? timeSinceSeen = enemy.TimeSinceSeen;
                if (timeSinceSeen != null)
                {
                    if (timeSinceSeen.Value < 3f)
                    {
                        return true;
                    }
                    if (timeSinceSeen.Value < 5f && enemy.InLineOfSight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Interact(DoorData data, EInteractionType Etype, Vector3 botPosition, System.Action onInteractionDone = null)
        {
            _activeDoor = data;
            data.LastInteractTime = Time.time;
            Door door = data.Door;
            BreachingDoor = Etype == EInteractionType.Breach;
            switch (Etype)
            {
                case EInteractionType.Breach:
                    break;

                case EInteractionType.Open:
                    door.Snap = EDoorState.None;
                    break;

                case EInteractionType.Close:
                    door.Snap = EDoorState.None;
                    break;

                default:
                    return;
            }

            if (Etype == EInteractionType.Breach ||
                ModDetection.ProjectFikaLoaded ||
                !GlobalSettingsClass.Instance.General.Doors.NoDoorAnimations)
            {
                //Logger.LogDebug($"{BotOwner.name} Executing [{Etype}] door interaction on {doorInfo.Link.Id}");
                Bot.Steering.LookToFloorPoint(data.LinkPosition);
                InteractionResult interactionResult = new(Etype);
                Player.CurrentManagedState.StartDoorInteraction(door, interactionResult, onInteractionDone);
                Interacting = true;
                return;
            }

            //Logger.LogDebug($"{BotOwner.name} Auto Opening Door on {doorInfo.Link.Id}");

            EDoorState state;
            switch (Etype)
            {
                case EInteractionType.Open:
                    state = EDoorState.Open;
                    break;

                case EInteractionType.Close:
                    state = EDoorState.Shut;
                    break;

                default:
                    Logger.LogError($"Door open type set wrong! {Etype}");
                    return;
            }

            bool shallInvert = ShallInvertDoorAngle(door, botPosition);
            GameWorldComponent.Instance.Doors.ChangeDoorState(door, state, shallInvert);
            BotManagerComponent.Instance.BotHearing.PlayAISound(PlayerComponent, SAINSoundType.Door, data.Link.MidClose, 30f, 1f, true);
            onInteractionDone?.Invoke();
        }

        private static bool ShallInvertDoorAngle(Door door, Vector3 botPosition)
        {
            return GlobalSettingsClass.Instance.General.Doors.InvertDoors && IsDoorPullOpen(door, botPosition);
        }

        public static bool IsDoorPullOpen(Door door, Vector3 botPosition)
        {
            return door.GetInteractionParameters(botPosition).AnimationId == (int)EInteraction.DoorPullBackward;
        }

        public static bool CheckWantToInteract(DoorData data, Vector3 botPosition)
        {
            botPosition += Vector3.up;
            if (Mathf.Abs(botPosition.y - data.Link.MidClose.y) > 2f)
            {
                return false;
            }
            return data.Door.DoorState switch {
                EDoorState.Open => data.DotProduct < 0.25f,
                EDoorState.Shut => data.DotProduct > 0.25f,
                _ => false,
            };
        }

        private static bool _debugMode => SAINPlugin.DebugSettings.Gizmos.DrawDoorLinks;
        private DoorData _activeDoor;
        private static readonly Dictionary<NavMeshDoorLink, linkObjects> _debugObjects = new();
    }
}