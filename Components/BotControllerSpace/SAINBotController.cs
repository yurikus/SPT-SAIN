using Comfort.Common;
using EFT;
using EFT.EnvironmentEffect;
using HarmonyLib;
using SAIN.BotController.Classes;
using SAIN.Components.BotController;
using SAIN.Components.BotController.PeacefulActions;
using SAIN.Components.BotControllerSpace.Classes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Models.Structs;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components
{
    public class GrenadeController : SAINControllerBase
    {
        public event Action<Grenade, float> OnGrenadeCollision;

        public event Action<Grenade, Vector3, string> OnGrenadeThrown;

        public event Action<Grenade, Vector3> OnGrenadeDangerUpdated;

        public GrenadeController(SAINBotController controller) : base(controller)
        {
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

        public void Subscribe(BotEventHandler eventHandler)
        {
            eventHandler.OnGrenadeThrow += GrenadeThrown;
            eventHandler.OnGrenadeExplosive += GrenadeExplosion;
        }

        public void UnSubscribe(BotEventHandler eventHandler)
        {
            eventHandler.OnGrenadeThrow -= GrenadeThrown;
            eventHandler.OnGrenadeExplosive -= GrenadeExplosion;
        }

        public void GrenadeCollided(Grenade grenade, float maxRange)
        {
            OnGrenadeCollision?.Invoke(grenade, maxRange);
        }

        private void GrenadeExplosion(Vector3 explosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (!Singleton<BotEventHandler>.Instantiated || playerProfileID == null)
            {
                return;
            }
            Player player = GameWorldInfo.GetAlivePlayer(playerProfileID);
            if (player != null)
            {
                if (!isSmoke)
                {
                    RegisterGrenadeExplosionForSAINBots(explosionPosition, player, playerProfileID, 200f);
                }
                else
                {
                    RegisterGrenadeExplosionForSAINBots(explosionPosition, player, playerProfileID, 50f);

                    float radius = smokeRadius * HelpersGClass.SMOKE_GRENADE_RADIUS_COEF;
                    Vector3 position = player.Position;

                    if (BotController.DefaultController != null)
                        foreach (var keyValuePair in BotController.DefaultController.Groups())
                            foreach (BotsGroup botGroupClass in keyValuePair.Value.GetGroups(true))
                                botGroupClass.AddSmokePlace(explosionPosition, smokeLifeTime, radius, position);
                }
            }
        }

        private void RegisterGrenadeExplosionForSAINBots(Vector3 explosionPosition, Player player, string playerProfileID, float range)
        {
            // Play a sound with the input range.
            Singleton<BotEventHandler>.Instance?.PlaySound(player, explosionPosition, range, AISoundType.gun);

            // We dont want bots to think the grenade explosion was a place they heard an enemy, so set this manually.
            foreach (var bot in Bots.Values)
            {
                if (bot?.BotActive == true)
                {
                    float distance = (bot.Position - explosionPosition).magnitude;
                    if (distance < range)
                    {
                        Enemy enemy = bot.EnemyController.GetEnemy(playerProfileID, true);
                        if (enemy != null)
                        {
                            float dispersion = distance / 10f;
                            Vector3 random = UnityEngine.Random.onUnitSphere * dispersion;
                            random.y = 0;
                            Vector3 estimatedThrowPosition = enemy.EnemyPosition + random;

                            SAINHearingReport report = new() {
                                position = estimatedThrowPosition,
                                soundType = SAINSoundType.GrenadeExplosion,
                                placeType = EEnemyPlaceType.Hearing,
                                isDanger = distance < 100f || enemy.InLineOfSight,
                                shallReportToSquad = true,
                            };
                            enemy.Hearing.SetHeard(report);
                        }
                    }
                }
            }
        }

        private void GrenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
        {
            if (grenade == null)
            {
                return;
            }

            Player player = GameWorldInfo.GetAlivePlayer(grenade.ProfileId);
            if (player == null)
            {
                Logger.LogError($"Player Null from ID {grenade.ProfileId}");
                return;
            }
            if (!player.HealthController.IsAlive)
            {
                return;
            }

            Vector3 dangerPoint = Vector.DangerPoint(position, force, mass);
            grenade.DestroyEvent += grenadeDestroyed;
            Singleton<BotEventHandler>.Instance?.PlaySound(player, grenade.transform.position, 20f, AISoundType.gun);
            OnGrenadeThrown?.Invoke(grenade, dangerPoint, grenade.ProfileId);
            if (GameWorldComponent.TryGetPlayerComponent(player, out PlayerComponent playerComponent))
            {
                List<PlayerComponent> RelevantPlayers = [];
                foreach (var otherPlayer in playerComponent.OtherPlayersData.DataDictionary.Values)
                {
                    if (otherPlayer.DistanceData.Distance < 125f && otherPlayer.PlayerComponent.IsSAINBot)
                    {
                        RelevantPlayers.Add(otherPlayer.PlayerComponent);
                    }
                }
                ActiveGrenades.Add(grenade, RelevantPlayers);
                BotController.StartCoroutine(GrenadeTracker(grenade, playerComponent, RelevantPlayers, dangerPoint));
            }
        }

        public readonly Dictionary<Throwable, List<PlayerComponent>> ActiveGrenades = [];

        private void grenadeDestroyed(Throwable Grenade)
        {
            ActiveGrenades.Remove(Grenade);
        }

        private IEnumerator GrenadeTracker(Grenade Grenade, PlayerComponent Thrower, List<PlayerComponent> RelevantPlayers, Vector3 DangerPoint)
        {
            Rigidbody Rigidbody = (Rigidbody)_rigidBodyField.GetValue(Grenade);

            if (Rigidbody == null)
            {
                Logger.LogError("RigidBody Null");
                yield break;
            }
            while (Grenade != null && BotController != null && Rigidbody != null)
            {
                Vector3 Velocity = Rigidbody.velocity;
                if (Velocity.magnitude < 0.1f)
                {
                    OnGrenadeDangerUpdated?.Invoke(Grenade, Grenade.transform.position);
                }
                else if (Velocity.y < 0)
                {
                    Vector3 VelocityNormal = Velocity.normalized;
                    if (Vector3.Dot(VelocityNormal, Vector3.down) > 0.5f &&
                        Physics.Raycast(Grenade.transform.position, VelocityNormal, out RaycastHit Hit, 5, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        OnGrenadeDangerUpdated?.Invoke(Grenade, Hit.point);
                    }
                }
                yield return null;
            }
        }

        static GrenadeController()
        {
            _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
        }

        private static FieldInfo _rigidBodyField;
    }

    public class SAINBotController : MonoBehaviour
    {
        public static SAINBotController Instance { get; private set; }

        public BotDictionary Bots => BotSpawnController.Bots;
        public GameWorld GameWorld => SAINGameWorld.GameWorld;
        public IBotGame BotGame => Singleton<IBotGame>.Instance;

        public BotEventHandler BotEventHandler {
            get
            {
                if (_eventHandler == null)
                {
                    _eventHandler = Singleton<BotEventHandler>.Instance;
                    if (_eventHandler != null)
                    {
                        GrenadeController.Subscribe(_eventHandler);
                    }
                }
                return _eventHandler;
            }
        }

        private BotEventHandler _eventHandler;

        public GameWorldComponent SAINGameWorld { get; private set; }
        public BotsController DefaultController { get; set; }

        public BotSpawner BotSpawner {
            get
            {
                return _spawner;
            }
            set
            {
                BotSpawnController.Subscribe(value);
                _spawner = value;
            }
        }

        private BotSpawner _spawner;
        public GrenadeController GrenadeController { get; private set; }
        public BotJobsClass BotJobs { get; private set; }
        public BotExtractManager BotExtractManager { get; private set; }
        public TimeClass TimeVision { get; private set; }
        public BotController.SAINWeatherClass WeatherVision { get; private set; }
        public BotSpawnController BotSpawnController { get; private set; }
        public BotSquads BotSquads { get; private set; }
        public BotHearingClass BotHearing { get; private set; }
        public BotPeacefulActionController PeacefulActions { get; private set; }

        public void PlayerEnviromentChanged(string profileID, IndoorTrigger trigger)
        {
            SAINGameWorld.PlayerTracker.GetPlayerComponent(profileID)?.AIData.PlayerLocation.UpdateEnvironment(trigger);
        }

        public void Awake()
        {
            Instance = this;
            SAINGameWorld = this.GetComponent<GameWorldComponent>();
            BotSpawnController = new BotSpawnController(this);
            BotExtractManager = new BotExtractManager(this);
            TimeVision = new TimeClass(this);
            WeatherVision = new BotController.SAINWeatherClass(this);
            BotSquads = new BotSquads(this);
            BotHearing = new BotHearingClass(this);
            PeacefulActions = new BotPeacefulActionController(this);
            BotJobs = new BotJobsClass(this);
            GrenadeController = new GrenadeController(this);
            GameWorld.OnDispose += Dispose;
        }

        public void Start()
        {
            //PeacefulActions.Init();
        }

        public void ManualUpdate()
        {
            BotSpawnController.Update();
            BotExtractManager.Update();
            TimeVision.Update();
            WeatherVision.Update();
            BotSquads.Update();

            HashSet<BotComponent> BotsArray = BotSpawnController?.SAINBots;
            if (BotsArray != null && BotsArray.Count > 0)
            {
                BotJobs.UpdateVisionForBots(BotsArray);
                foreach (BotComponent BotComponent in BotsArray)
                {
                    BotComponent?.ManualUpdate();
                }
            }
        }

        public void BotDeath(BotOwner bot)
        {
            if (bot?.GetPlayer != null && bot.IsDead)
            {
                DeadBots.Add(bot.GetPlayer);
            }
        }

        public List<Player> DeadBots { get; private set; } = new List<Player>();

        public List<BotDeathObject> DeathObstacles { get; private set; } = new List<BotDeathObject>();

        private readonly List<int> IndexToRemove = new();

        public void AddNavObstacles()
        {
            if (DeadBots.Count > 0)
            {
                const float ObstacleRadius = 1.5f;

                for (int i = 0; i < DeadBots.Count; i++)
                {
                    var bot = DeadBots[i];
                    if (bot == null || bot.GetPlayer == null)
                    {
                        IndexToRemove.Add(i);
                        continue;
                    }
                    bool enableObstacle = true;
                    Collider[] players = Physics.OverlapSphere(bot.Position, ObstacleRadius, LayerMaskClass.PlayerMask);
                    foreach (var p in players)
                    {
                        if (p == null) continue;
                        if (p.TryGetComponent<Player>(out var player))
                        {
                            if (player.IsAI && player.HealthController.IsAlive)
                            {
                                enableObstacle = false;
                                break;
                            }
                        }
                    }
                    if (enableObstacle)
                    {
                        if (bot != null && bot.GetPlayer != null)
                        {
                            var obstacle = new BotDeathObject(bot);
                            obstacle.Activate(ObstacleRadius);
                            DeathObstacles.Add(obstacle);
                        }
                        IndexToRemove.Add(i);
                    }
                }

                foreach (var index in IndexToRemove)
                {
                    DeadBots.RemoveAt(index);
                }

                IndexToRemove.Clear();
            }
        }

        private void UpdateObstacles()
        {
            if (DeathObstacles.Count > 0)
            {
                for (int i = 0; i < DeathObstacles.Count; i++)
                {
                    var obstacle = DeathObstacles[i];
                    if (obstacle?.TimeSinceCreated > 30f)
                    {
                        obstacle?.Dispose();
                        IndexToRemove.Add(i);
                    }
                }

                foreach (var index in IndexToRemove)
                {
                    DeathObstacles.RemoveAt(index);
                }

                IndexToRemove.Clear();
            }
        }

        public List<string> Groups = new();

        public void OnDestroy()
        {
        }

        public void Dispose()
        {
            try
            {
                GameWorld.OnDispose -= Dispose;
                StopAllCoroutines();
                BotJobs.Dispose();
                BotSpawnController.UnSubscribe();
                PeacefulActions.Dispose();

                if (BotEventHandler != null)
                {
                    GrenadeController.UnSubscribe(BotEventHandler);
                }

                if (Bots != null && Bots.Count > 0)
                {
                    foreach (var bot in Bots.Values)
                    {
                        bot?.Dispose();
                    }
                }

                Bots?.Clear();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose SAIN BotController Error: {ex}");
            }

            Destroy(this);
        }

        public bool GetSAIN(BotOwner botOwner, out BotComponent bot)
        {
            bot = BotSpawnController.GetSAIN(botOwner);
            return bot != null;
        }
    }

    public class BotDeathObject
    {
        public BotDeathObject(Player player)
        {
            Player = player;
            NavMeshObstacle = player.gameObject.AddComponent<NavMeshObstacle>();
            NavMeshObstacle.carving = false;
            NavMeshObstacle.enabled = false;
            Position = player.Position;
            TimeCreated = Time.time;
        }

        public void Activate(float radius = 2f)
        {
            if (NavMeshObstacle != null)
            {
                NavMeshObstacle.enabled = true;
                NavMeshObstacle.carving = true;
                NavMeshObstacle.radius = radius;
            }
        }

        public void Dispose()
        {
            if (NavMeshObstacle != null)
            {
                NavMeshObstacle.carving = false;
                NavMeshObstacle.enabled = false;
                GameObject.Destroy(NavMeshObstacle);
            }
        }

        public NavMeshObstacle NavMeshObstacle { get; private set; }
        public Player Player { get; private set; }
        public Vector3 Position { get; private set; }
        public float TimeCreated { get; private set; }
        public float TimeSinceCreated => Time.time - TimeCreated;
        public bool ObstacleActive => NavMeshObstacle.carving;
    }
}