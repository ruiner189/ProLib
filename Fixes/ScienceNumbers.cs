using Battle.Enemies;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace Promethium.Fixes
{
    [HarmonyPatch(typeof(Enemy), "UpdateHealthBar")]
    public static class ScienceHealthNumbers
    {
        private static bool Prefix(Enemy __instance, UpdateSlider ___HealthBarBarSprite, TextMeshProUGUI ____healthText)
        {
            if (___HealthBarBarSprite != null)
            {
                ___HealthBarBarSprite.UpdateSize(Mathf.Max(__instance.CurrentHealth / __instance.maxHealth, 0f));
                String minHealth = __instance.CurrentHealth.ToString();
                String maxHealth = __instance.maxHealth.ToString();

                if (__instance.CurrentHealth >= 100000)
                    minHealth = __instance.CurrentHealth.ToString("0.##e0"); // Any more and it displays numbers weird
                if (__instance.maxHealth >= 100000)
                    maxHealth = __instance.maxHealth.ToString("0.##e0");
                ____healthText.text = minHealth + "/" + maxHealth;
            }
            return false;
        }
    }
}
