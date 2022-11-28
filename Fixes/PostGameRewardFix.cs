using Battle.Attacks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProLib.Fixes
{
    [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.GetOrbPrefabs))]
    public static class PostGameRewardFix
    {
        private static void Prefix(DeckManager __instance, List<String> attackNames)
        {
			if (attackNames == null || attackNames.Count == 0) return;

			List<String> validNames = new List<String>();
			foreach (GameObject gameObject in __instance.CommonOrbPool)
			{
				validNames.Add(gameObject.GetComponent<Attack>().locNameString);
			}
			foreach (GameObject gameObject in __instance.UncommonOrbPool)
			{
				validNames.Add(gameObject.GetComponent<Attack>().locNameString);
			}
			foreach (GameObject gameObject in __instance.RareOrbPool)
			{
				validNames.Add(gameObject.GetComponent<Attack>().locNameString);
			}
			foreach (GameObject gameObject in __instance.SpecialOrbPool)
			{
				validNames.Add(gameObject.GetComponent<Attack>().locNameString);
			}

			attackNames.RemoveAll(name => !validNames.Contains(name));
		}
    }
}
