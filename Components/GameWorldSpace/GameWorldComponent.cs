using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.Components
{
    public class GameWorldComponent : MonoBehaviour
    {
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
            PlayerComponent = PlayerTracker.AlivePlayers.GetPlayerComponent(Player);
            return PlayerComponent != null;
        }

        public static void RegisterShot(Player Player, EftBulletClass Bullet, Item Weapon)
        {
            //ActiveBullets.Add(Bullet);
            if (TryGetPlayerComponent(Player, out PlayerComponent PlayerComponent))
            {
                Instance.StartCoroutine(TrackBullet(PlayerComponent, Bullet));
            }
        }

        private static IEnumerator TrackBullet(PlayerComponent Player, EftBulletClass Bullet)
        {
            Vector3 LastPosition = Bullet.StartPosition;
            var OtherPlayerData = Player.OtherPlayersData.Datas;
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

        private static List<EftBulletClass> ActiveBullets = [];

        public static GameWorldComponent Instance { get; private set; }
        public GameWorld GameWorld { get; private set; }
        public PlayerSpawnTracker PlayerTracker { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
        public Extract.ExtractFinderComponent ExtractFinder { get; private set; }
        public DoorHandler Doors { get; private set; }
        public LocationClass Location { get; private set; }
        public SpawnPointMarker[] SpawnPointMarkers { get; private set; }

        public void Update()
        {
            UpdateAIHearingEvents();
            Doors?.Update();
            Location?.Update();
            findSpawnPointMarkers();
        }

        private void UpdateAIHearingEvents()
        {
            if (Time.time >= _UpdateSoundEventsTimer)
            {
                _UpdateSoundEventsTimer = Time.time + 0.1f;
                if (PlayerTracker != null)
                {
                    var AlivePlayers = PlayerTracker.AlivePlayers.Values;

                    TriggerSoundEvents(AlivePlayers);
                    
                    UnityEngine.Profiling.Profiler.BeginSample("Process Sounds For Bots");
                    foreach (PlayerComponent Player in AlivePlayers)
                    {
                        Player?.Person?.AIInfo?.BotComponent?.Hearing.SoundInput.ProcessAISoundCache();
                    }
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
        }

        private static void TriggerSoundEvents(Dictionary<string, PlayerComponent>.ValueCollection AlivePlayers)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Player Sound Event Triggering");
            foreach (PlayerComponent Player in AlivePlayers)
            {
                if (Player != null)
                {
                    var events = Player.AISoundCachedEvents;
                    if (Player.IsActive)
                    {
                        foreach (var OtherPlayerData in Player.OtherPlayersData.Datas.Values)
                        {
                            PlayerComponent OtherPlayer = OtherPlayerData?.PlayerComponent;
                            if (OtherPlayer != null && OtherPlayer.IsActive && OtherPlayer.IsSAINBot)
                            {
                                float Distance = OtherPlayerData.DistanceData.Distance;
                                foreach (var soundEvent in events)
                                {
                                    OtherPlayer.Person.AIInfo.BotComponent.Hearing.SoundInput.CheckAddSoundToCache(soundEvent, Distance);
                                }
                            }
                        }
                    }
                    events.Clear();
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private float _UpdateSoundEventsTimer;

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

        public void Awake()
        {
            Instance = this;
            GameWorld = this.GetComponent<GameWorld>();
            if (GameWorld == null)
            {
                Logger.LogWarning($"GameWorld is null from GetComponent");
            }
            StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            yield return getGameWorld();

            if (GameWorld == null)
            {
                Logger.LogWarning("GameWorld Null, cannot Init SAIN Gameworld! Check 2. Disposing Component...");
                Dispose();
                yield break;
            }

            PlayerTracker = new PlayerSpawnTracker(this);
            SAINBotController = this.GetOrAddComponent<SAINBotController>();
            Doors = new DoorHandler(this);
            Location = new LocationClass(this);
            ExtractFinder = this.GetOrAddComponent<Extract.ExtractFinderComponent>();
            GameWorld.OnDispose += Dispose;

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

        private IEnumerator getGameWorld()
        {
            if (GameWorld != null)
            {
                yield break;
            }
            if (GameWorld == null)
            {
                yield return new WaitForEndOfFrame();
                GameWorld = findGameWorld();
                if (GameWorld != null)
                {
                    Logger.LogWarning("Found GameWorld at EndOfFrame");
                    yield break;
                }
            }
            for (int i = 0; i < 30; i++)
            {
                if (GameWorld == null)
                {
                    yield return null;
                    GameWorld = findGameWorld();
                }
                if (GameWorld != null)
                {
                    break;
                }
            }
        }

        private GameWorld findGameWorld()
        {
            GameWorld gameWorld = this.GetComponent<GameWorld>();
            if (gameWorld == null)
            {
                gameWorld = Singleton<GameWorld>.Instance;
            }
            return gameWorld;
        }

        public void Dispose()
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
            GameWorld.OnDispose -= Dispose;
            Destroy(this);
            //Logger.LogDebug("SAIN GameWorld Destroyed.");
        }
    }
}