using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
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

            foreach (PlayerComponent playerComp in allPlayers)
            {
                BotOwner botOwner = playerComp.BotOwner;
                if (botOwner != null)
                {
                    var controller = playerComp.CharacterController;
                    controller.UpdateTurnSettings(deltaTime, botOwner, playerComp.BotComponent, randomSwayEnabled);
                    controller.UpdateBotTurnData(deltaTime);
                    controller.RotatePlayer(playerComp);
                }
            }
        }
    }
}