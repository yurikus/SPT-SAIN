using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class LightDetectionClass : PlayerComponentBase
    {
        public List<FlashLightPoint> LightPoints { get; } = new List<FlashLightPoint>();

        public LightDetectionClass(PlayerComponent component) : base(component)
        {
        }

        public void CreateDetectionPoints(bool visibleLight, bool onlyLaser)
        {
            if (PlayerComponent.IsAI)
            {
                return;
            }

            Vector3 lightDirection = getLightDirection(onlyLaser);
            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;
            float detectionDistance = 100f;
            Vector3 firePort = Transform.WeaponFirePort;

            // Our flashlight did not hit an object, return
            if (!Physics.Raycast(firePort, lightDirection, out RaycastHit hit, detectionDistance, mask))
            {
                return;
            }

            // our flashlight hit an object,
            // create a light point which is slightly away from the object,
            // so it can be clearly detected by a raycast from a bot
            Vector3 point = hit.point + (hit.normal * 0.1f);
            LightPoints.Add(new FlashLightPoint(point));

            // Debug is off, return
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.Flashlight.DebugFlash)
            {
                return;
            }

            if (visibleLight)
            {
                DebugGizmos.Sphere(point, 0.1f, Color.red, true, 0.25f);
                DebugGizmos.Line(point, firePort, Color.red, 0.015f, true, 0.25f);
                return;
            }

            DebugGizmos.Sphere(point, 0.1f, Color.blue, true, 0.25f);
            DebugGizmos.Line(point, firePort, Color.blue, 0.05f, true, 0.25f);
        }

        private Vector3 getLightDirection(bool onlyLaser)
        {
            if (onlyLaser)
            {
                return Transform.WeaponPointDirection;
            }
            if (_nextUpdatebeamtime < Time.time)
            {
                _nextUpdatebeamtime = Time.time + 0.5f;
                createFlashlightBeam(_lightBeamDirections);
            }
            return _lightBeamDirections.GetRandomItem();
        }

        private void createFlashlightBeam(List<Vector3> beamDirections)
        {
            // Define the cone angle (in degrees)
            float coneAngle = 10f;

            beamDirections.Clear();
            Vector3 weaponPointDir = Transform.WeaponPointDirection;
            for (int i = 0; i < 10; i++)
            {
                // Generate random angles within the cone range for yaw and pitch
                float angle = coneAngle * 0.5f;
                float x = Random.Range(-angle, angle);
                float y = Random.Range(-angle, angle);
                float z = Random.Range(-angle, angle);

                // AddColor a Quaternion rotation based on the random yaw and pitch angles
                Quaternion randomRotation = Quaternion.Euler(x, y, z);

                // Rotate the player's look direction by the Quaternion rotation
                Vector3 randomBeamDirection = randomRotation * weaponPointDir;

                beamDirections.Add(randomBeamDirection);
            }
        }

        public void DetectAndInvestigateFlashlight()
        {
            if (!PlayerComponent.IsSAINBot)
            {
                return;
            }
            if (_searchTime > Time.time)
            {
                return;
            }

            if (PlayerComponent.BotComponent?.BotActive != true)
            {
                return;
            }

            var enemies = PlayerComponent.BotComponent.EnemyController.Enemies.Values;
            if (enemies == null)
            {
                return;
            }

            BotOwner bot = PlayerComponent.BotOwner;
            if (bot == null)
            {
                return;
            }

            bool usingNVGs = bot.NightVision?.UsingNow == true;
            Vector3 botPos = bot.LookSensor._headPoint;

            foreach (var enemy in enemies)
            {
                checkEnemyLight(enemy, botPos, usingNVGs);
            }
        }

        private void checkEnemyLight(Enemy enemy, Vector3 botPos, bool usingNVGs)
        {
            // something is wrong with this enemy, or the enemy is another bot
            if (!validateEnemyIsHuman(enemy))
            {
                return;
            }

            // we checked this enemies flashlight recently, continue to next enemy
            if (enemy.NextCheckFlashLightTime > Time.time)
            {
                return;
            }
            enemy.NextCheckFlashLightTime = Time.time + 0.2f;

            var flashLight = enemy.EnemyPlayerComponent.Flashlight;
            if (!checkIsBeamVisible(flashLight, usingNVGs))
            {
                return;
            }

            // Light point is out of range, dont raycast to check vision
            FlashLightPoint lightPoint = flashLight.LightDetection.LightPoints.PickRandom();
            if (!isLightInRange(botPos, lightPoint.Point))
            {
                return;
            }

            // is the point within a bot's field of view?
            if (!PlayerComponent.BotOwner.LookSensor.IsPointInVisibleSector(lightPoint.Point))
            {
                return;
            }

            // raycast to check if the point is visible
            if (!raycastToLightPoint(lightPoint.Point, botPos))
            {
                if (SAINPlugin.DebugMode)
                {
                    DebugGizmos.Line(lightPoint.Point, botPos, Color.white, 0.05f, true, 0.25f);
                    DebugGizmos.Line(lightPoint.Point, enemy.EnemyPosition + Vector3.up, Color.white, 0.05f, true, 0.25f);
                }
                return;
            }

            if (SAINPlugin.DebugMode)
            {
                DebugGizmos.Line(lightPoint.Point, botPos, Color.red, 0.1f, true, 3f);
                DebugGizmos.Line(lightPoint.Point, enemy.EnemyPosition + Vector3.up, Color.red, 0.1f, true, 3f);
            }

            // all checks are passed, estimate the enemy position and try to investigate
            Vector3 estimatedPosition = estimatePosition(enemy.EnemyPosition, lightPoint.Point, botPos, 20f);
            tryToInvestigate(estimatedPosition);
            _searchTime = Time.time + 1f;
        }

        private bool validateEnemyIsHuman(Enemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }
            if (enemy.IsAI)
            {
                return false;
            }
            if (!enemy.CheckValid())
            {
                return false;
            }
            if (enemy.EnemyPlayerComponent == null)
            {
                return false;
            }
            return true;
        }

        private bool checkIsBeamVisible(FlashLightClass flashLight, bool usingNVGs)
        {
            // If this isn't visible light, and the bot doesn't have night vision, ignore it
            if (!flashLight.WhiteLight &&
                !flashLight.Laser &&
                !usingNVGs)
            {
                return false;
            }
            if (flashLight.LightDetection.LightPoints.Count <= 0)
            {
                return false;
            }
            return true;
        }

        private bool isLightInRange(Vector3 botPos, Vector3 lightPos)
        {
            return (botPos - lightPos).sqrMagnitude < _maxLightRange;
        }

        private const float _maxLightRange = 100f * 100f;

        private bool raycastToLightPoint(Vector3 lightPointPos, Vector3 botPos)
        {
            Vector3 direction = (lightPointPos - botPos);
            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMaskAI;
            return !Physics.Raycast(botPos, direction, direction.magnitude, mask);
        }

        private void tryToInvestigate(Vector3 estimatedPosition)
        {
            var botComponent = PlayerComponent.BotComponent;
            if (botComponent != null)
            {
                botComponent.Squad.SquadInfo.AddPointToSearch
                    (estimatedPosition,
                    25f,
                    botComponent,
                    AISoundType.step,
                    Singleton<GameWorld>.Instance.MainPlayer,
                    SAIN.BotController.Classes.Squad.ESearchPointType.Flashlight);
            }
            else
            {
                PlayerComponent.BotOwner?.BotsGroup.AddPointToSearch(estimatedPosition, 20f, PlayerComponent.BotOwner, true, false);
            }
        }

        private Vector3 estimatePosition(Vector3 playerPos, Vector3 flashPos, Vector3 botPos, float dispersion)
        {
            Vector3 estimatedPosition = playerPos;
            // Vector3 estimatedPosition = Vector3.Lerp(playerPos, flashPos, Random.Range(0.0f, 0.25f));

            float distance = (playerPos - botPos).magnitude;

            float maxDispersion = Mathf.Clamp(distance, 0f, 50f);

            float positionDispersion = maxDispersion / dispersion;

            float x = EFTMath.Random(-positionDispersion, positionDispersion);
            float z = EFTMath.Random(-positionDispersion, positionDispersion);

            return new Vector3(estimatedPosition.x + x, estimatedPosition.y, estimatedPosition.z + z);
        }

        private float _searchTime;
        private float _nextUpdatebeamtime;
        private readonly List<Vector3> _lightBeamDirections = new();
    }
}