using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;

namespace SAIN.SAINComponent
{
    public abstract class BotBase
    {
        public BotComponent Bot { get; }
        public PersonClass Person => Bot.Person;
        public PlayerComponent PlayerComponent => Bot.PlayerComponent;
        public BotOwner BotOwner => Bot.BotOwner;
        public Player Player => Bot.Player;
        public IPlayer IPlayer => Bot.Person.IPlayer;

        protected GlobalSettingsClass GlobalSettings => GlobalSettingsClass.Instance;
        protected SAINPresetClass Preset => SAINPresetClass.Instance;

        public BotBase(BotComponent bot)
        {
            Bot = bot;
        }

        protected virtual void SubscribeToPreset(Action<SAINPresetClass> func)
        {
            if (func != null)
            {
                func.Invoke(SAINPresetClass.Instance);
                _autoUpdater.Subscribe(func);
                Bot.OnDispose += this.UnSubscribeToPreset;
            }
        }

        protected virtual void UnSubscribeToPreset()
        {
            if (_autoUpdater.Subscribed)
            {
                _autoUpdater.UnSubscribe();
                Bot.OnDispose -= this.UnSubscribeToPreset;
            }
        }

        protected readonly PresetAutoUpdater _autoUpdater = new();
    }

    public abstract class BotSubClass<T> : BotBase where T : IBotClass
    {
        protected T BaseClass { get; }

        public BotSubClass(T sainClass) : base(sainClass.Bot)
        {
            BaseClass = sainClass;
        }
    }
}