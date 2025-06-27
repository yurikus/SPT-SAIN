using EFT;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerSpawnTracker
    {
        public event Action<PlayerComponent> OnPlayerAdded;

        public event Action<string, PlayerComponent> OnPlayerRemoved;

        public readonly PlayerDictionary AlivePlayers = new();

        public readonly Dictionary<string, Player> DeadPlayers = new();

        public PlayerComponent GetPlayerComponent(string profileId) => AlivePlayers.GetPlayerComponent(profileId);

        public PlayerComponent FindClosestHumanPlayer(out float closestPlayerSqrMag, Vector3 targetPosition, out Player player)
        {
            PlayerComponent closestPlayer = null;
            closestPlayerSqrMag = float.MaxValue;
            player = null;

            foreach (var component in AlivePlayers.Values)
            {
                if (component != null &&
                    component.Player != null &&
                    !component.IsAI)
                {
                    float sqrMag = (component.Position - targetPosition).sqrMagnitude;
                    if (sqrMag < closestPlayerSqrMag)
                    {
                        player = component.Player;
                        closestPlayer = component;
                        closestPlayerSqrMag = sqrMag;
                    }
                }
            }
            return closestPlayer;
        }

        public Player FindClosestHumanPlayer(out float closestPlayerSqrMag, Vector3 targetPosition)
        {
            FindClosestHumanPlayer(out closestPlayerSqrMag, targetPosition, out Player player);
            return player;
        }

        public PlayerComponent AddPlayerManual(IPlayer player)
        {
            if (player == null)
            {
                return null;
            }
            //Logger.LogDebug($"Manually trying to recreate Player Component for [{player.Profile?.Nickname} : {player.ProfileId}]");
            addPlayer(player);
            if (AlivePlayers.TryGetValue(player.ProfileId, out var component))
            {
                Logger.LogDebug($"Successfully created new Player Component for [{player.Profile?.Nickname} : {player.ProfileId}]");
                return component;
            }
            return null;
        }

        private void addPlayer(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null IPlayer.");
                return;
            }

            string profileId = iPlayer.ProfileId;
            Player player = GetPlayer(profileId);
            if (player == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null Player. IPlayer: {iPlayer.Profile?.Nickname} : {profileId}");
                return;
            }

            if (AlivePlayers.TryRemove(profileId, out bool compDestroyed))
            {
                string playerInfo = $"{player.name} : {player.Profile?.Nickname} : {profileId}";
                Logger.LogWarning($"PlayerComponent already exists for Player: {playerInfo}");
                if (compDestroyed)
                {
                    Logger.LogWarning($"Destroyed old Component for: {playerInfo}");
                }
            }

            PlayerComponent component = player.gameObject.AddComponent<PlayerComponent>();
            if (component?.Init(iPlayer, player) == true)
            {
                component.Person.ActivationClass.OnPersonDeadOrDespawned += removePerson;
                AlivePlayers.Add(profileId, component);
                OnPlayerAdded?.Invoke(component);
            }
            else
            {
                Logger.LogError($"Init PlayerComponent Failed for {player.name} : {player.ProfileId}");
                GameObject.Destroy(component);
            }
        }

        private void removePerson(PersonClass person)
        {
            OnPlayerRemoved?.Invoke(person.ProfileId, person.PlayerComponent);
            person.ActivationClass.OnPersonDeadOrDespawned -= removePerson;
            AlivePlayers.TryRemove(person.ProfileId, out _);

            if (!person.ActivationClass.IsAlive &&
                person.Player != null)
            {
                //SAINGameWorld.StartCoroutine(addDeadPlayer(person.Player));
            }
        }

        public Player GetPlayer(string profileId)
        {
            if (!profileId.IsNullOrEmpty())
            {
                return GameWorldInfo.GetAlivePlayer(profileId);
            }
            return null;
        }

        private IEnumerator addDeadPlayer(Player player)
        {
            yield return null;

            if (player != null &&
                !player.HealthController.IsAlive)
            {
                if (DeadPlayers.Count > _maxDeadTracked)
                {
                    DeadPlayers.Remove(DeadPlayers.First().Key);
                }
                DeadPlayers.Add(player.ProfileId, player);
            }
        }

        public PlayerSpawnTracker(GameWorldComponent sainGameWorld)
        {
            _sainGameWorld = sainGameWorld;
            sainGameWorld.GameWorld.OnPersonAdd += addPlayer;
        }

        public void Dispose()
        {
            var gameWorld = _sainGameWorld?.GameWorld;
            if (gameWorld != null)
            {
                gameWorld.OnPersonAdd -= addPlayer;
            }
            foreach (var player in AlivePlayers)
            {
                player.Value?.Dispose();
            }
            AlivePlayers.Clear();
        }

        private readonly GameWorldComponent _sainGameWorld;
        private const int _maxDeadTracked = 30;
    }
}