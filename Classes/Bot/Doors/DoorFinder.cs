using EFT;
using EFT.Interactive;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class DoorFinderNew(DoorOpener opener) : BotSubClass<DoorOpener>(opener)
    {
        private const float DOOR_UPDATE_INTERVAL = 0.5f;

        public List<DoorDataStruct> InteractionDoors { get; } = [];
        public List<DoorDataStruct> AllDoors { get; } = [];
        public NavGraphVoxelSimple CurrentVoxel { get; private set; }

        public void UpdateDoors(Vector3 botPosition, Vector3 corner)
        {
            float time = Time.time;
            if (_nextDoorUpdateTime < time)
            {
                _nextDoorUpdateTime = time + DOOR_UPDATE_INTERVAL;
                BotOwner.AIData.SetPosToVoxel(botPosition);

                var lastVoxel = CurrentVoxel;
                CurrentVoxel = BotOwner.VoxelesPersonalData.CurVoxel;

                if (lastVoxel != CurrentVoxel)
                {
                    AllDoors.Clear();
                    if (CurrentVoxel != null)
                    {
                        //Logger.LogDebug($"{CurrentVoxel.DoorLinks.Count} doors in voxel");
                        foreach (var link in CurrentVoxel.DoorLinks)
                        {
                            if (IsDoorOpenable(link.Door))
                            {
                                AllDoors.Add(new DoorDataStruct(link));
                            }
                        }
                    }
                    //Logger.LogDebug($"{AllDoors.Count} door data created");
                }

                InteractionDoors.Clear();
                for (int i = 0; i < AllDoors.Count; i++)
                {
                    DoorDataStruct door = AllDoors[i];
                    door.ManualUpdate(botPosition);
                    if (door.InRangeToInteract(door.Door) && door.CanInteractByTime(time))
                    {
                        InteractionDoors.Add(door);
                    }
                    AllDoors[i] = door;
                }
            }
        }

        private static bool IsDoorOpenable(Door door)
        {
            if (!door.enabled || !door.gameObject.activeInHierarchy || !door.Operatable)
            {
                return false;
            }
            //if (!ModDetection.ProjectFikaLoaded &&
            //    GlobalSettingsClass.Instance.General.Doors.DisableAllDoors &&
            //    GameWorldComponent.Instance.Doors.DisableDoor(door))
            //{
            //    return false;
            //}
            return true;
        }

        private float _nextDoorUpdateTime;
    }

    public class DoorFinder(DoorOpener opener) : BotSubClass<DoorOpener>(opener)
    {
        static DoorFinder()
        {
            //debugFindDoors();
        }

        private const float DOORS_INTERACTION_DISTANCE_SQR = 10f * 10f;
        private const float DOOR_UPDATE_INTERVAL = 0.5f;

        public List<DoorData> InteractionDoors { get; } = [];
        public List<DoorData> AllDoors { get; } = [];
        public NavGraphVoxelSimple CurrentVoxel { get; private set; }

        public void UpdateDoors(Vector3 botPosition, Vector3 corner)
        {
            float time = Time.time;
            if (_nextDoorUpdateTime < time)
            {
                _nextDoorUpdateTime = time + DOOR_UPDATE_INTERVAL;
                BotOwner.AIData.SetPosToVoxel(botPosition);

                var lastVoxel = CurrentVoxel;
                CurrentVoxel = BotOwner.VoxelesPersonalData.CurVoxel;

                if (lastVoxel != CurrentVoxel)
                {
                    AllDoors.Clear();
                    if (CurrentVoxel != null)
                    {
                        //Logger.LogDebug($"{CurrentVoxel.DoorLinks.Count} doors in voxel");
                        foreach (var link in CurrentVoxel.DoorLinks)
                            if (IsDoorOpenable(link.Door))
                                AllDoors.Add(new DoorData(link));
                    }
                    //Logger.LogDebug($"{AllDoors.Count} door data created");
                }

                foreach (DoorData door in AllDoors) door.ManualUpdate(botPosition);

                FindDoorsInRange(DOORS_INTERACTION_DISTANCE_SQR, AllDoors, InteractionDoors);
                //Logger.LogDebug($"{InteractionDoors.Count} interaction doors");

                Vector3 cornerDirNormal = (corner - botPosition).normalized;
                foreach (var door in InteractionDoors)
                    door.DotProduct = Vector3.Dot(door.DirectionNormal, cornerDirNormal);
            }
        }

        private static bool IsDoorOpenable(Door door)
        {
            if (!door.enabled ||
                !door.gameObject.activeInHierarchy ||
                !door.Operatable)
            {
                return false;
            }
            if (!ModDetection.ProjectFikaLoaded &&
                GlobalSettings.General.Doors.DisableAllDoors &&
                GameWorldComponent.Instance.Doors.DisableDoor(door))
            {
                return false;
            }
            return true;
        }

        private static void FindDoorsInRange(float rangeSqr, List<DoorData> doorsToCheck, List<DoorData> result)
        {
            result.Clear();
            foreach (var door in doorsToCheck)
                if (door.CurrentSqrMagnitude <= rangeSqr)
                    result.Add(door);
        }

        private float _nextDoorUpdateTime;
    }
}