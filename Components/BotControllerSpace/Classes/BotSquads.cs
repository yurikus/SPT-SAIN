using EFT;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.BotController.Classes
{
    public class BotSquads : SAINControllerBase
    {
        public BotSquads(SAINBotController botController) : base(botController)
        {
        }

        public void Update()
        {
            int count = 0;
            foreach (var squad in Squads.Values)
            {
                if (squad != null)
                {
                    squad.Update();

                    if (SAINPlugin.DebugMode && DebugTimer < Time.time)
                    {
                        DebugTimer = Time.time + 60f;

                        string debugText = $"Squad [{count}]: " +
                            $"ID: [{squad.GetId()}] " +
                            $"Count: [{squad.Members.Count}] " +
                            $"Power: [{squad.SquadPowerLevel}] " +
                            $"Members:";

                        foreach (var member in squad.MemberInfos.Values)
                        {
                            debugText += $" [{member.Nickname}, {member.PowerLevel}]";
                        }

                        Logger.LogDebug(debugText);
                    }
                }
                count++;
            }
        }

        private float DebugTimer = 0f;

        public readonly Dictionary<string, Squad> Squads = new();

        public Squad GetSquad(BotOwner botOwner)
        {
            Squad result = null;
            var group = botOwner.BotsGroup;
            if (group != null)
            {
                int groupCount = group.MembersCount;

                if (SAINPlugin.DebugMode)
                    Logger.LogDebug($"Member Count: {groupCount} Checking for existing squad object");

                for (int i = 0; i < groupCount; i++)
                {
                    var defaultMember = group.Member(i);
                    if (defaultMember != null &&
                        defaultMember.ProfileId != botOwner.ProfileId)
                    {
                        if (BotController.GetSAIN(defaultMember, out var sainComponent))
                        {
                            if (SAINPlugin.DebugMode)
                                Logger.LogInfo($"Found SAIN Bot for squad");

                            result = sainComponent.Squad.SquadInfo;
                            if (result != null)
                            {
                                if (SAINPlugin.DebugMode)
                                    Logger.LogInfo($"Adding bot to squad [{result.GUID}]");

                                break;
                            }
                        }
                    }
                }
            }

            if (result == null)
            {
                result = new Squad();
                if (SAINPlugin.DebugMode)
                    Logger.LogWarning($"Created New Squad [{result.GUID}]");

                if (!Squads.ContainsKey(result.GUID))
                {
                    result.OnSquadEmpty += removeSquad;
                    Squads.Add(result.GUID, result);
                }
            }
            return result;
        }

        private void removeSquad(Squad squad)
        {
            if (squad != null)
            {
                squad.OnSquadEmpty -= removeSquad;
                squad.Dispose();
            }
        }
    }
}
