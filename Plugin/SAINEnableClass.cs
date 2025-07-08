using EFT;
using SAIN.Components;
using SAIN.Components.BotController;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static SAIN.Helpers.EnumValues;

namespace SAIN
{
    public class SAINEnableClass
    {
        static SAINEnableClass()
        {
            GameWorld.OnDispose += Clear;
        }

        private static readonly HashSet<string> ExcludedBots = [];
        private static readonly HashSet<string> EnabledBots = [];

        /// <summary>
        /// Checks if this bot has SAIN enabled or if it is a vanilla bot.
        /// </summary>
        public static bool IsSAINDisabledForBot(BotOwner botOwner)
        {
            if (botOwner == null)
                return true;
            Player player = botOwner.GetPlayer;
            if (player == null) 
                return true;

            string id = player.ProfileId;
            if (ExcludedBots.Contains(id))
                return true;
            if (EnabledBots.Contains(id))
                return false;
            
            ProfileInfoSettingsClass settings = botOwner.Profile?.Info?.Settings;
            if (settings == null)
                return true;

            player.OnIPlayerDeadOrUnspawn += ClearBot;

            if (IsBotExcluded(botOwner)) {
                ExcludedBots.Add(id);
                Logger.LogDebug($"Added Excluded Bot [{player.Profile.Nickname},{id}]");
                return true;
            }
            EnabledBots.Add(id);
            Logger.LogDebug($"Added Enabled Bot [{player.Profile.Nickname},{id}]");
            return false;
        }

        /// <summary>
        /// Checks if this IPlayer has SAIN enabled or if it is a vanilla bot.
        /// </summary>
        public static bool IsSAINDisabledForBot(IPlayer iPlayer)
        {
            if (iPlayer == null || !iPlayer.IsAI)
                return true;

            BotOwner botOwner = iPlayer.AIData?.BotOwner;
            if (botOwner == null)
                return true;

            string id = iPlayer.ProfileId;
            if (ExcludedBots.Contains(id))
                return true;
            if (EnabledBots.Contains(id))
                return false;
            
            ProfileInfoSettingsClass settings = iPlayer.Profile?.Info?.Settings;
            if (settings == null)
                return true;

            botOwner.GetPlayer.OnIPlayerDeadOrUnspawn += ClearBot;

            if (IsBotExcluded(botOwner)) {
                ExcludedBots.Add(id);
                return true;
            }
            EnabledBots.Add(id);
            return false;
        }

        private static void Clear()
        {
            ExcludedBots.Clear();
            EnabledBots.Clear();
        }

        private static void ClearBot(IPlayer player)
        {
            if (player != null) {
                player.OnIPlayerDeadOrUnspawn -= ClearBot;
                string id = player.ProfileId;
                ExcludedBots.Remove(id);
                EnabledBots.Remove(id);
            }
        }

        public static bool IsBotExcluded(BotOwner botOwner)
        {
            var settings = botOwner.Profile?.Info?.Settings;
            if (settings == null)
                return true;

            WildSpawnType type = settings.Role;

            if (BotSpawnController.StrictExclusionList.Contains(type))
                return true;

            if (IsAlwaysEnabled(type, botOwner))
                return false;

            return ShallExludeByWildSpawnType(type, botOwner);
        }

        public static bool ShallExludeByWildSpawnType(WildSpawnType wildSpawnType, BotOwner botOwner)
        {
            return
                ExcludeOthers(wildSpawnType) ||
                ExcludeScav(wildSpawnType, botOwner) ||
                ExcludeBoss(wildSpawnType) ||
                ExcludeFollower(wildSpawnType) ||
                ExcludeGoons(wildSpawnType);
        }

        private static bool IsAlwaysEnabled(WildSpawnType wildSpawnType, BotOwner botOwner)
        {
            return
                WildSpawn.IsPMC(wildSpawnType) ||
                BotManagerComponent.Instance?.Bots?.ContainsKey(botOwner.ProfileId) == true;
        }

        private static bool ExcludeBoss(WildSpawnType wildSpawnType)
        {
            return SAINEnabled.VanillaBosses
            && !WildSpawn.IsGoons(wildSpawnType)
            && WildSpawn.IsBoss(wildSpawnType);
        }

        private static bool ExcludeGoons(WildSpawnType wildSpawnType)
        {
            return SAINEnabled.VanillaGoons
            && WildSpawn.IsGoons(wildSpawnType);
        }

        private static bool ExcludeFollower(WildSpawnType wildSpawnType)
        {
            return SAINEnabled.VanillaFollowers
            && !WildSpawn.IsGoons(wildSpawnType)
            && WildSpawn.IsFollower(wildSpawnType);
        }

        private static bool ExcludeScav(WildSpawnType wildSpawnType, BotOwner botOwner)
        {
            return SAINEnabled.VanillaScavs
            && WildSpawn.IsScav(wildSpawnType) &&
            !IsPlayerScav(botOwner.Profile);
        }

        private static bool ExcludeOthers(WildSpawnType wildSpawnType)
        {
            if (SAINEnabled.VanillaCultists &&
                WildSpawn.IsCultist(wildSpawnType)) {
                return true;
            }
            if (SAINEnabled.VanillaRogues &&
                wildSpawnType == WildSpawnType.exUsec) {
                return true;
            }
            // Raiders have the same brain type as PMCs, so I'll need a new solution to have them excluded
            //if (SAINEnabled.VanillaRaiders &&
            //    wildSpawnType == WildSpawnType.pmcBot)
            //{
            //    return true;
            //}
            if (SAINEnabled.VanillaBloodHounds) {
                if (wildSpawnType == WildSpawnType.arenaFighter ||
                    wildSpawnType == WildSpawnType.arenaFighterEvent) {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPlayerScav(Profile profile)
        {
            // Handle the old version of creating player Scavs
            if (profile.Info.Nickname.Contains(" ("))
            {
                return true;
            }
            // Check for player Scavs created by SPT
            return profile.Info.Settings.Role == WildSpawnType.assault && !string.IsNullOrEmpty(profile.Info.MainProfileNickname);
        }

        /// <summary>
        /// Is this player a sain bot, and are they also in combat state?
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsBotInCombat(IPlayer player)
        {
            return BotManagerComponent.Instance?.BotSpawnController?.GetSAIN(player.ProfileId)?.SAINLayersActive == true;
        }

        public static bool GetSAIN(BotOwner botOwner, out BotComponent sain)
        {
            sain = null;
            if (IsSAINDisabledForBot(botOwner)) {
                return false;
            }
            if (BotManagerComponent.Instance == null) {
                //Logger.LogError($"Bot Controller Null");
                return false;
            }
            return BotManagerComponent.Instance.GetSAIN(botOwner, out sain);
        }

        public static bool GetSAIN(Player player, out BotComponent sain)
        {
            return GetSAIN(player?.AIData?.BotOwner, out sain);
        }

        private static VanillaBotSettings SAINEnabled => SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaBots;
    }
}