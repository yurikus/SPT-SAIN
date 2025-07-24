using Comfort.Common;
using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using SAIN.Types.TurnSmoothing;
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

        protected void Update()
        {
            HashSet<PlayerComponent> allPlayers = GameWorldComponent.Instance.PlayerTracker.AlivePlayerArray;
            foreach (PlayerComponent player in allPlayers)
            {
                if (player.BotOwner != null)
                {
                    player.CharacterController.TickBotSteering(Time.deltaTime, player.BotOwner, player.BotComponent);
                }
            }
        }
    }
}