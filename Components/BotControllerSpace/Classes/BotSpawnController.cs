using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.SAINComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.Components.BotController
{
    public class BotSpawnController : SAINControllerBase
    {
        public event Action<BotComponent> OnBotAdded;
        public event Action<BotComponent> OnBotRemoved;

        public BotSpawnController(SAINBotController botController) : base(botController)
        {
            Instance = this;
        }

        public static BotSpawnController Instance;

        public BotDictionary BotDictionary = new();

        public static readonly List<WildSpawnType> StrictExclusionList = new()
        {
            WildSpawnType.bossZryachiy,
            WildSpawnType.followerZryachiy,
            WildSpawnType.peacefullZryachiyEvent,
            WildSpawnType.ravangeZryachiyEvent,
            WildSpawnType.shooterBTR,
            WildSpawnType.marksman,
            WildSpawnType.infectedAssault,
            WildSpawnType.infectedCivil,
            WildSpawnType.infectedLaborant,
            WildSpawnType.infectedPmc,
            WildSpawnType.infectedTagilla
        };

        public void Update()
        {
            if (Subscribed &&
                GameEnding)
            {
                UnSubscribe();
            }
        }

        public bool GameEnding
        {
            get
            {
                var status = GameStatus;
                return status == GameStatus.Stopping || status == GameStatus.Stopped || status == GameStatus.SoftStopping;
            }
        }

        private GameStatus GameStatus
        {
            get
            {
                var botGame = BotController?.BotGame;
                if (botGame != null)
                {
                    return botGame.Status;
                }
                return GameStatus.Starting;
            }
        }

        public void AddBot(BotOwner botOwner)
        {
            //Logger.LogDebug($"Checking {botOwner.name} for adding sain");
            BotController.StartCoroutine(addBot(botOwner));
        }

        private IEnumerator addBot(BotOwner botOwner)
        {
            PlayerComponent playerComponent = null;
            BotComponent botComponent = null;
            try
            {
                //Logger.LogDebug($"Checking {botOwner.name}...");
                playerComponent = getPlayerComp(botOwner);
                checkExisting(botOwner);

                //Logger.LogDebug($"Checking if {botOwner.name} excluded...");
                if (SAINPlugin.IsBotExluded(botOwner))
                {
                    //Logger.LogDebug($"{botOwner.name} is excluded");
                    botOwner.gameObject.AddComponent<SAINNoBushESP>().Init(botOwner);
                    yield break;
                }

                //Logger.LogDebug($"Adding SAIN to {botOwner.name}...");
                botComponent = botOwner.gameObject.AddComponent<BotComponent>();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                yield break;
            }

            if (botComponent == null)
            {
                Logger.LogError($"Bot Component Null!");
                yield break;
            }
            if (playerComponent == null)
            {
                botComponent.Dispose();
                Logger.LogError($"Player Component Null!");
                yield break;
            }

            if (botComponent.InitializeBot(playerComponent.Person))
            {
                BotDictionary.Add(botOwner.name, botComponent);
                playerComponent.InitBotComponent(botComponent);
                botOwner.LeaveData.OnLeave += removeBot;
                playerComponent.Person.ActivationClass.OnPersonDeadOrDespawned += removePerson;
                OnBotAdded?.Invoke(botComponent);
            }
            else
            {
                Logger.LogDebug($"Failed to Init Bot [{botOwner.name}]");
                botComponent.Dispose();
            }

            yield break;
        }

        public void Subscribe(BotSpawner botSpawner)
        {
            if (!Subscribed)
            {
                botSpawner.OnBotRemoved += removeBot;
                Subscribed = true;
            }
        }

        public void UnSubscribe()
        {
            if (Subscribed &&
                BotController?.BotSpawner != null)
            {
                BotController.BotSpawner.OnBotRemoved -= removeBot;
                Subscribed = false;
            }
        }

        private bool Subscribed = false;

        public BotComponent GetSAIN(BotOwner botOwner, StringBuilder debugString)
        {
            return GetSAIN(botOwner?.name);
        }

        public BotComponent GetSAIN(string botName)
        {
            if (!botName.IsNullOrEmpty() &&
                BotDictionary.TryGetValue(botName, out BotComponent component))
            {
                return component;
            }
            return null;
        }

        private PlayerComponent getPlayerComp(BotOwner botOwner)
        {
            PlayerComponent playerComponent = botOwner.gameObject.GetComponent<PlayerComponent>();
            playerComponent.InitBotOwner(botOwner);
            return playerComponent;
        }

        private void removePerson(PersonClass person)
        {
            person.ActivationClass.OnPersonDeadOrDespawned -= removePerson;
            removeBot(person.AIInfo.BotOwner);
        }

        private void checkExisting(BotOwner botOwner)
        {
            string name = botOwner.name;
            if (BotDictionary.ContainsKey(name))
            {
                Logger.LogDebug($"{name} was already present in Bot Dictionary. Removing...");
                BotDictionary.Remove(name);
            }

            GameObject gameObject = botOwner.gameObject;
            // If somehow this bot already has components attached, destroy it.
            if (gameObject.TryGetComponent(out BotComponent botComponent))
            {
                Logger.LogDebug($"{name} already had a BotComponent attached. Destroying...");
                botComponent.Dispose();
            }
            if (gameObject.TryGetComponent(out SAINNoBushESP noBushComponent))
            {
                Logger.LogDebug($"{name} already had No Bush ESP attached. Destroying...");
                GameObject.Destroy(noBushComponent);
            }
        }

        private void initBotComp(BotOwner botOwner, PlayerComponent playerComponent)
        {
            BotComponent botComponent = botOwner.gameObject.AddComponent<BotComponent>();
            if (botComponent.Init(playerComponent.Person))
            {
                BotDictionary.Add(botOwner.name, botComponent);
                playerComponent.InitBotComponent(botComponent);
                botOwner.LeaveData.OnLeave += removeBot;
                playerComponent.Person.ActivationClass.OnPersonDeadOrDespawned += removePerson;
            }
            else
            {
                botComponent?.Dispose();
            }
        }

        public void removeBot(BotOwner botOwner)
        {
            try
            {
                if (botOwner != null)
                {
                    if (BotDictionary.TryGetValue(botOwner.name, out BotComponent botComponent))
                    {
                        OnBotRemoved?.Invoke(botComponent);
                        botComponent.Dispose();
                    }
                    BotDictionary.Remove(botOwner.name);
                    if (botOwner.TryGetComponent(out BotComponent component))
                    {
                        OnBotRemoved?.Invoke(botComponent);
                        component.Dispose();
                    }
                    if (botOwner.TryGetComponent(out SAINNoBushESP noBush))
                    {
                        UnityEngine.Object.Destroy(noBush);
                    }
                }
                else
                {
                    Logger.LogError("Bot is null, cannot dispose!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Component Error: {ex}");
            }
        }
    }
}