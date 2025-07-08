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
        public readonly HashSet<PlayerComponent> AlivePlayerArray = [];

        public event Action<PlayerComponent> OnPlayerAdded;

        public event Action<string, PlayerComponent> OnPlayerRemoved;

        public readonly PlayerDictionary AlivePlayersDictionary = [];

        public readonly List<IPlayer> DeadPlayers = [];

        public PlayerComponent GetPlayerComponent(string profileId) => AlivePlayersDictionary.GetPlayerComponent(profileId);
        public PlayerComponent GetPlayerComponent(IPlayer Player) => AlivePlayersDictionary.GetPlayerComponent(Player);

        public PlayerComponent FindClosestHumanPlayer(out float closestPlayerSqrMag, Vector3 targetPosition, out Player player)
        {
            PlayerComponent closestPlayer = null;
            closestPlayerSqrMag = float.MaxValue;
            player = null;

            foreach (var component in AlivePlayersDictionary.Values)
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
            AddPlayer(player);
            if (AlivePlayersDictionary.TryGetValue(player.ProfileId, out var component))
            {
                Logger.LogDebug($"Successfully created new Player Component for [{player.Profile?.Nickname} : {player.ProfileId}]");
                return component;
            }
            return null;
        }

        private void AddPlayer(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null IPlayer.");
                return;
            }

            string profileId = iPlayer.ProfileId;
            Player player = iPlayer as Player;
            if (player == null)
            {
                Logger.LogError($"Could not add PlayerComponent for Null Player. IPlayer: {iPlayer.Profile?.Nickname} : {profileId}");
                return;
            }
            if (player.gameObject == null)
            {
                Logger.LogError($"Player Has null gameobject? IPlayer: {iPlayer.Profile?.Nickname} : {profileId}");
                return;
            }

            if (AlivePlayersDictionary.TryRemove(profileId, out bool compDestroyed))
            {
                string playerInfo = $"{player.name} : {player.Profile?.Nickname} : {profileId}";
                Logger.LogWarning($"PlayerComponent already exists for Player: {playerInfo}");
                if (compDestroyed)
                {
                    Logger.LogWarning($"Destroyed old Component for: {playerInfo}");
                }
            }

            PlayerComponent component = player.gameObject.AddComponent<PlayerComponent>();
            if (component?.Init(iPlayer) == true)
            {
                component.Person.ActivationClass.OnPersonDeadOrDespawned += removePerson;
                AlivePlayersDictionary.Add(profileId, component);
                AlivePlayerArray.Add(component);
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
            AlivePlayerArray.Remove(person.PlayerComponent);
            person.ActivationClass.OnPersonDeadOrDespawned -= removePerson;
            AlivePlayersDictionary.TryRemove(person.ProfileId, out _);

            if (!person.ActivationClass.IsAlive &&
                person.Player != null)
            {
                //SAINGameWorld.StartCoroutine(addDeadPlayer(person.Player));
            }
        }

        public PlayerSpawnTracker(GameWorldComponent sainGameWorld)
        {
            _sainGameWorld = sainGameWorld;
            sainGameWorld.GameWorld.OnPersonAdd += AddPlayer;
        }

        public void Dispose()
        {
            var gameWorld = _sainGameWorld?.GameWorld;
            if (gameWorld != null)
            {
                gameWorld.OnPersonAdd -= AddPlayer;
            }
            foreach (var player in AlivePlayersDictionary)
            {
                player.Value?.Dispose();
            }
            AlivePlayersDictionary.Clear();
        }

        private readonly GameWorldComponent _sainGameWorld;
    }
}