using SAIN.Components;
using UnityEngine;

namespace SAIN
{
    public class GameWorldHandler
    {
        public static void Create(GameObject gameWorldObject)
        {
            if (SAINGameWorld != null)
            {
                Logger.LogWarning($"Old SAIN Gameworld is not null! Destroying...");
                SAINGameWorld.Dispose();
                GameObject.Destroy(SAINGameWorld);
            }
            SAINGameWorld = gameWorldObject.AddComponent<GameWorldComponent>();
        }

        public static GameWorldComponent SAINGameWorld { get; private set; }
        public static SAINBotController SAINBotController => SAINGameWorld?.SAINBotController;
    }
}
