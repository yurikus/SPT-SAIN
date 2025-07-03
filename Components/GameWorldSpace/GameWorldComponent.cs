using EFT;
using EFT.Game.Spawning;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                var OtherPlayerData = PlayerComponent.OtherPlayersData.DataHashSet;
                Vector3 PlayerLookDir = PlayerComponent.LookDirection;

                // Add any other AI Controlled players that are in the direction this shot is going
                _tempOtherPlayerCache.AddRange(from Data in OtherPlayerData
                                               let OtherPlayerDirNormal = Data.DistanceData.DirectionNormal
                                               let BotPlayerComponent = Data.PlayerComponent
                                               where BotPlayerComponent != null && BotPlayerComponent.IsAI && BotPlayerComponent.IsActive && Vector3.Dot(OtherPlayerDirNormal, PlayerLookDir) > 0.75f
                                               select Data);

                if (_tempOtherPlayerCache.Count > 0)
                {
                    List<OtherPlayerData> RelevantPlayers = [];
                    RelevantPlayers.AddRange(_tempOtherPlayerCache);
                    _tempOtherPlayerCache.Clear();
                    ActiveBullets.Add(new(Bullet, PlayerComponent, RelevantPlayers));
                }
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
            Vector3 LastPosition = Bullet.StartPosition;
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
                    DebugGizmos.Sphere(BulletPosition, 0.3f, Color.red, true, 3.0f);
                    DebugGizmos.Line(BulletPosition, LastPosition, Color.red, 0.05f, true, 3, true);

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
                    LastPosition = BulletPosition;
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

        public void Update()
        {
            ExtractFinder.ManualUpdate();
            Doors.Update();
            Location.Update();
            findSpawnPointMarkers();
            //SAINBotController.ManualUpdate();
            TickPlayerComponents();
        }

        private const float _Sounds_PlayerCache_Interval = 1f / 30f;
        private const float _Sounds_BotCache_Interval = 1f / 15f;

        private void TickPlayerComponents()
        {
            UnityEngine.Profiling.Profiler.BeginSample("SAIN: TickPlayerComponents");

            HashSet<PlayerComponent> PlayerComponents = PlayerTracker?.AlivePlayerArray;
            if (PlayerComponents == null || PlayerComponents.Count == 0)
                return;

            float CurrentTime = Time.time;
            bool TickPlayerSoundCache = CurrentTime >= _Sounds_PlayerCache_Time;
            if (TickPlayerSoundCache)
            {
                _Sounds_PlayerCache_Time = CurrentTime + _Sounds_PlayerCache_Interval;
            }
            bool TickBotSoundCache = CurrentTime >= _Sounds_BotCache_Time;
            if (TickBotSoundCache)
            {
                _Sounds_BotCache_Time = CurrentTime + _Sounds_BotCache_Interval;
            }

            UnityEngine.Profiling.Profiler.BeginSample("SAIN: Update Player Components");

            foreach (PlayerComponent Player in PlayerComponents)
            {
                if (Player != null)
                {
                    Player.ManualUpdate();
                    if (TickPlayerSoundCache)
                    {
                        UpdatePlayerSoundCache(Player);
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("SAIN: Update SAIN Bots");

            if (TickBotSoundCache)
            {
                foreach (var playerComponent in PlayerComponents)
                {
                    BotComponent Bot = playerComponent?.BotComponent;
                    if (Bot != null && Bot.BotOwner?.BotState == EBotState.Active)
                    {
                        Bot.Hearing.SoundInput.ProcessAISoundCache();
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void UpdatePlayerSoundCache(PlayerComponent Player)
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

            Instance = null;
            GameWorld.OnDispose -= DestroyComponent;
            Destroy(this);
            //Logger.LogDebug("SAIN GameWorld Destroyed.");
        }

        private float _Sounds_PlayerCache_Time;
        private float _Sounds_BotCache_Time;
    }
}