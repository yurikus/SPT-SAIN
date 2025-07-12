using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyValidChecker : EnemyBase, IBotClass
    {
        public bool CheckValid()
        {
            if (!WasValid)
            {
                return false;
            }
            WasValid = isValid();
            if (!WasValid)
            {
                Enemy.Events.SetEnemyAsInvalid();
            }
            return WasValid;
        }

        public bool WasValid { get; private set; } = true;

        public EnemyValidChecker(EnemyData enemy) : base(enemy)
        {
            CanEverTick = false;
        }

        private bool isValid()
        {
            var component = EnemyPlayerComponent;
            if (component == null)
            {
                //Logger.LogError($"Enemy {Enemy.EnemyName} PlayerComponent is Null");
                return false;
            }
            if (component.Player?.HealthController?.IsAlive != true)
            {
                //Logger.LogDebug("Enemy Player Is Dead");
                return false;
            }
            // Checks specific to bots
            BotOwner botOwner = component.BotOwner;
            if (component?.IsAI == true && botOwner == null)
            {
                //Logger.LogDebug("Enemy is AI, but BotOwner is null. Removing...");
                return false;
            }
            if (botOwner != null && botOwner.ProfileId == BotOwner.ProfileId)
            {
                //Logger.LogWarning("Enemy has same profile id as Bot? Removing...");
                return false;
            }
            return true;
        }
    }
}
