using HarmonyLib;
using ProLib.Relics;
using Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProLib.Fixes
{
    [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.AddRelic))]
    public static class TrophyStacking
    {
        public static bool Prefix(RelicManager __instance, ref Relic relic)
        {
            if (relic == __instance.consolationPrize)
            {
                return false;
            }

            return true;
        }
    }
}
