using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Run
{
    internal class GetUnstuckAction : CombatAction
    {
        public GetUnstuckAction(BotOwner bot) : base(bot, nameof(GetUnstuckAction))
        {
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            Bot.Mover.SetTargetPose(1f);
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Steering.LookToMovingDirection();

            Vector3? unstuckDestination = null;
            var coverPoints = Bot.Cover.CoverPoints;
            if (coverPoints.Count > 0)
            {
                for (int i = 0; i < coverPoints.Count; i++)
                {
                    var cover = coverPoints[i];
                    NavMeshPath path = new();
                    if (NavMesh.CalculatePath(cover.Position, Bot.Position, -1, path))
                    {
                        unstuckDestination = new Vector3?(path.corners[path.corners.Length - 1]);
                        break;
                    }
                }
            }

            if (unstuckDestination != null)
            {
                BotOwner.Mover.GoToByWay([Bot.Position, unstuckDestination.Value], -1f);
            }
            this.EndProfilingSample();
        }

        public override void Start()
        {
            Bot.Mover.StopMove();
        }

        public override void Stop()
        {
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
        }
    }
}