using EFT;
using SAIN.BotController.Classes;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class SAINSquadClass : BotBase, IBotClass
    {
        public SAINSquadClass(BotComponent bot) : base(bot)
        {
            getSquad();
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void RemoveFromSquad()
        {
            SquadInfo = null;
            getSquad();
        }

        private void getSquad()
        {
            SquadInfo = SAINBotController.Instance.BotSquads.GetSquad(Bot.Person.AIInfo.BotOwner);
        }

        public Squad SquadInfo { get; private set; }

        public float DistanceToSquadLeader { get; private set; }

        public readonly List<BotComponent> VisibleMembers = new();

        private float _updateMemberTime = 0f;

        public bool IAmLeader => SquadInfo.LeaderId == Bot.ProfileId;

        public BotComponent LeaderComponent => SquadInfo?.LeaderComponent;

        public bool BotInGroup => BotOwner.BotsGroup.MembersCount > 1 || HumanFriendClose;

        public Dictionary<string, BotComponent> Members => SquadInfo?.Members;

        public bool HumanFriendClose
        {
            get
            {
                if (_nextCheckhumantime < Time.time)
                {
                    _nextCheckhumantime = Time.time + 3f;
                    _humanFriendclose = humanFriendClose(50f * 50f);
                }
                return _humanFriendclose;
            }
        }

        private bool _humanFriendclose;

        private float _nextCheckhumantime;

        private bool humanFriendClose(float distToCheck)
        {
            foreach (var playerComponent in GameWorldComponent.Instance.PlayerTracker.AlivePlayers.Values)
            {
                if (playerComponent != null &&
                    !playerComponent.IsAI &&
                    Bot?.EnemyController?.IsPlayerAnEnemy(playerComponent.ProfileId) == false &&
                    (playerComponent.Position - Bot.Position).sqrMagnitude < distToCheck)
                {
                    return true;
                }
            }
            return false;
        }

        public void Update()
        {
            if (BotOwner.BotsGroup.MembersCount > 1 &&
                SquadInfo != null &&
                _updateMemberTime < Time.time)
            {
                _updateMemberTime = Time.time + 0.5f;

                checkVisibleMembers();

                if (!IAmLeader && LeaderComponent != null)
                {
                    DistanceToSquadLeader = (Bot.Position - LeaderComponent.Position).magnitude;
                }
            }
        }

        public void Dispose()
        {
        }

        private void checkVisibleMembers()
        {
            VisibleMembers.Clear();
            Vector3 eyePos = Bot.Transform.EyePosition;
            foreach (var member in Members.Values)
            {
                if (member != null &&
                    member.ProfileId != Bot.ProfileId)
                {
                    Vector3 direction = member.Transform.BodyPosition - eyePos;
                    float magnitude = direction.magnitude;
                    if (magnitude > 100)
                    {
                        continue;
                    }
                    if (!Physics.Raycast(eyePos, direction, magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        VisibleMembers.Add(member);
                    }
                }
            }
        }
    }
}