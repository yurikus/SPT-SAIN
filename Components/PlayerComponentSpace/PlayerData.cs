namespace SAIN.Components.PlayerComponentSpace
{
    public class OtherPlayerData
    {
        public readonly string ProfileId;
        public readonly PlayerComponent PlayerComponent;
        public PlayerDistanceData DistanceData { get; } = new PlayerDistanceData();

        public OtherPlayerData(string id, PlayerComponent Component)
        {
            ProfileId = id;
            PlayerComponent = Component;
        }
    }
}