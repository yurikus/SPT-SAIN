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
                SAINGameWorld.Dispose();
                GameObject.Destroy(SAINGameWorld);
            }
            SAINGameWorld = gameWorld.gameObject.AddComponent<GameWorldComponent>();
            SAINBotController botController = gameWorld.gameObject.AddComponent<SAINBotController>();
            SAINGameWorld.Init(gameWorld, botController);
        }

        public static GameWorldComponent SAINGameWorld { get; private set; }
    }
}
