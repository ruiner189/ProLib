using DG.Tweening;
using ProLib.Attributes;
using HarmonyLib;
using Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ToolBox.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Battle.Attacks;
using ProLib.Fixes;

namespace ProLib.Relics
{
    public static class CustomRelicManager
    {
        public static readonly Dictionary<CustomRelic, int> RelicCountdownValues = new Dictionary<CustomRelic, int>();
        public static readonly Dictionary<CustomRelic, int> RelicRemainingCountdowns = new Dictionary<CustomRelic, int>();
        public static readonly Dictionary<CustomRelic, int> RelicUsesPerBattleCounts = new Dictionary<CustomRelic, int>();
        public static readonly Dictionary<CustomRelic, int> RelicRemainingUsesPerBattle = new Dictionary<CustomRelic, int>();
        public static readonly Dictionary<CustomRelic, int> OrderOfRelicsObtained = new Dictionary<CustomRelic, int>();
        public static readonly Dictionary<CustomRelic, RelicIcon> RelicIcons = new Dictionary<CustomRelic, RelicIcon>();
        public static readonly List<CustomRelic> OwnedRelics = new List<CustomRelic>();
        public static readonly HashSet<String> UnlockedRelics = new HashSet<String>();

        public static void AddCountdown(CustomRelic relic, int value)
        {
            RelicCountdownValues[relic] = value;
        }

        public static void AddUses(CustomRelic relic, int value)
        {
            RelicUsesPerBattleCounts[relic] = value;
        }

        public static void Reset()
        {
            RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
            foreach (CustomRelic relic in OwnedRelics)
            {
                relic.OnRelicRemoved(relicManager);
            }
            RelicUsesPerBattleCounts.Clear();
            RelicRemainingUsesPerBattle.Clear();
            OrderOfRelicsObtained.Clear();
            OwnedRelics.Clear();
            RelicManager.OnRelicsReset?.Invoke(null);
        }

        public static bool RelicActive(string id)
        {
            if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
            {
                return RelicActive(relic);
            }
            return false;
        }

        public static bool RelicActive(CustomRelic relic)
        {
           return OwnedRelics.Contains(relic);
        }

        public static bool AttemptUseRelic(string id)
        {
            if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
            {
                return AttemptUseRelic(relic);
            }
            return false;
        }

        public static bool AttemptUseRelic(CustomRelic relic)
        {
            if (!OwnedRelics.Contains(relic))
            {
                return false;
            }

            if (RelicCountdownValues.ContainsKey(relic))
            {
                int num = RelicRemainingCountdowns.ContainsKey(relic) ? RelicRemainingCountdowns[relic] : RelicCountdownValues[relic];
                if (--num <= 0)
                {
                    RelicManager.OnRelicUsed?.Invoke(relic);
                    RelicRemainingCountdowns[relic] = RelicCountdownValues[relic];
                    return true;
                }
                RelicRemainingCountdowns[relic] = num;
                RelicManager.OnCountdownDecremented(relic, num);
                return false;
            }
            else
            {
                if (!RelicUsesPerBattleCounts.ContainsKey(relic))
                {
                    RelicManager.OnRelicUsed(relic);
                    return true;
                }
                int num2 = RelicRemainingUsesPerBattle.ContainsKey(relic) ? RelicRemainingUsesPerBattle[relic] : RelicUsesPerBattleCounts[relic];
                if (--num2 >= 0)
                {
                    RelicManager.OnRelicUsed?.Invoke(relic);
                    RelicRemainingUsesPerBattle[relic] = num2;
                    if (num2 == 0)
                    {
                        RelicManager.OnRelicDisabled?.Invoke(relic);
                    }
                    return true;
                }
                return false;
            }
        }

        [Register]
        public static void RegisterDelegates()
        {
            RelicManager.OnRelicAdded += new RelicManager.RelicManagement(OnRelicAddedHandler);
            RelicManager.OnRelicRemoved += new RelicManager.RelicManagement(OnRelicRemovedHandler);
            RelicManager.OnRelicUsed += new RelicManager.RelicManagement(OnRelicUsedHandler);
            RelicManager.OnRelicEnabled += new RelicManager.RelicManagement(OnRelicEnabledHandler);
            RelicManager.OnRelicDisabled += new RelicManager.RelicManagement(OnRelicDisabledHandler);
            RelicManager.OnCountdownDecremented += new RelicManager.CountdownRelicEvent(OnCountdownDecrementedHandler);
        }

        private static void OnRelicAddedHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicAdded(relicManager);
            }
        }

        private static void OnRelicRemovedHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicRemoved(relicManager);
            }
        }

        private static void OnRelicUsedHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicUsed(relicManager);
            }
        }

        private static void OnRelicEnabledHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicEnabled(relicManager);
            }
        }

        private static void OnRelicDisabledHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicDisabled(relicManager);
            }
        }

        private static void OnCountdownDecrementedHandler(Relic relic, int remainingCountdown)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnCountdownDecremented(relicManager, remainingCountdown);
            }
        }

        private static float GetDamageModifier(Attack attack, int critCount, float damage)
        {
            foreach (CustomRelic relic in OwnedRelics)
            {
                damage += relic.DamageModifier(attack, critCount);
            }

            return damage;
        }

        private static float GetCritModifier(Attack attack, int critCount, float damage)
        {
            foreach (CustomRelic relic in OwnedRelics)
            {
                damage += relic.CritModifier(attack, critCount);
            }

            return damage;
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.GetModifiedDamagePerPeg))]
        private class ChangeAttackPerPeg
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);
                int insertionIndex = 0;
                // Checking where the first relicManager is null. We can use that as an anchor for where we insert our code, as it is nearby.
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldfld && code[i].operand == (object)AccessTools.Field(typeof(Attack), "_relicManager"))
                    {
                        insertionIndex = i + 4;
                        break;
                    }
                }

                #region Change Attack
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load this (attack)

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Load critCount

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load local variable num

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRelicManager), nameof(GetDamageModifier)))); // Call Method CustomRelicManager::GetDamageModifier
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set local variable num to the return of CustomRelic::DamageModifier
                #endregion

                #region Change Crit
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load this (attack)

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Load critCount

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloc_2)); // Load local variable num2

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRelicManager), nameof(GetCritModifier)))); // Call Method CustomRelicManager::CritModifier
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Stloc_2)); // Set local variable num2 to the return of CustomRelic::DamageModifier
                #endregion

                code.InsertRange(insertionIndex, instructionsToInsert);


                return code;
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.AddRelic))]
        private static class RelicAdded
        {
            private static void Postfix(RelicManager __instance, Relic relic)
            {
                if (relic is CustomRelic customRelic)
                {
                    if(customRelic == __instance.consolationPrize)
                    {
                        TrophyStack(__instance, customRelic);
                        return;
                    }
                    UnlockedRelics.Add(customRelic.Id);

                    if (!OwnedRelics.Contains(customRelic))
                    {
                        if (RelicCountdownValues.ContainsKey(customRelic))
                        {
                            RelicRemainingCountdowns[customRelic] = RelicCountdownValues[customRelic];
                        }
                        if (RelicUsesPerBattleCounts.ContainsKey(customRelic))
                        {
                            RelicRemainingUsesPerBattle[customRelic] = RelicUsesPerBattleCounts[customRelic];
                        }
                        OwnedRelics.Add(customRelic);
                        OrderOfRelicsObtained.Add(customRelic, __instance._orderCounter);
                        __instance._orderCounter++;

                        __instance._availableCommonRelics.Remove(relic);
                        __instance._availableRareRelics.Remove(relic);
                        __instance._availableBossRelics.Remove(relic);
                    }
                }
            }

            private static void TrophyStack(RelicManager relicManager, CustomRelic trophy)
            {
                int stack = RelicRemainingCountdowns.ContainsKey(trophy) ? RelicRemainingCountdowns[trophy] + 1 : 1;
                RelicRemainingCountdowns[trophy] = stack;

                if (stack == 1)
                {
                    OwnedRelics.Add(trophy);
                    OrderOfRelicsObtained.Add(trophy, relicManager._orderCounter);
                    relicManager._orderCounter++;
                    RelicManager.OnRelicAdded(trophy);
                }

                RelicManager.OnCountdownDecremented(trophy, stack);


                
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ResetBattleRelics))]
        private static class ResetBattleRelics
        {
            private static void Postfix()
            {
                foreach (KeyValuePair<CustomRelic, int> key in RelicUsesPerBattleCounts)
                {
                    RelicRemainingUsesPerBattle[key.Key] = key.Value;
                    if (OwnedRelics.Contains(key.Key))
                    {
                        RelicManager.OnRelicEnabled?.Invoke(key.Key);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.Reset))]
        private static class RelicManagerReset
        {
            private static void Prefix(RelicManager __instance)
            {
                Reset();
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.ArmBallForShot))]
        private static class ArmBallForShot
        {
            [HarmonyPriority(Priority.Low)]
            private static void Prefix(BattleController __instance)
            {
                foreach (Relic relic in __instance._relicManager._ownedRelics.Values)
                {
                    if (relic is CustomRelic customRelic)
                    {
                        customRelic.OnArmBallForShot(__instance);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.RemoveRelic))]
        private static class RemoveRelicIcon
        {
            private static bool Prefix(Relic toRemove)
            {
                if(toRemove is CustomRelic customRelic)
                {
                    RelicIcon relicIcon = RelicIcons[customRelic];
                    if (relicIcon != null)
                    {
                        relicIcon.Remove();
                    }
                    RelicIcons.Remove(customRelic);
                    return false;
                }

                return true;

            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.RemoveAll))]
        private static class RemoveAllRelicIcons
        {
            public static void Prefix()
            {
                foreach (RelicIcon relicIcon in RelicIcons.Values)
                {
                    if (relicIcon != null)
                        UnityEngine.Object.Destroy(relicIcon.gameObject);
                }
                RelicIcons.Clear();
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.AddRelic))]
        private static class AddRelicIcon
        {
            private static bool Prefix(RelicUI __instance, Relic toAdd)
            {
                if(toAdd is CustomRelic customRelic)
                {

                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.relicIconPrefab);
                    RelicIcon component = gameObject.GetComponent<RelicIcon>();

                    if (customRelic.CustomRelicIconType != typeof(RelicIcon))
                    {
                        if(component != null)
                            GameObject.DestroyImmediate(gameObject.GetComponent<RelicIcon>());
                        component = (RelicIcon) gameObject.AddComponent(customRelic.CustomRelicIconType);
                    }

                    if(component == null)
                    {
                        Plugin.Log.LogWarning($"Failed to generate RelicIcon for {customRelic.Id}");
                        return false;
                    }

                    component.SetRelic(toAdd);
                    RelicIcons[customRelic] = component;     
                    gameObject.transform.SetParent(__instance.gameObject.transform, false);
                    gameObject.GetComponent<Image>().DOFade(1f, 0.5f).From(0f, true, false);
                    return false;

                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.UseRelic))]
        private static class UseRelicIcon
        {
            private static bool Prefix(RelicUI __instance, Relic relic)
            {
                if(relic is CustomRelic customRelic)
                {
                    if (RelicIcons.ContainsKey(customRelic))
                    {
                        RelicIcons[customRelic].Flash();
                        if (RelicCountdownValues.ContainsKey(customRelic))
                        {
                            RelicIcons[customRelic].ResetCountdownText();
                        }
                    }
                    if (relic.useSfx != null)
                    {
                        __instance._audioSource.PlayOneShot(relic.useSfx);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.EnableRelic))]
        private static class EnableRelicIcon
        {
            private static bool Prefix(Relic relic)
            {
                if(relic is CustomRelic customRelic)
                {
                    if (RelicIcons.ContainsKey(customRelic) && RelicIcons[customRelic].grayScaleActive)
                    {
                        RelicIcons[customRelic].RemoveGrayscale();
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.DisableRelic))]
        private static class DisableRelicIcon
        {
            private static bool Prefix(Relic relic)
            {
                if (relic is CustomRelic customRelic)
                {
                    if (RelicIcons.ContainsKey(customRelic) && !RelicIcons[customRelic].grayScaleActive)
                    {
                        RelicIcons[customRelic].ApplyGrayscale();
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.UpdateRelicCountdown))]
        private static class UpdateRelicCountdown
        {
            private static bool Prefix(Relic relic, int countdown)
            {
                if(relic is CustomRelic customRelic)
                {
                    RelicIcons[customRelic].SetCountdownText(countdown.ToString());
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicIcon), nameof(RelicIcon.SetRelic))]
        private static class SetRelicForIcon
        {
            private static bool Prefix(RelicIcon __instance, Relic r)
            {
                if(r is CustomRelic customRelic)
                {
                    __instance.relic = r;
                    __instance._image.sprite = r.sprite;
                    if (__instance._countdownText == null)
                    {
                        return false;
                    }
                    if (RelicCountdownValues.ContainsKey(customRelic))
                    {
                        __instance.SetCountdownText(RelicCountdownValues[customRelic].ToString());
                        return false;
                    }
                    __instance._countdownText.enabled = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicIcon), nameof(RelicIcon.ResetCountdownText))]
        private static class ResetCountdown
        {
            private static bool Prefix(RelicIcon __instance)
            {
                if(__instance.relic is CustomRelic customRelic)
                {
                    __instance.SetCountdownText(RelicCountdownValues[customRelic].ToString());
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.SaveRelicData))]
        private static class Save
        {
            private static void Prefix(RelicManager __instance)
            {
                if (OwnedRelics == null)
                {
                    return;
                }
                List<CustomRelicManagerSaveData.CustomRelicSaveObject> list = new List<CustomRelicManagerSaveData.CustomRelicSaveObject>();
                foreach (CustomRelic relic in OwnedRelics)
                {
                    CustomRelicManagerSaveData.CustomRelicSaveObject item = new CustomRelicManagerSaveData.CustomRelicSaveObject(relic.Id, OrderOfRelicsObtained[relic]);
                    list.Add(item);
                }
                List<CustomRelicManagerSaveData.CustomRelicCountdownStatus> list2 = CreateRelicCountdownsForSave();
                CustomRelicManagerSaveData.CustomRelicPoolSaveObject relicPoolSaveObject = GetRelicPoolSaveObject(__instance._availableCommonRelics);
                CustomRelicManagerSaveData.CustomRelicPoolSaveObject relicPoolSaveObject2 = GetRelicPoolSaveObject(__instance._availableRareRelics);
                CustomRelicManagerSaveData.CustomRelicPoolSaveObject relicPoolSaveObject3 = GetRelicPoolSaveObject(__instance._availableBossRelics);
                new CustomRelicManagerSaveData(list.ToArray(), list2.ToArray(), relicPoolSaveObject, relicPoolSaveObject2, relicPoolSaveObject3).Save();
            }

            private static List<CustomRelicManagerSaveData.CustomRelicCountdownStatus> CreateRelicCountdownsForSave()
            {
                List<CustomRelicManagerSaveData.CustomRelicCountdownStatus> list = new List<CustomRelicManagerSaveData.CustomRelicCountdownStatus>();
                foreach (KeyValuePair<CustomRelic, int> pair in RelicRemainingCountdowns)
                {
                    CustomRelicManagerSaveData.CustomRelicCountdownStatus item = new CustomRelicManagerSaveData.CustomRelicCountdownStatus(pair.Key.Id, pair.Value);
                    list.Add(item);
                }
                return list;
            }

            private static CustomRelicManagerSaveData.CustomRelicPoolSaveObject GetRelicPoolSaveObject(List<Relic> pool)
            {
                List<String> list = new List<String>();
                foreach (Relic relic in pool)
                {
                    if (relic is CustomRelic customRelic)
                    {
                        list.Add(customRelic.Id);
                    }
                }
                return new CustomRelicManagerSaveData.CustomRelicPoolSaveObject(list.ToArray());
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.LoadRelicData))]
        private static class Load
        {
            private static bool Prefix(RelicManager __instance)
            {
                RelicManager.RelicManagerSaveData relicManagerSaveData = (RelicManager.RelicManagerSaveData)DataSerializer.Load<SaveObjectData>(RelicManager.RelicManagerSaveData.KEY);
                CustomRelicManagerSaveData customRelicManagerSaveData = (CustomRelicManagerSaveData)DataSerializer.Load<SaveObjectData>(CustomRelicManagerSaveData.KEY);

                List<Relic> relics = new List<Relic>();

                if (relicManagerSaveData != null)
                {
                    List<RelicManager.RelicSaveObject> list = (
                        from x in relicManagerSaveData.Relics                              
                        orderby x.OrderOfAcquisition                              
                        select x
                    ).ToList<RelicManager.RelicSaveObject>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        foreach (Relic relic in __instance._globalRelics.relics)
                        {
                            if (relic.effect == (RelicEffect)list[i].RelicEffect)
                            {
                                relics.Add(relic);
                            }
                        }
                    }

                    foreach (Relic relic2 in __instance._globalRelics.relics)
                    {
                        if (relicManagerSaveData.CommonRelicPool.RelicEffects.Contains((int)relic2.effect))
                        {
                            __instance._availableCommonRelics.Add(relic2);
                        }
                        else if (relicManagerSaveData.RareRelicPool.RelicEffects.Contains((int)relic2.effect))
                        {
                            __instance._availableRareRelics.Add(relic2);
                        }
                        else if (relicManagerSaveData.BossRelicPool.RelicEffects.Contains((int)relic2.effect))
                        {
                            __instance._availableBossRelics.Add(relic2);
                        }
                    }
                }

                if (customRelicManagerSaveData != null)
                {
                    List<CustomRelicManagerSaveData.CustomRelicSaveObject> list = (
                            from x in customRelicManagerSaveData.Relics
                            orderby x.OrderOfAcquisition
                            select x
                     ).ToList();


                    for (int i = 0; i < list.Count; i++)
                    {
                        foreach (Relic relic in __instance._globalRelics.relics)
                        {
                            if (relic is CustomRelic customRelic && customRelic.Id == list[i].RelicId)
                            {
                                relics.Insert(list[i].OrderOfAcquisition, relic);
                            }
                        }
                    }

                    foreach (string id in customRelicManagerSaveData.CommonRelicPool.RelicIds)
                    {
                        if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
                            __instance._availableCommonRelics.Add(relic);
                    }

                    foreach (string id in customRelicManagerSaveData.RareRelicPool.RelicIds)
                    {
                        if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
                            __instance._availableRareRelics.Add(relic);
                    }
                    foreach (string id in customRelicManagerSaveData.BossRelicPool.RelicIds)
                    {
                        if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
                            __instance._availableBossRelics.Add(relic);
                    }
                }

                foreach (Relic relic in relics)
                {
                    LoadRelicFromSaveFile(__instance, relic);
                }

                if (relicManagerSaveData != null)
                    __instance.LoadRelicCountdownsFromSaveFile(relicManagerSaveData.RelicCountdowns);
                if (customRelicManagerSaveData != null)
                    LoadRelicCountdownsFromSaveFile(customRelicManagerSaveData.RelicCountdowns);

                return false;
            }
        }

        private static void LoadRelicFromSaveFile(RelicManager relicManager, Relic relic)
        {
            if (relicManager._ownedRelics == null)
            {
                relicManager.Reset();
            }

            if (OwnedRelics == null)
            {
                Reset();
            }

            if (relic is CustomRelic customRelic)
            {
                OwnedRelics.Add(customRelic);
                OrderOfRelicsObtained.Add(customRelic, relicManager._orderCounter);
            }
            else if (relic.effect != RelicEffect.NONE)
            {
                relicManager._ownedRelics.Add(relic.effect, relic);
                relicManager._orderOfRelicsObtained.Add(relic.effect, relicManager._orderCounter);
            }

            relicManager._orderCounter++;
            relicManager._availableCommonRelics.Remove(relic);
            relicManager._availableRareRelics.Remove(relic);
            relicManager._availableBossRelics.Remove(relic);

            RelicManager.OnRelicAdded(relic);
        }

        private static void LoadRelicCountdownsFromSaveFile(CustomRelicManagerSaveData.CustomRelicCountdownStatus[] countdowns)
        {
            foreach (CustomRelicManagerSaveData.CustomRelicCountdownStatus relicCountdownStatus in countdowns)
            {
                if (CustomRelic.TryGetCustomRelic(relicCountdownStatus.RelicId, out CustomRelic relic))
                {
                    if (RelicCountdownValues.ContainsKey(relic))
                    {
                        RelicRemainingCountdowns[relic] = RelicCountdownValues[relic];
                    }
                    RelicRemainingCountdowns[relic] = relicCountdownStatus.RelicCountdown;
                    if (OwnedRelics.Contains(relic))
                    {
                        RelicManager.OnCountdownDecremented?.Invoke(relic, relicCountdownStatus.RelicCountdown);
                    }
                }

            }
        }
    }
}
