using EFT;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public abstract class PersonBase(PlayerData inPlayerData)
    {
        public PlayerData PlayerData { get; } = inPlayerData;
        public PlayerComponent PlayerComponent => PlayerData.PlayerComponent;
        public IPlayer IPlayer => PlayerData.IPlayer;
        public Player Player => PlayerData.Player;
        public string ProfileId => PlayerData.Profile.ProfileId;
        public string Nickname => PlayerData.Profile.Nickname;
    }
}