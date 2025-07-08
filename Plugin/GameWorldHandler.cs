using EFT;
using SAIN.Components;
using UnityEngine;

namespace SAIN
{
    public class GameWorldHandler
    {
        public static void Create(GameWorld gameWorld)
        {
            if (SAINGameWorld != null)
            {
                Logger.LogWarning($"Old SAIN Gameworld is not null! Destroying...");
                SAINGameWorld.DestroyComponent();
                GameObject.Destroy(SAINGameWorld);
            }
            SAINGameWorld = gameWorld.gameObject.AddComponent<GameWorldComponent>();
            BotManagerComponent botController = gameWorld.gameObject.AddComponent<BotManagerComponent>();
            SAINGameWorld.Init(gameWorld, botController);
        }

        public static GameWorldComponent SAINGameWorld { get; private set; }
    }
}
