using SAIN.Preset;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN
{
    public interface IBot
    {
        BotComponent Bot { get; }
        void Init();
        void Dispose();
    }

    public interface IBotClass : IBot
    {
        void Update();
    }

    public interface IBotDecisionClass : IBot
    {
        bool GetDecision(Enemy enemy, out string reason);

        void UpdatePresetSettings(SAINPresetClass preset);
    }


    public interface IBotEnemyClass : IBotClass
    {
        void OnEnemyKnownChanged(bool known, Enemy enemy);
    }
}
