using System;
using System.Collections.Generic;
using System.Text;
using EFT;

namespace SAIN.Extensions;

public static class BotExtensions
{
    public static bool IsBotActive(this BotOwner botOwner)
    {
        if (botOwner == null && botOwner.StandBy == null)
        {
            return false;
        }

        var standByType = botOwner.StandBy.StandByType;

        if (standByType == BotStandByType.none || standByType == BotStandByType.active)
        {
            return true;
        }

        return false;
    }
}
