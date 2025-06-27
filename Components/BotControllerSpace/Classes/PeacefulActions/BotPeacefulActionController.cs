using SAIN.Models.Enums;
using System.Text;
using UnityEngine;

namespace SAIN.Components.BotController.PeacefulActions
{
    public class BotPeacefulActionController : SAINControllerBase, IBotControllerClass
    {
        public PeacefulBotFinder PeacefulBotFinder { get; }
        public PeacefulActionSet Actions { get; } = new PeacefulActionSet();

        public BotPeacefulActionController(SAINBotController controller) : base(controller)
        {
            //PeacefulBotFinder = new PeacefulBotFinder(controller);
        }

        public void Init()
        {
            //PeacefulBotFinder.Init();
            //initActions();
        }

        private void initActions()
        {
            Actions.Add(EPeacefulAction.Gathering, new BotGatheringController(BotController, EPeacefulAction.Gathering));
            Actions.Add(EPeacefulAction.Conversation, new BotConversationController(BotController, EPeacefulAction.Conversation));
        }

        public void Update()
        {
            //PeacefulBotFinder.Update();
            //Actions.CheckExecute(PeacefulBotFinder.ZoneDatas);
            //logDatas();
        }

        public void Dispose()
        {
            //PeacefulBotFinder.Dispose();
        }

        private void logDatas()
        {
            if (_nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 10;
                StringBuilder stringBuilder = new();
                foreach (var datas in PeacefulBotFinder.ZoneDatas)
                {
                    stringBuilder.AppendLine($"{datas.Key} : [{datas.Value.AllContainedBots.Count.ToString()}] : [{datas.Value.AllPeacefulBots.Count.ToString()}]");
                }
                Logger.LogDebug(stringBuilder.ToString());
            }
        }

        private float _nextLogTime;
    }
}