using EFT;
using EFT.Game.Spawning;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.RotationController;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.Types.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace SAIN.Components
{
    public class GameWorldComponent : MonoBehaviour
    {
        protected readonly struct BulletData(EftBulletClass inBullet, PlayerComponent InOwner, List<OtherPlayerData> InRelevantPlayers)
        {
            public readonly EftBulletClass Bullet = inBullet;
            public readonly PlayerComponent Owner = InOwner;
            public readonly List<OtherPlayerData> RelevantPlayers = InRelevantPlayers;
        }

        public static bool TryGetPlayerComponent(IPlayer Player, out PlayerComponent PlayerComponent)
        {
            if (Player == null)
            {
                Logger.LogError("Player Null");
                PlayerComponent = null;
                return false;
            }
            PlayerSpawnTracker PlayerTracker = Instance?.PlayerTracker;
            if (PlayerTracker == null)
            {
                Logger.LogError("GameWorld Component Null, can't get Player Component");
                PlayerComponent = null;
                return false;
            }
            PlayerComponent = PlayerTracker.AlivePlayersDictionary.GetPlayerComponent(Player);
            return PlayerComponent != null;
        }

        public void RegisterShot(Player Player, EftBulletClass Bullet, Item Weapon)
        {
            if (TryGetPlayerComponent(Player, out PlayerComponent PlayerComponent))
            {
                StartCoroutine(TrackBullet(PlayerComponent, Bullet));
                //var OtherPlayerData = PlayerComponent.OtherPlayersData.DataHashSet;
                //Vector3 PlayerLookDir = PlayerComponent.LookDirection;
                //
                //// Add any other AI Controlled players that are in the direction this shot is going
                //_tempOtherPlayerCache.AddRange(from Data in OtherPlayerData
                //                               let OtherPlayerDirNormal = Data.DistanceData.DirectionNormal
                //                               let BotPlayerComponent = Data.PlayerComponent
                //                               where BotPlayerComponent != null && BotPlayerComponent.IsAI && BotPlayerComponent.IsActive && Vector3.Dot(OtherPlayerDirNormal, PlayerLookDir) > 0.75f
                //                               select Data);
                //
                //if (_tempOtherPlayerCache.Count > 0)
                //{
                //    List<OtherPlayerData> RelevantPlayers = [];
                //    RelevantPlayers.AddRange(_tempOtherPlayerCache);
                //    _tempOtherPlayerCache.Clear();
                //    ActiveBullets.Add(new(Bullet, PlayerComponent, RelevantPlayers));
                //}
            }
        }

        private readonly List<OtherPlayerData> _tempOtherPlayerCache = [];

        private void UpdateActiveBullets(List<EftBulletClass> bullets)
        {
            int bulletCount = bullets.Count;
            if (bulletCount == 0)
                return;
            for (int i = 0; i < bulletCount; i++)
            {
                EftBulletClass bullet = bullets[i];
                if (bullet == null || bullet.BulletState != EftBulletClass.EBulletState.Flying)
                {
                    bullets.RemoveAt(i);
                    continue;
                }
            }
        }

        private IEnumerator TrackBullet(PlayerComponent Player, EftBulletClass Bullet)
        {
            //Vector3 LastPosition = Bullet.StartPosition;
            var OtherPlayerData = Player.OtherPlayersData.DataDictionary;
            Vector3 PlayerLookDir = Player.LookDirection;

            List<OtherPlayerData> PlayersToCheck = [];
            PlayersToCheck.AddRange(from Data in OtherPlayerData
                                    let OtherPlayerDirNormal = Data.Value.DistanceData.DirectionNormal
                                    let PlayerComponent = Data.Value.PlayerComponent
                                    where PlayerComponent?.IsAI == true && PlayerComponent.IsActive && Vector3.Dot(OtherPlayerDirNormal, PlayerLookDir) > 0.75f
                                    select Data.Value);

            const float MaxFlyByDistSqr = 10 * 10;

            if (PlayersToCheck.Count > 0)
            {
                while (!Bullet.IsShotFinished && Player?.IsActive == true && PlayersToCheck.Count > 0)
                {
                    Vector3 BulletPosition = Bullet.CurrentPosition;
                    //DebugGizmos.Sphere(BulletPosition, 0.3f, Color.red, true, 3.0f);
                    //DebugGizmos.Line(BulletPosition, LastPosition, Color.red, 0.05f, true, 3, true);

                    for (int i = PlayersToCheck.Count - 1; i >= 0; i--)
                    {
                        OtherPlayerData Data = PlayersToCheck[i];
                        if (Data?.PlayerComponent?.IsActive == false)
                        {
                            PlayersToCheck.RemoveAt(i);
                            continue;
                        }
                        Vector3 PlayerPosition = Data.DistanceData.Position;
                        float BulletDistSqr = (PlayerPosition - BulletPosition).sqrMagnitude;
                        if (BulletDistSqr < MaxFlyByDistSqr)
                        {
                            Data.PlayerComponent.RegisterFlyBy(Player, Bullet);
                            PlayersToCheck.RemoveAt(i);
                            continue;
                        }
                    }
                    //LastPosition = BulletPosition;
                    yield return null;
                }
            }
        }

        private List<BulletData> ActiveBullets { get; } = [];

        public static GameWorldComponent Instance { get; private set; }
        public GameWorld GameWorld { get; private set; }
        public PlayerSpawnTracker PlayerTracker { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
        public Extract.ExtractFinderComponent ExtractFinder { get; private set; }
        public DoorHandler Doors { get; private set; }
        public LocationClass Location { get; private set; }
        public SpawnPointMarker[] SpawnPointMarkers { get; private set; }
        public JobManager JobManager { get; private set; }

        public static float WorldTickDeltaTime { get; private set; }
        public void WorldTickLoop(float deltaTime, GameWorld gameWorld)
        {
            WorldTickDeltaTime = deltaTime;
            ManualUpdate();
            SAINBotController.ManualUpdate();
        }

        protected void ManualUpdate()
        {
            if (_activated)
            {
                ExtractFinder.ManualUpdate();
                Doors.Update();
                Location.Update();
                findSpawnPointMarkers();
                HashSet<PlayerComponent> players = PlayerTracker?.AlivePlayerArray;
                if (players != null && players.Count > 0)
                {
                    TickPlayerComponents(players, Time.time);
                    TickSoundCaches(players, Time.time);
                }
            }
        }

        private IEnumerator CalcPathsJobs()
        {
            while (true)
            {
                _enemies.Clear();
                HashSet<BotComponent> bots = SAINBotController?.BotSpawnController?.SAINBots;
                if (bots != null && bots.Count > 0)
                {
                    foreach (BotComponent bot in bots)
                    {
                        if (bot != null)
                        {
                            foreach (Enemy enemy in bot.EnemyController.EnemiesArray)
                            {
                                if (enemy != null)
                                {
                                    _enemies.Add(enemy);
                                }
                            }
                        }
                    }
                }
                int count = _enemies.Count;
                if (count > 0)
                {
                    NavJob = new NavMeshPathQuerryJob(_enemies, _dataList);
                    NavJobHandle = NavJob.Schedule(count, 2, new JobHandle());
                    yield return null;
                    while (!NavJobHandle.IsCompleted)
                    {
                        Logger.LogDebug("NavJobHandle not complete");
                        yield return null;
                    }
                    NavJobHandle.Complete();
                    foreach (var data in NavJob.Output)
                    {
                        PathQueryStatus Status = data.EndFindPath(out int pathLength);
                        Logger.LogDebug($"{Status} :: {pathLength}");
                        NativeArray<PolygonId> polygonPath = new(pathLength, Allocator.Temp);
                        data.GetPathResult(new NativeSlice<PolygonId>(polygonPath));
                        // Convert polygons to waypoints (midpoint of portal between polys)
                        for (int i = 0; i < pathLength - 1; i++)
                        {
                            if (data.GetPortalPoints(polygonPath[i], polygonPath[i + 1], out Vector3 left, out Vector3 right))
                            {
                                Vector3 center = (left + right) * 0.5f;
                                DebugGizmos.Line(left, right, Color.cyan, 5f, true, 0.25f);
                                DebugGizmos.Line(center, center + Vector3.up, Color.red, 5f, true, 0.25f);
                                Logger.LogDebug("Portal center at: " + center);
                            }
                        }
                        polygonPath.Dispose();
                        data.Dispose();
                    }
                    NavJob.Dispose();
                }
                yield return null;
            }
        }

        private List<Enemy> _enemies = [];
        private List<NavMeshQueryDataEnemy> _dataList = [];
        private NavMeshPathQuerryJob NavJob;
        private JobHandle NavJobHandle;

        private const float _Sounds_PlayerCache_Interval = 1f / 30f;
        private const float _Sounds_BotCache_Interval = 1f / 15f;

        protected void TickPlayerComponents(HashSet<PlayerComponent> PlayerComponents, float CurrentTime)
        {
            foreach (PlayerComponent Player in PlayerComponents)
            {
                Player?.ManualUpdate();
            }
        }

        protected void TickSoundCaches(HashSet<PlayerComponent> PlayerComponents, float CurrentTime)
        {
            if (_Sounds_PlayerCache_Time < CurrentTime)
            {
                _Sounds_PlayerCache_Time = CurrentTime + _Sounds_PlayerCache_Interval;

                foreach (PlayerComponent Player in PlayerComponents)
                {
                    if (Player != null)
                    {
                        UpdatePlayerSoundCache(Player);
                    }
                }
            }

            if (_Sounds_BotCache_Time < CurrentTime)
            {
                _Sounds_BotCache_Time = CurrentTime + _Sounds_BotCache_Interval;

                foreach (var playerComponent in PlayerComponents)
                {
                    BotComponent Bot = playerComponent?.BotComponent;
                    if (Bot != null && Bot.BotOwner?.BotState == EBotState.Active)
                    {
                        Bot.Hearing.SoundInput.ProcessAISoundCache();
                    }
                }
            }
        }

        protected static void UpdatePlayerSoundCache(PlayerComponent Player)
        {
            List<SoundEvent> events = Player.AISoundCachedEvents;
            if (Player.IsActive)
            {
                foreach (OtherPlayerData OtherPlayerData in Player.OtherPlayersData.DataHashSet)
                {
                    PlayerComponent OtherPlayer = OtherPlayerData?.PlayerComponent;
                    if (OtherPlayer != null && OtherPlayer.IsActive && OtherPlayer.IsSAINBot)
                    {
                        BotComponent Bot = OtherPlayer.Person.AIInfo.BotComponent;
                        if (Bot != null && Bot.BotOwner?.BotState == EBotState.Active)
                        {
                            bool InFootstepRadius = OtherPlayerData.IsInHearingRadius_Footsteps;
                            bool InGunfireRadius = OtherPlayerData.IsInHearingRadius_GunFire;
                            float Distance = OtherPlayerData.DistanceData.Distance;
                            foreach (var soundEvent in events)
                            {
                                bool isGunshot = soundEvent.IsGunShot;
                                if (isGunshot && !InGunfireRadius)
                                    continue;
                                if (!isGunshot && !InFootstepRadius)
                                    continue;
                                OtherPlayer.Person.AIInfo.BotComponent?.Hearing.SoundInput.CheckAddSoundToCache(soundEvent, Distance);
                            }
                        }
                    }
                }
            }
            events.Clear();
        }

        private void findSpawnPointMarkers()
        {
            if ((SpawnPointMarkers != null) || (Camera.main == null))
            {
                return;
            }

            SpawnPointMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();

            if (SAINPlugin.DebugMode)
                Logger.LogInfo($"Found {SpawnPointMarkers.Length} spawn point markers");
        }

        public IEnumerable<Vector3> GetAllSpawnPointPositionsOnNavMesh()
        {
            if (SpawnPointMarkers == null)
            {
                return Enumerable.Empty<Vector3>();
            }

            List<Vector3> spawnPointPositions = new();
            foreach (SpawnPointMarker spawnPointMarker in SpawnPointMarkers)
            {
                // Try to find a point on the NavMesh nearby the spawn point
                Vector3? spawnPointPosition = NavMeshHelpers.GetNearbyNavMeshPoint(spawnPointMarker.Position, 2);
                if (spawnPointPosition.HasValue && !spawnPointPositions.Contains(spawnPointPosition.Value))
                {
                    spawnPointPositions.Add(spawnPointPosition.Value);
                }
            }

            return spawnPointPositions;
        }

        protected BotRotationManagerComponent BotRotationManager { get; set; }

        public void Activate(BotsController botsController)
        {
            SAINBotController.DefaultController = botsController;
            SAINBotController.BotSpawner = botsController.BotSpawner;
            BotRotationManager = BotRotationManagerComponent.Create(gameObject, botsController.BotSpawner, PlayerTracker);
            _activated = true;
           // StartCoroutine(CalcPathsJobs());
        }

        private bool _activated = false;

        public void Init(GameWorld gameWorld, SAINBotController sainBotController)
        {
            Instance = this;
            GameWorld = gameWorld;
            if (GameWorld == null)
            {
                Logger.LogWarning("GameWorld Null, cannot Init SAIN Gameworld! Check 2. Disposing Component...");
                DestroyComponent();
                return;
            }

            SAINBotController = sainBotController;
            PlayerTracker = new PlayerSpawnTracker(this);
            Doors = new DoorHandler(this);
            Location = new LocationClass(this);
            ExtractFinder = this.GetOrAddComponent<Extract.ExtractFinderComponent>();
            JobManager = new JobManager(this);
            GameWorld.OnDispose += DestroyComponent;

            try
            {
                EFTCoreSettings.UpdateCoreSettings();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            //Logger.LogDebug("SAIN GameWorld Created.");

            Doors.Init();
            Location.Init();
        }

        public void DestroyComponent()
        {
            Instance = null;
            try
            {
                PlayerTracker?.Dispose();
                Doors?.Dispose();
                Location?.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld Component Class Error: {e}");
            }

            try
            {
                ComponentHelpers.DestroyComponent(SAINBotController);
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld SubComponent Error: {e}");
            }

            StopAllCoroutines();
            Instance = null;
            GameWorld.OnDispose -= DestroyComponent;
            Destroy(this);
            //Logger.LogDebug("SAIN GameWorld Destroyed.");
        }

        private float _Sounds_PlayerCache_Time;
        private float _Sounds_BotCache_Time;
    }
}