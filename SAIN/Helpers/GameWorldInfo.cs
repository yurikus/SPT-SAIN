using System.Collections.Generic;
using Comfort.Common;
using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Helpers;

internal class GameWorldInfo
{
    public static bool IsEnemyMainPlayer(Enemy enemy)
    {
        Player player = enemy?.EnemyPlayer;
        Player mainPlayer = GameWorld?.MainPlayer;
        return player != null && mainPlayer != null && player.ProfileId == mainPlayer.ProfileId;
    }

    public static Player GetAlivePlayer(IPlayer person)
    {
        return GetAlivePlayer(person?.ProfileId);
    }

    public static Player GetAlivePlayer(string profileID)
    {
        return GameWorld?.GetAlivePlayerByProfileID(profileID);
    }

    public static GameWorld GameWorld
    {
        get { return Singleton<GameWorld>.Instance; }
    }

    public static List<Player> AlivePlayers
    {
        get { return GameWorld?.AllAlivePlayersList; }
    }

    public static Dictionary<string, Player> AlivePlayersDictionary
    {
        get { return GameWorld?.allAlivePlayersByID; }
    }
}
