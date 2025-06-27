using SAIN.Components.BotControllerSpace.Classes.Raycasts;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.SAINComponent;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class BotJobsClass : SAINControllerBase
    {
        public BotRaycastJobs RaycastJobs { get; }

        public PlayerDistancesJob PlayerDistancesJob { get; }

        public BotJobsClass(SAINBotController botController) : base(botController)
        {
            RaycastJobs = new BotRaycastJobs(BotController);
            PlayerDistancesJob = new PlayerDistancesJob(BotController);
        }

        public void Update()
        {
            var bots = Bots;
            if (bots != null && bots.Count > 0)
            {
                RaycastJobs.Update();
                UpdateVisionForBots();
            }
        }

        public void Dispose()
        {
            RaycastJobs.Dispose();
            PlayerDistancesJob.Dispose();
        }

        private void UpdateVisionForBots()
        {
            _localBotList.Clear();
            _localBotList.AddRange(Bots.Values);
            _localBotList.Sort((x, y) => x.LastCheckVisibleTime.CompareTo(y.LastCheckVisibleTime));

            int count = 0;
            foreach (var bot in _localBotList)
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
                        break;
                    }
                }
            }
            _localBotList.Clear();
        }

        private static int maxBotsPerFrame = 5;
        private readonly List<BotComponent> _localBotList = new();

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