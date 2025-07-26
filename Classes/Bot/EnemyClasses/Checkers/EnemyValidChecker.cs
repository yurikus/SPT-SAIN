using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyValidChecker : EnemyBase, IBotClass
    {
        public bool CheckValid()
        {
            if (!WasValid) return false;
            WasValid = isValid();
            return WasValid;
        }

        public bool WasValid { get; private set; } = true;

        public EnemyValidChecker(EnemyData enemy) : base(enemy)
        {
            CanEverTick = false;
        }

        private bool isValid()
        {
            var enemyPlayerComp = EnemyPlayerComponent;
            if (enemyPlayerComp == null || enemyPlayerComp.Player == null)
            {
                //Logger.LogError($"Enemy {Enemy.EnemyName} PlayerComponent is Null");
                return false;
            }
            if (!enemyPlayerComp.Player.HealthController.IsAlive)
            {
                //Logger.LogDebug("Enemy Player Is Dead");
                return false;
            }
            // Checks specific to bots
            BotOwner enemyBotOwner = enemyPlayerComp.BotOwner;
            if (enemyPlayerComp.IsAI && enemyBotOwner == null)
            {
                if (enemyBotOwner == null)
                {
                    //Logger.LogDebug("Enemy is AI, but BotOwner is null. Removing...");
                    return false;
                }
                if (enemyBotOwner.ProfileId == BotOwner.ProfileId)
                {
                    //Logger.LogWarning("Enemy has same profile id as Bot? Removing...");
                    return false;
                }
            }
            return true;
        }
    }
}