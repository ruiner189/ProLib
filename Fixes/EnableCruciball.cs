using Cruciball;
using HarmonyLib;
using Peglin.Achievements;
using PeglinUI.LoadoutManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProLib.Fixes
{
    public static class EnableCruciball
    {
        public static bool HasCustomLoadout = false;

        [HarmonyPatch(typeof(LoadoutManager), nameof(LoadoutManager.SetupDataForNewGame))]
        private static class GetCustomLoadoutStatus
        {
            public static void Prefix(LoadoutManager __instance)
            {
                HasCustomLoadout = __instance._hasCustomLoadout;
            }
        }

        [HarmonyPatch(typeof(CruciballManager), nameof(CruciballManager.CruciballVictoryAchieved))]
        private static class AllowCruciball
        {
            public static void Prefix()
            {
                if (!HasCustomLoadout)
                {
                    AchievementManager.AchievementsOn = true;
                }
            }

            public static void Postfix()
            {
                // We can disable without checks because achievements should be off while playing modded.
                AchievementManager.AchievementsOn = false;
            }
        }
    }
}
