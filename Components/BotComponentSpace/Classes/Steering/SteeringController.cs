using System.Collections;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SteeringController : BotBase, IBotClass
    {
        public Vector3 LookDirection => Bot.Transform.LookDirection;
        public Vector3 TargetSteerDirection { get; private set; }

        private Coroutine _controller;

        public SteeringController(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle += onBotActive;
            onBotActive(true);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle -= onBotActive;
        }

        private void onBotActive(bool value)
        {
            switch (value)
            {
                case true:
                    if (_controller == null)
                    {
                        _controller = Bot.StartCoroutine(controlSteeringLoop());
                    }
                    break;

                case false:
                    if (_controller != null)
                    {
                        Bot.StopCoroutine(_controller);
                        _controller = null;
                    }
                    break;
            }
        }

        private IEnumerator controlSteeringLoop()
        {
            while (true)
            {
                yield return null;
            }
        }
    }
}