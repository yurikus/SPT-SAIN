using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public class VisionRaycastJob : BotManagerBase
    {
        private const float VISION_UPDATE_INTERVAL = 1f / 20f;

        public VisionRaycastJob(BotManagerComponent botcontroller) : base(botcontroller)
        {
            botcontroller.StartCoroutine(CheckVisionLoop());
        }

        private IEnumerator CheckVisionLoop()
        {
            WaitForSeconds wait = new(VISION_UPDATE_INTERVAL);
            yield return wait;
            while (true)
            {
                if (BotController == null)
                {
                    yield return wait;
                    continue;
                }

                HashSet<BotComponent> bots = BotController?.BotSpawnController?.SAINBots;
                if (bots == null || bots.Count == 0)
                {
                    yield return wait;
                    continue;
                }

                if (BotController.BotGame?.Status == EFT.GameStatus.Stopping)
                {
                    yield return wait;
                    continue;
                }

                FindEnemies(bots, _enemies);
                int enemyCount = _enemies.Count;
                if (enemyCount == 0)
                {
                    yield return wait;
                    continue;
                }

                int partCount = _enemies[0].Vision.EnemyParts.PartsArray.Length;
                int totalRaycasts = enemyCount * partCount * RAYCAST_CHECKS;

                _hits = new NativeArray<RaycastHit>(totalRaycasts, Allocator.TempJob);
                _commands = new NativeArray<RaycastCommand>(totalRaycasts, Allocator.TempJob);

                CreateCommands(_commands, enemyCount, partCount);
                _handle = RaycastCommand.ScheduleBatch(_commands, _hits, 32);

                yield return null;

                var handle = _handle;
                if (!handle.IsCompleted) handle.Complete();
                _handle = handle;

                AnalyzeHits(_hits, enemyCount, partCount);
                _commands.Dispose();
                _hits.Dispose();
                
                // Updates vanilla eft vision code, it'll run another raycast if this one succeeds which isn't ideal, but id rather not rewrite all their code.
                foreach (var bot in BotController.BotSpawnController.BotGroup1) bot?.Vision.BotLook.UpdateLook(Time.time - _timelastLook);
                yield return null;
                foreach (var bot in BotController.BotSpawnController.BotGroup2) bot?.Vision.BotLook.UpdateLook(Time.time - _timelastLook);
                _timelastLook = Time.time;
                yield return wait;
            }
        }

        private float _timelastLook = 0;

        public void Dispose()
        {
            if (!_handle.IsCompleted) _handle.Complete();
            if (_commands.IsCreated) _commands.Dispose();
            if (_hits.IsCreated) _hits.Dispose();
        }

        private NativeArray<RaycastHit> _hits;
        private NativeArray<RaycastCommand> _commands;
        private JobHandle _handle;

        private void CreateCommands(NativeArray<RaycastCommand> raycastCommands, int enemyCount, int partCount)
        {
            _colliderTypes.Clear();
            _castPoints.Clear();

            int commands = 0;
            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = _enemies[i];
                var transform = enemy.Bot.Transform;
                Vector3 eyePosition = transform.EyePosition;
                Vector3 weaponFirePort = transform.WeaponData.FirePort;
                var parts = enemy.Vision.EnemyParts.PartsArray;
                // var partDistances = enemy.EnemyPlayerData.DistanceData.BodyPartDistances;

                for (int j = 0; j < partCount; j++)
                {
                    var part = parts[j];

                    SAINBodyPartRaycast raycastData = part.GetRaycast();
                    Vector3 castPoint = raycastData.CastPoint;

                    _colliderTypes.Add(raycastData.ColliderType);
                    _castPoints.Add(castPoint);

                    Vector3 weaponDir = castPoint - weaponFirePort; // we should normalize this, however, setting the magnitude to 1f allows us to skip that
                    Vector3 eyeDir = castPoint - eyePosition;

                    raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, new QueryParameters {
                        layerMask = _LOSMask
                    }, 1f);
                    commands++;

                    raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, new QueryParameters {
                        layerMask = _VisionMask
                    }, 1f);
                    commands++;

                    raycastCommands[commands] = new RaycastCommand(weaponFirePort, weaponDir, new QueryParameters {
                        layerMask = _ShootMask
                    }, 1f);
                    commands++;
                }
            }
        }

        private void AnalyzeHits(NativeArray<RaycastHit> raycastHits, int enemyCount, int partCount)
        {
            float time = Time.time;
            int hits = 0;
            int colliderTypeCount = 0;

            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = _enemies[i];
                var parts = enemy.Vision.EnemyParts.PartsArray;
                enemy.Bot.Vision.TimeLastCheckedLOS = time;

                for (int j = 0; j < partCount; j++)
                {
                    var part = parts[j];
                    EBodyPartColliderType colliderType = _colliderTypes[colliderTypeCount];
                    Vector3 castPoint = _castPoints[colliderTypeCount];
                    colliderTypeCount++;

                    part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.LineofSight, time);
                    hits++;
                    part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.Vision, time);
                    hits++;
                    part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.Shoot, time);
                    hits++;
                }
            }
            if (DebugSettings.Instance.Gizmos.DrawLineOfSightGizmos)
            {
                hits = 0;

                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = _enemies[i];
                    var parts = enemy.Vision.EnemyParts.PartsArray;

                    for (int j = 0; j < partCount; j++)
                    {
                        var part = parts[j];
                        EBodyPartColliderType colliderType = _colliderTypes[colliderTypeCount];
                        Vector3 castPoint = _castPoints[colliderTypeCount];
                        colliderTypeCount++;

                        if (raycastHits[hits].collider == null)
                        {
                            DebugGizmos.DrawLine(_commands[hits].from, _commands[hits].from + _commands[hits].direction, Color.red, 0.025f, 0.02f);
                        }
                        hits++;
                        //if (raycastHits[hits].collider == null)
                        //{
                        //}
                        hits++;
                        //if (raycastHits[hits].collider == null)
                        //{
                        //}
                        hits++;
                    }
                }
            }
        }

        private const int RAYCAST_CHECKS = 3;
        private readonly LayerMask _LOSMask = LayerMaskClass.HighPolyWithTerrainMask;
        private readonly LayerMask _VisionMask = LayerMaskClass.AI;
        private readonly LayerMask _ShootMask = LayerMaskClass.HighPolyWithTerrainMask;

        private readonly List<EBodyPartColliderType> _colliderTypes = [];
        private readonly List<Vector3> _castPoints = [];

        private static void FindEnemies(HashSet<BotComponent> bots, List<Enemy> enemies)
        {
            enemies.Clear();
            foreach (BotComponent bot in bots)
                if (bot != null && bot.BotActive)
                    foreach (Enemy enemy in bot.EnemyController.EnemiesArray)
                    {
                        if (enemy == null || enemy.Player == null || !enemy.Player.HealthController.IsAlive) continue;
                        //if (enemy.RealDistance > EnemyVisionClass.AIVisionRangeLimit(enemy)) continue;
                        enemies.Add(enemy);
                    }
        }

        private readonly List<Enemy> _enemies = [];
    }
}