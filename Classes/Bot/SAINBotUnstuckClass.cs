using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Debug;

public class SAINBotUnstuckClass : BotComponentClassBase
{
    public SAINBotUnstuckClass(BotComponent sain) : base(sain)
    {
        TickRequirement = ESAINTickState.OnlyBotActive;
    }

    public override void Init()
    {
        PathController = BotOwner.Mover.ActualPathController;
        DontUnstuckMe = DontUnstuckTheseTypes.Contains(Bot.Info.Profile.WildSpawnType);
        base.Init();
    }

    private void CheckIfPositionChanged()
    {
        if (CheckPositionTimer < Time.time)
        {
            CheckPositionTimer = Time.time + 0.5f;

            bool botChangedPositionLast = BotHasChangedPosition;

            const float DistThreshold = 0.1f;
            BotHasChangedPosition = (LastPos - Bot.Position).sqrMagnitude > DistThreshold * DistThreshold;

            if (botChangedPositionLast && !BotHasChangedPosition)
            {
                TimeStartedChangingPosition = Time.time;
            }
            else if (BotHasChangedPosition)
            {
                TimeStartedChangingPosition = 0f;
            }

            LastPos = Bot.Position;
        }
    }
    private void teleport(Vector3 position)
    {
        Player.Teleport(position + Vector3.up * 0.25f);
#if DEBUG
        if (SAINPlugin.DebugMode)
            Logger.LogDebug($"{BotOwner.name} has teleported because they were stuck after vaulting, and no human players are visible to them, and no human players are close.");
#endif
        BotOwner.Mover?.Stop();
        BotOwner.Mover?.RecalcWay();
    }

    private bool isHumanVisible() => Bot.EnemyController.HumanEnemyInLineofSight;

    private bool isHumanClose()
    {
        bool closeHuman = false;
        var allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList;
        foreach (var player in allPlayers)
        {
            if (player != null
                && !player.IsAI
                && player.HealthController.IsAlive
                && (player.Position - Bot.Position).sqrMagnitude < 50f * 50f)
            {
                closeHuman = true;
                break;
            }
        }
        return closeHuman;
    }

    private bool _botStuckAfterVault;


    private bool tryVault()
    {
        if (!Bot.Info.FileSettings.Move.VAULT_UNSTUCK_TOGGLE || !GlobalSettingsClass.Instance.Move.VAULT_UNSTUCK_TOGGLE)
        {
            return false;
        }
        if (Bot.Mover.TryVault())
        {
            _botVaultedTime = Time.time;
            return true;
        }
        return false;
    }

    private float _nextVaultCheckTime;
    private bool DontUnstuckMe;

    private static readonly List<WildSpawnType> DontUnstuckTheseTypes = new()
    {
        WildSpawnType.marksman,
        WildSpawnType.shooterBTR,
    };

    private void checkResetPathFromVault()
    {
        if (_botVaulted
            && !_botStuckAfterVault
            && _botVaultedTime + 1f < Time.time)
        {
            _botVaulted = false;
            Bot.Mover.RecalcPath();
        }
    }

    private bool _botVaulted;
    private float _botVaultedTime;

    private void tryAutoVault()
    {
        if (!Bot.Info.FileSettings.Move.VAULT_TOGGLE || !GlobalSettingsClass.Instance.Move.VAULT_TOGGLE)
        {
            return;
        }
        if (_nextVaultCheckTime < Time.time
            && (BotOwner?.Mover?.IsMoving == true || Bot.Mover.Moving))
        {
            float timeAdd;
            Vector3 lookDir = Player.LookDirection.normalized;
            Vector3 targetDir = BotOwner.Mover.NormDirCurPoint;
            if (Vector3.Dot(lookDir, targetDir) > 0.85f && tryVault())
            {
                _botVaulted = true;
                timeAdd = 2f;
            }
            else
            {
                timeAdd = 0.5f;
            }
            _nextVaultCheckTime = Time.time + timeAdd;
        }
    }

    public override void ManualUpdate()
    {
        if (!DontUnstuckMe && !Bot.BotActivation.BotInStandBy)
        {
            startCoroutine();
        }
        else if (botUnstuckCoroutine != null)
        {
            Bot.StopCoroutine(botUnstuckCoroutine);
        }
        base.ManualUpdate();
    }

    private void startCoroutine()
    {
        if (botUnstuckCoroutine == null)
        {
            botUnstuckCoroutine = Bot.StartCoroutine(botUnstuck());
        }
    }

    private Coroutine botUnstuckCoroutine;

    private IEnumerator botUnstuck()
    {
        while (true)
        {
            //if (Bot.BotActive
            //&& !Bot.GameEnding)
            //{
            //    tryAutoVault();
            //    checkResetPathFromVault();
            //}
            yield return null;
        }
    }

    private const float MinDistance = 100f;
    private const float MaxDistance = 300f;
    private const float PathLengthCoef = 1.25f;
    private const float MinDistancePathLength = MinDistance * PathLengthCoef;

    //private Coroutine TeleportCoroutine;

    private IEnumerator CheckIfTeleport()
    {
        bool shallTeleport = true;
        var humanPlayers = GetHumanPlayers();

        Vector3? teleportDestination = null;

        const float minTeleDist = 1f;
        if (BotOwner.Mover.HasPathAndNoComplete)
        {
            for (int i = PathController.CurPath.CurIndex; i < PathController.CurPath.Length - 1; i++)
            {
                Vector3 corner = PathController.CurPath.GetPoint(i);
                Vector3 cornerDirection = corner - Bot.Position;
                float cornerDistance = cornerDirection.sqrMagnitude;
                if (cornerDirection.sqrMagnitude >= minTeleDist * minTeleDist)
                {
                    teleportDestination = new Vector3?(corner);
                    break;
                }
                yield return null;
            }
        }

        Vector3 botPosition = Bot.Position;

        if (teleportDestination != null)
        {
            var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
            if (allPlayers != null)
            {
                foreach (var player in allPlayers)
                {
                    if (ShallCheckPlayer(player))
                    {
                        if (!BotIsStuck)
                        {
                            shallTeleport = false;
                            yield break;
                        }

                        Vector3 playerPosition = player.Position;

                        // Makes sure the bot isn't too close to a human for them to hear
                        float sqrMag = (playerPosition - botPosition).sqrMagnitude;
                        if (sqrMag < MinDistance * MinDistance)
                        {
                            shallTeleport = false;
                            break;
                        }

                        // Checks the max distance to do a path calculation
                        if (sqrMag < MaxDistance * MaxDistance)
                        {
                            NavMeshPath path = CalcPath(botPosition, playerPosition, out float pathLength);
                            if (CheckPathLength(playerPosition, path, pathLength) == false)
                            {
                                shallTeleport = false;
                                break;
                            }
                        }

                        // Check next player on the next frame
                        yield return null;
                    }
                }
            }
        }

        IsTeleporting = BotIsStuck && shallTeleport && teleportDestination != null;

        if (IsTeleporting)
        {
            Teleport(teleportDestination.Value + Vector3.up * 0.25f);
            float distance = (teleportDestination.Value - botPosition).magnitude;
            Logger.LogDebug($"Teleporting stuck bot: [{Player.name}] [{distance}] meters to the next corner they are trying to go to");
            _botStuckAfterVault = false;
            BotIsStuck = false;
        }

        yield return null;
    }

    private bool IsTeleporting;

    private bool ShallCheckPlayer(Player player)
    {
        if (Player == null || Player.HealthController == null || Player.AIData == null)
        {
            return false;
        }
        return Player.HealthController.IsAlive == true && Player.AIData.IsAI == false;
    }

    private void Teleport(Vector3 position)
    {
        if (teleportTimer < Time.time)
        {
            teleportTimer = Time.time + 3f;
            Player.Teleport(position);
        }
    }

    private float teleportTimer;

    public PathControllerClass PathController { get; private set; }

    private static NavMeshPath CalcPath(Vector3 start, Vector3 end, out float pathLength)
    {
        if (PathToPlayer == null)
        {
            PathToPlayer = new NavMeshPath();
        }
        if (NavMesh.SamplePosition(end, out NavMeshHit hit, 1f, -1))
        {
            PathToPlayer.ClearCorners();
            if (NavMesh.CalculatePath(start, hit.position, -1, PathToPlayer))
            {
                pathLength = PathToPlayer.CalculatePathLength();
                return PathToPlayer;
            }
        }
        pathLength = 0f;
        return null;
    }

    private static bool CheckPathLength(Vector3 end, NavMeshPath path, float pathLength)
    {
        if (path == null)
        {
            return false;
        }
        if (path.status == NavMeshPathStatus.PathPartial)
        {
            Vector3 lastCorner = path.corners[path.corners.Length - 1];
            float sqrMag = (lastCorner - end).magnitude;
            float combinedLength = sqrMag + pathLength;
            if (combinedLength < MinDistancePathLength)
            {
                return false;
            }
        }
        if (path.status == NavMeshPathStatus.PathComplete && pathLength < MinDistancePathLength)
        {
            return false;
        }
        return path.status != NavMeshPathStatus.PathInvalid;
    }

    private List<Player> GetHumanPlayers()
    {
        HumanPlayers.Clear();
        var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
        if (allPlayers != null)
        {
            foreach (var player in allPlayers)
            {
                if (player != null && player.AIData.IsAI == false && player.HealthController.IsAlive)
                {
                    HumanPlayers.Add(player);
                }
            }
        }
        return HumanPlayers;
    }

    private static NavMeshPath PathToPlayer;
    private List<Player> HumanPlayers = new();
    public float TimeSinceStuck => Time.time - TimeStuck;
    public float TimeStuck { get; private set; }

    private float CheckPositionTimer = 0f;

    private Vector3 LastPos = Vector3.zero;

    public float TimeSpentNotMoving => Time.time - TimeStartedChangingPosition;

    public float TimeStartedChangingPosition { get; private set; }

    public bool BotIsStuck { get; private set; }

    public bool BotHasChangedPosition { get; private set; }

}