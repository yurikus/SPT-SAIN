namespace SAIN.Components.PlayerComponentSpace
{
    public class OtherPlayerData(string id, PlayerComponent Component)
    {
        public readonly string ProfileId = id;
        public readonly PlayerComponent PlayerComponent = Component;
        public PlayerDistanceData DistanceData { get; } = new PlayerDistanceData();
    }
}