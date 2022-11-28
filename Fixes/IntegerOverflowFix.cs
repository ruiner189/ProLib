using HarmonyLib;
using UnityEngine;

namespace ProLib.Fixes
{
    [HarmonyPatch(typeof(Mathf), nameof(Mathf.FloorToInt))]
    public static class IntegerOverflowFix
    {
        public static void Postfix(float f, ref int __result)
        {
            if (__result == int.MinValue && f > 0)
            {
                __result = int.MaxValue;
            }
        }
    }
}
