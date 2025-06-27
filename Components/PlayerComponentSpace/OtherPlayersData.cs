using SAIN.SAINComponent;
using System.Collections.Generic;

namespace SAIN.Components.PlayerComponentSpace
{
    public class OtherPlayersData : PlayerComponentBase
    {
        public readonly Dictionary<string, OtherPlayerData> Datas = new();

        public OtherPlayersData(PlayerComponent playerComponent) : base(playerComponent)
        {
        }

        public void Init()
        {
            _spawns.OnPlayerAdded += playerAdded;
            _spawns.OnPlayerRemoved += playerRemoved;
            createExistingPlayers();
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            _spawns.OnPlayerAdded -= playerAdded;
            _spawns.OnPlayerRemoved -= playerRemoved;
            Datas.Clear();
        }

        private void createExistingPlayers()
        {
            foreach (var player in _spawns.AlivePlayers.Values)
            {
                playerAdded(player);
            }
        }

        private void playerAdded(PlayerComponent playerComp)
        {
            if (playerComp == null)
            {
                return;
            }
            string profileId = playerComp.ProfileId;
            if (profileId == PlayerComponent.ProfileId)
            {
                return;
            }
            if (Datas.ContainsKey(profileId))
            {
                return;
            }
            Datas.Add(profileId, new OtherPlayerData(profileId));
        }

        private void playerRemoved(string profileId, PlayerComponent playerComp)
        {
            Datas.Remove(profileId);
        }

        private static PlayerSpawnTracker _spawns => GameWorldComponent.Instance.PlayerTracker;
    }
}