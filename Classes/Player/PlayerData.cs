namespace SAIN.Components.PlayerComponentSpace
{
    public class OtherPlayerData(string id, PlayerComponent Component)
    {
        public readonly string ProfileId = id;
        public readonly PlayerComponent OtherPlayerComponent = Component;
        public PlayerDistanceData DistanceData { get; } = new PlayerDistanceData(Component);

        public bool IsInHearingRadius_Footsteps => DistanceData.Distance <= 100.0f;
        public bool IsInHearingRadius_GunFire => DistanceData.Distance <= 500.0f;
    }
}