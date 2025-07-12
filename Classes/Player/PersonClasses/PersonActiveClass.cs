using EFT;
using System;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonActiveClass
    {
        public event Action<bool> OnGameObjectActiveChanged;

        public event Action<bool> OnPlayerActiveChanged;

        public bool PlayerActive { get; private set; }
        public bool GameObjectActive { get; private set; }
        public bool IsAlive { get; private set; }

        public void CheckActive(PlayerComponent playerComponent)
        {
            if (IsAlive) IsAlive = CheckAlive(playerComponent);

            bool wasGameObjectActive = GameObjectActive;
            GameObjectActive = IsAlive && CheckGameObjectActive(playerComponent);
            if (wasGameObjectActive != GameObjectActive)
            {
                OnGameObjectActiveChanged?.Invoke(GameObjectActive);
                //Logger.LogDebug($"GameObject {_person.Nickname} Active [{GameObjectActive}]");
            }

            bool wasActive = PlayerActive;
            PlayerActive = IsAlive && GameObjectActive && CheckPlayerExists(playerComponent?.Player);
            if (wasActive != PlayerActive)
            {
                OnPlayerActiveChanged?.Invoke(PlayerActive);
                //Logger.LogDebug($"Player {_person.Nickname} Active [{PlayerActive}]");
            }
        }

        public void Disable()
        {
            bool wasGameObjectActive = GameObjectActive;
            GameObjectActive = false;
            if (wasGameObjectActive != GameObjectActive)
            {
                OnGameObjectActiveChanged?.Invoke(GameObjectActive);
            }

            bool wasActive = PlayerActive;
            PlayerActive = false;
            if (wasActive != PlayerActive)
            {
                OnPlayerActiveChanged?.Invoke(PlayerActive);
            }
        }

        public PersonActiveClass(PlayerComponent playerComponent)
        {
            IsAlive = true;
        }

        private static bool CheckAlive(PlayerComponent playerComponent)
        {
            IPlayer iPlayer = playerComponent.Player;
            if (iPlayer == null)
            {
                return false;
            }
            if (iPlayer.HealthController?.IsAlive == false)
            {
                return false;
            }

            if (iPlayer.IsAI)
            {
                BotOwner botOwner = iPlayer.AIData?.BotOwner;
                if (botOwner == null ||
                    botOwner.gameObject == null ||
                    botOwner.Transform?.Original == null)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckGameObjectActive(PlayerComponent playerComponent)
        {
            GameObject gameObject = playerComponent?.gameObject;
            if (gameObject == null || !gameObject.activeSelf || !gameObject.activeInHierarchy)
            {
                return false;
            }
            return true;
        }

        private static bool CheckPlayerExists(Player player)
        {
            return player != null && player.gameObject != null && player.Transform?.Original != null;
        }
    }
}