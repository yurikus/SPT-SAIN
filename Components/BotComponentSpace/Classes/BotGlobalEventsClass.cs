using System;

namespace SAIN.SAINComponent.Classes
{
    public class BotGlobalEventsClass : BotComponentClassBase
    {
        public event Action<BotComponent> OnEnterPeace;

        public event Action<BotComponent> OnExitPeace;

        public event Action<BotComponent, NavGraphVoxelSimple, NavGraphVoxelSimple> OnVoxelChanged;

        public BotGlobalEventsClass(BotComponent sain) : base(sain)
        {
            CanEverTick = false;
        }

        public override void Init()
        {
            Bot.EnemyController.Events.OnPeaceChanged.OnToggle += PeaceChanged;
            Bot.DoorOpener.DoorFinder.OnNewVoxel += onVoxelChange;
            base.Init();
        }

        public override void Dispose()
        {
            Bot.EnemyController.Events.OnPeaceChanged.OnToggle -= PeaceChanged;
            Bot.DoorOpener.DoorFinder.OnNewVoxel -= onVoxelChange;
            base.Dispose();
        }

        private void onVoxelChange(NavGraphVoxelSimple newVoxel, NavGraphVoxelSimple oldVoxel)
        {
            OnVoxelChanged?.Invoke(Bot, newVoxel, oldVoxel);
        }

        public void PeaceChanged(bool value)
        {
            if (value)
                OnEnterPeace?.Invoke(Bot);
            else
                OnExitPeace?.Invoke(Bot);
        }
    }
}