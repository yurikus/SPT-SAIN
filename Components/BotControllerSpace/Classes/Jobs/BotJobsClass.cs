using SAIN.Components.BotControllerSpace.Classes.Raycasts;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.SAINComponent;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class BotJobsClass(SAINBotController botController) : SAINControllerBase(botController)
    {
        public PlayerDistancesJob PlayerDistancesJob { get; } = new PlayerDistancesJob(botController);
        public VisionRaycastJob VisionJob { get; } = new VisionRaycastJob(botController);
        public EnemyPlaceRaycastJob EnemyPlaceJob { get; } = new EnemyPlaceRaycastJob(botController);

        public void Update()
        {
        }

        public void Dispose()
        {
            VisionJob.Dispose();
            EnemyPlaceJob.Dispose();
            PlayerDistancesJob.Dispose();
        }

        public void UpdateVisionForBots(HashSet<BotComponent> Bots)
        {
            //_localBotList.Sort((x, y) => x.LastCheckVisibleTime.CompareTo(y.LastCheckVisibleTime));

            int count = 0;
            foreach (BotComponent bot in Bots)
            {
                if (bot == null) continue;

                float frequency = bot.BotActive ? 0.05f : 0.25f;
                if (bot.LastCheckVisibleTime + frequency > Time.time)
                    continue;

                bot.LastCheckVisibleTime = Time.time;
                int numUpdated = bot.Vision.BotLook.UpdateLook();
                if (numUpdated > 0)
                {
                    count++;
                    if (count >= maxBotsPerFrame)
                    {
                        //break;
                    }
                }
            }
            //_localBotList.Clear();
        }

        private static int maxBotsPerFrame = 5;
        private readonly HashSet<BotComponent> _localBotList = [];

        static BotJobsClass()
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings(SAINPresetClass.Instance);
        }

        private static void updateSettings(SAINPresetClass preset)
        {
            maxBotsPerFrame = Mathf.RoundToInt(preset.GlobalSettings.General.Performance.MaxBotsToCheckVisionPerFrame);
        }
    }
}