using Coffee.UIEffects;
using HarmonyLib;
using PeglinUI.LoadoutManager;
using Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProLib.Relics
{
    public static class CustomLoadoutPatches
    {
        [HarmonyPatch(typeof(LoadoutIcon), nameof(LoadoutIcon.InitializeRelic))]
        public static class UnlockRelics
        {
            public static bool Prefix(LoadoutIcon __instance, Relic r)
            {
                CustomRelic customRelic = r as CustomRelic;
                if (customRelic != null || Plugin.AllItemsUnlocked)
                {
                    __instance._relic = r;
                    __instance.isUnlocked = Plugin.AllItemsUnlocked || customRelic.AlwaysUnlocked || CustomRelicManager.Instance.UnlockedRelics.Contains(customRelic.Id);
                    __instance.image.sprite = r.sprite;
                    if (__instance.dropShadow != null)
                    {
                        __instance.dropShadow.sprite = r.sprite;
                    }
                    __instance.image.color = (__instance.isUnlocked ? Color.white : __instance.lockedColor);
                    __instance._imageFillEffect = __instance.image.GetComponent<UIEffect>();
                    if (__instance._imageFillEffect != null)
                    {
                        __instance._imageFillEffect.colorFactor = (__instance.isUnlocked ? 0f : 1f);
                    }
                    if (__instance.button != null && __instance.isUnlocked)
                    {
                        __instance.button.onClick.RemoveAllListeners();
                        __instance.button.onClick.AddListener(delegate ()
                        {
                            LoadoutIcon.RelicIconClicked onRelicIconClicked = __instance.OnRelicIconClicked;
                            if (onRelicIconClicked == null)
                            {
                                return;
                            }
                            onRelicIconClicked(r);
                        });
                    }
                    __instance.SetAmount(0);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(LoadoutEditorRelics), nameof(LoadoutEditorRelics.CreateRelicPoolIcons))]
        public static class HideRelics
        {
            public static void Prefix(List<Relic> relicPool)
            {
                relicPool.RemoveAll(relic =>
                {
                    if (relic is CustomRelic customRelic)
                    {
                        if (!customRelic.IncludeInCustomLoadout)
                            return true;
                    }
                    return false;
                });
            }
        }
    }
}
