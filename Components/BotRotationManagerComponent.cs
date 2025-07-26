using Comfort.Common;
using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using SAIN.Types.TurnSmoothing;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.Components.RotationController
{
    public class BotRotationManagerComponent : MonoBehaviour
    {
        public static BotRotationManagerComponent Create(GameObject gameObject, BotSpawner botSpawner, PlayerSpawnTracker playerSpawner)
        {
            return gameObject.AddComponent<BotRotationManagerComponent>();
        }

        protected void FixedUpdate()
        {
            HashSet<PlayerComponent> allPlayers = GameWorldComponent.Instance.PlayerTracker.AlivePlayerArray;
            float deltaTime = Time.fixedDeltaTime;
            bool randomSwayEnabled = GlobalSettingsClass.Instance.Steering.RANDOMSWAY_TOGGLE;
            foreach (PlayerComponent player in allPlayers)
            {
                BotOwner botOwner = player.BotOwner;
                if (botOwner!= null)
                {
                    player.CharacterController.TickBotSteering(deltaTime, botOwner, player.BotComponent, randomSwayEnabled);
                }
            }
        }
    }
}