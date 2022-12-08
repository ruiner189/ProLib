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
using Battle;
using System.Collections;
using System.Diagnostics;

namespace ProLib.Relics
{
    public class CustomRelicManager : MonoBehaviour
    {
        public readonly Dictionary<CustomRelic, int> RelicCountdownValues = new Dictionary<CustomRelic, int>();
        public readonly Dictionary<CustomRelic, int> RelicRemainingCountdowns = new Dictionary<CustomRelic, int>();
        public readonly Dictionary<CustomRelic, int> RelicUsesPerBattleCounts = new Dictionary<CustomRelic, int>();
        public readonly Dictionary<CustomRelic, int> RelicRemainingUsesPerBattle = new Dictionary<CustomRelic, int>();
        public readonly Dictionary<CustomRelic, int> OrderOfRelicsObtained = new Dictionary<CustomRelic, int>();

        public readonly Dictionary<RelicRarity, List<CustomRelic>> AvailableRelicsOfRarity = new Dictionary<RelicRarity, List<CustomRelic>>();

        public readonly Dictionary<CustomRelic, RelicIcon> RelicIcons = new Dictionary<CustomRelic, RelicIcon>();
        public readonly List<CustomRelic> OwnedRelics = new List<CustomRelic>();
        public readonly HashSet<String> UnlockedRelics = new HashSet<String>();
        private readonly List<IDamageModifier> _damageModifiers = new List<IDamageModifier>();

        public RelicManager RelicManager;
        public delegate void RelicRegister(CustomRelicManager manager);

        private bool _relicsRegistered;

        public static CustomRelicManager Instance;
        public static RelicRegister Register = delegate (CustomRelicManager manager) { };

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (this != Instance) Destroy(this);
        }

        public void Start()
        {
            StartCoroutine(DelayedStart());
        }

        public IEnumerator DelayedStart()
        {
            int attempts = 10;
            while (RelicManager == null && attempts > 0)
            {
                yield return new WaitForEndOfFrame();
                attempts--;
                RelicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
            }

            if (RelicManager != null)
            {
                if (!_relicsRegistered)
                {
                    RegisterCustomRelics();
                }
                AddRelicsToPools();
            }
            else
            {
                Plugin.Log.LogWarning("Could not find Relic Manager. Aborting Relic Registration.");
            }
        }

        private void RegisterCustomRelics()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Register(this);
            _relicsRegistered = true;

            stopwatch.Stop();
            Plugin.Log.LogInfo($"All Custom relics built! Took {stopwatch.ElapsedMilliseconds}ms");
        }

        private void AddRelicsToPools()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<CustomRelic> relics = CustomRelic.AllCustomRelics;

            RelicSet[] pools = Resources.FindObjectsOfTypeAll<RelicSet>();
            RelicSet globalRelics = null;

            foreach (RelicSet pool in pools)
            {
                if (pool.name == "GlobalRelics") globalRelics = pool;
            }

            if (globalRelics != null)
                foreach (CustomRelic relic in relics)
                {
                    if (relic.IsEnabled)
                    {
                        globalRelics.relics.Add(relic);
                    }
                }
            else
            {
                Plugin.Log.LogError("Could not find the global relic pool. Relic Registration failed.");
                return;
            }

            stopwatch.Stop();
            Plugin.Log.LogInfo($"Custom relics injected into relic pool! Took {stopwatch.ElapsedMilliseconds}ms");
        }

        public void AddCountdown(CustomRelic relic, int value)
        {
            RelicCountdownValues[relic] = value;
        }

        public void AddUses(CustomRelic relic, int value)
        {
            RelicUsesPerBattleCounts[relic] = value;
        }

        public List<CustomRelic> GetAvailableRelicsOfRarity(RelicRarity rarity)
        {
            if (!AvailableRelicsOfRarity.ContainsKey(rarity)) AvailableRelicsOfRarity[rarity] = new List<CustomRelic>();
            return AvailableRelicsOfRarity[rarity];
        }

        public void AddToAvailablePool(CustomRelic relic)
        {
            RelicRarity rarity = relic.globalRarity;
            if (!AvailableRelicsOfRarity.ContainsKey(rarity)) AvailableRelicsOfRarity[rarity] = new List<CustomRelic>();
            AvailableRelicsOfRarity[rarity].Add(relic);
        }

        public void RemoveFromAvailablityPools(CustomRelic relic)
        {
            foreach (List<CustomRelic> pool in AvailableRelicsOfRarity.Values)
            {
                pool.Remove(relic);
            }
        }

        public void SetupInternalRelicPools()
        {

            AvailableRelicsOfRarity.Clear();

            foreach (CustomRelic relic in CustomRelic.AllCustomRelics)
            {
                if (relic.IsEnabled)
                {
                    AddToAvailablePool(relic);
                }
            }
        }

        public void AddDamageModifier(IDamageModifier modifier)
        {
            _damageModifiers.Add(modifier);
        }

        public void RemoveDamageModifier(IDamageModifier modifier)
        {
            _damageModifiers.Remove(modifier);
        }

        public void Reset()
        {
            RelicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
            foreach (CustomRelic relic in OwnedRelics)
            {
                relic.OnRelicRemoved(RelicManager);
            }
            RelicUsesPerBattleCounts.Clear();
            RelicRemainingUsesPerBattle.Clear();
            OrderOfRelicsObtained.Clear();
            OwnedRelics.Clear();
            SetupInternalRelicPools();
            RelicManager.OnRelicsReset?.Invoke(null);
        }

        public bool RelicActive(string id)
        {
            if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
            {
                return RelicActive(relic);
            }
            return false;
        }

        public bool RelicActive(CustomRelic relic)
        {
            return OwnedRelics.Contains(relic);
        }

        public bool AttemptUseRelic(string id)
        {
            if (CustomRelic.TryGetCustomRelic(id, out CustomRelic relic))
            {
                return AttemptUseRelic(relic);
            }
            return false;
        }

        public bool AttemptUseRelic(CustomRelic relic)
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

        public void RemoveRelic(CustomRelic relic)
        {
            if (OwnedRelics.Contains(relic))
            {
                OwnedRelics.Remove(relic);
                RelicManager.OnRelicRemoved(relic);
            }
        }

        public void OnEnable()
        {
            RelicManager.OnRelicAdded += OnRelicAddedHandler;
            RelicManager.OnRelicRemoved += OnRelicRemovedHandler;
            RelicManager.OnRelicUsed += OnRelicUsedHandler;
            RelicManager.OnRelicEnabled += OnRelicEnabledHandler;
            RelicManager.OnRelicDisabled += OnRelicDisabledHandler;
            RelicManager.OnCountdownDecremented += OnCountdownDecrementedHandler;
        }

        public void OnDisable()
        {
            RelicManager.OnRelicAdded -= OnRelicAddedHandler;
            RelicManager.OnRelicRemoved -= OnRelicRemovedHandler;
            RelicManager.OnRelicUsed -= OnRelicUsedHandler;
            RelicManager.OnRelicEnabled -= OnRelicEnabledHandler;
            RelicManager.OnRelicDisabled -= OnRelicDisabledHandler;
            RelicManager.OnCountdownDecremented -= OnCountdownDecrementedHandler;
        }

        private void OnRelicAddedHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicAdded(relicManager);
            }
        }

        private void OnRelicRemovedHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicRemoved(relicManager);
            }
        }

        private void OnRelicUsedHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicUsed(relicManager);
            }
        }

        private void OnRelicEnabledHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicEnabled(relicManager);
            }
        }

        private void OnRelicDisabledHandler(Relic relic)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnRelicDisabled(relicManager);
            }
        }

        private void OnCountdownDecrementedHandler(Relic relic, int remainingCountdown)
        {
            if (relic is CustomRelic customRelic)
            {
                RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
                customRelic.OnCountdownDecremented(relicManager, remainingCountdown);
            }
        }

        private static float GetDamageModifier(Attack attack, int critCount, float damage)
        {
            float start = damage;
            foreach (CustomRelic relic in Instance.OwnedRelics)
            {
                damage += relic.DamageModifier(attack, critCount);
            }

            foreach (IDamageModifier modifier in Instance._damageModifiers)
            {
                float additional = modifier.GetDamageModifier(attack, Instance.RelicManager, critCount, damage);
                damage += additional;
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
                // Checking where num1 and num2 are first set.
                for (int i = 0; i < code.Count - 1; i++)
                {
                    if (code[i].opcode == OpCodes.Ldc_R4 && code[i + 1].opcode == OpCodes.Stloc_2)
                    {
                        insertionIndex = i + 2;
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

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRelicManager), nameof(GetDamageModifier)))); // Call Method CustomRelicManager::CritModifier
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
                    if (customRelic == __instance.consolationPrize)
                    {
                        TrophyStack(__instance, customRelic);
                        return;
                    }
                    Instance.UnlockedRelics.Add(customRelic.Id);

                    if (!Instance.OwnedRelics.Contains(customRelic))
                    {
                        if (Instance.RelicCountdownValues.ContainsKey(customRelic))
                        {
                            Instance.RelicRemainingCountdowns[customRelic] = Instance.RelicCountdownValues[customRelic];
                        }
                        if (Instance.RelicUsesPerBattleCounts.ContainsKey(customRelic))
                        {
                            Instance.RelicRemainingUsesPerBattle[customRelic] = Instance.RelicUsesPerBattleCounts[customRelic];
                        }
                        Instance.OwnedRelics.Add(customRelic);
                        Instance.OrderOfRelicsObtained.Add(customRelic, __instance._orderCounter);
                        __instance._orderCounter++;

                        __instance._availableCommonRelics.Remove(relic);
                        __instance._availableRareRelics.Remove(relic);
                        __instance._availableBossRelics.Remove(relic);
                    }
                }
            }

            private static void TrophyStack(RelicManager relicManager, CustomRelic trophy)
            {
                int stack = Instance.RelicRemainingCountdowns.ContainsKey(trophy) ? Instance.RelicRemainingCountdowns[trophy] + 1 : 1;
                Instance.RelicRemainingCountdowns[trophy] = stack;

                if (stack == 1)
                {
                    Instance.OwnedRelics.Add(trophy);
                    Instance.OrderOfRelicsObtained.Add(trophy, relicManager._orderCounter);
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
                foreach (KeyValuePair<CustomRelic, int> key in Instance.RelicUsesPerBattleCounts)
                {
                    Instance.RelicRemainingUsesPerBattle[key.Key] = key.Value;
                    if (Instance.OwnedRelics.Contains(key.Key))
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
                Instance.Reset();
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.ArmBallForShot))]
        private static class ArmBallForShot
        {
            [HarmonyPriority(Priority.Low)]
            private static void Prefix(BattleController __instance)
            {
                foreach (CustomRelic relic in Instance.OwnedRelics)
                {
                    relic.OnArmBallForShot(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(TargetingManager), nameof(TargetingManager.HandlePegHit))]
        private static class HandlePegHit
        {
            private static void Prefix(TargetingManager __instance)
            {
                List<CustomRelic> relics = new List<CustomRelic>(Instance.OwnedRelics); // We are doing this just in case the list is modified while iterating through

                relics.ForEach(relic => relic.HandlePegHit(__instance._relicManager));
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.RemoveRelic))]
        private static class RemoveRelicIcon
        {
            private static bool Prefix(Relic toRemove)
            {
                if (toRemove is CustomRelic customRelic)
                {
                    RelicIcon relicIcon = Instance.RelicIcons[customRelic];
                    if (relicIcon != null)
                    {
                        relicIcon.Remove();
                    }
                    Instance.RelicIcons.Remove(customRelic);
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
                foreach (RelicIcon relicIcon in Instance.RelicIcons.Values)
                {
                    if (relicIcon != null)
                        UnityEngine.Object.Destroy(relicIcon.gameObject);
                }
                Instance.RelicIcons.Clear();
            }
        }

        [HarmonyPatch(typeof(RelicUI), nameof(RelicUI.AddRelic))]
        private static class AddRelicIcon
        {
            private static bool Prefix(RelicUI __instance, Relic toAdd)
            {
                if (toAdd is CustomRelic customRelic)
                {

                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.relicIconPrefab);
                    RelicIcon component = gameObject.GetComponent<RelicIcon>();

                    if (customRelic.CustomRelicIconType != typeof(RelicIcon))
                    {
                        if (component != null)
                            GameObject.DestroyImmediate(gameObject.GetComponent<RelicIcon>());
                        component = (RelicIcon)gameObject.AddComponent(customRelic.CustomRelicIconType);
                    }

                    if (component == null)
                    {
                        Plugin.Log.LogWarning($"Failed to generate RelicIcon for {customRelic.Id}");
                        return false;
                    }

                    component.SetRelic(toAdd);
                    Instance.RelicIcons[customRelic] = component;
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
                if (relic is CustomRelic customRelic)
                {
                    if (Instance.RelicIcons.ContainsKey(customRelic))
                    {
                        Instance.RelicIcons[customRelic].Flash();
                        if (Instance.RelicCountdownValues.ContainsKey(customRelic))
                        {
                            Instance.RelicIcons[customRelic].ResetCountdownText();
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
                if (relic is CustomRelic customRelic)
                {
                    if (Instance.RelicIcons.ContainsKey(customRelic) && Instance.RelicIcons[customRelic].grayScaleActive)
                    {
                        Instance.RelicIcons[customRelic].RemoveGrayscale();
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
                    if (Instance.RelicIcons.ContainsKey(customRelic) && !Instance.RelicIcons[customRelic].grayScaleActive)
                    {
                        Instance.RelicIcons[customRelic].ApplyGrayscale();
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
                if (relic is CustomRelic customRelic)
                {
                    Instance.RelicIcons[customRelic].SetCountdownText(countdown.ToString());
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
                if (r is CustomRelic customRelic)
                {
                    __instance.relic = r;
                    __instance._image.sprite = r.sprite;
                    if (__instance._countdownText == null)
                    {
                        return false;
                    }
                    if (Instance.RelicCountdownValues.ContainsKey(customRelic))
                    {
                        __instance.SetCountdownText(Instance.RelicCountdownValues[customRelic].ToString());
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
                if (__instance.relic is CustomRelic customRelic)
                {
                    __instance.SetCountdownText(Instance.RelicCountdownValues[customRelic].ToString());
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
                if (Instance.OwnedRelics == null)
                {
                    return;
                }
                List<CustomRelicManagerSaveData.CustomRelicSaveObject> list = new List<CustomRelicManagerSaveData.CustomRelicSaveObject>();
                foreach (CustomRelic relic in Instance.OwnedRelics)
                {
                    CustomRelicManagerSaveData.CustomRelicSaveObject item = new CustomRelicManagerSaveData.CustomRelicSaveObject(relic.Id, Instance.OrderOfRelicsObtained[relic]);
                    list.Add(item);
                }
                List<CustomRelicManagerSaveData.CustomRelicCountdownStatus> list2 = CreateRelicCountdownsForSave();
                CustomRelicManagerSaveData.CustomRelicPoolSaveObject relicPoolSaveObject = GetRelicPoolSaveObject(Instance.GetAvailableRelicsOfRarity(RelicRarity.COMMON));
                CustomRelicManagerSaveData.CustomRelicPoolSaveObject relicPoolSaveObject2 = GetRelicPoolSaveObject(Instance.GetAvailableRelicsOfRarity(RelicRarity.RARE));
                CustomRelicManagerSaveData.CustomRelicPoolSaveObject relicPoolSaveObject3 = GetRelicPoolSaveObject(Instance.GetAvailableRelicsOfRarity(RelicRarity.BOSS));
                new CustomRelicManagerSaveData(list.ToArray(), list2.ToArray(), relicPoolSaveObject, relicPoolSaveObject2, relicPoolSaveObject3).Save();

            }

            private static List<CustomRelicManagerSaveData.CustomRelicCountdownStatus> CreateRelicCountdownsForSave()
            {
                List<CustomRelicManagerSaveData.CustomRelicCountdownStatus> list = new List<CustomRelicManagerSaveData.CustomRelicCountdownStatus>();
                foreach (KeyValuePair<CustomRelic, int> pair in Instance.RelicRemainingCountdowns)
                {
                    CustomRelicManagerSaveData.CustomRelicCountdownStatus item = new CustomRelicManagerSaveData.CustomRelicCountdownStatus(pair.Key.Id, pair.Value);
                    list.Add(item);
                }
                return list;
            }

            private static CustomRelicManagerSaveData.CustomRelicPoolSaveObject GetRelicPoolSaveObject(List<CustomRelic> pool)
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

                __instance._availableCommonRelics.Clear();
                __instance._availableRareRelics.Clear();
                __instance._availableBossRelics.Clear();

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
                        if (relic2 is CustomRelic) continue;

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
                                int insert = Math.Min(list[i].OrderOfAcquisition, relics.Count - 1);
                                relics.Insert(insert, relic);
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

            if (Instance.OwnedRelics == null)
            {
                Instance.Reset();
            }

            if (relic is CustomRelic customRelic)
            {
                Instance.OwnedRelics.Add(customRelic);
                Instance.OrderOfRelicsObtained.Add(customRelic, relicManager._orderCounter);
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
                    if (Instance.RelicCountdownValues.ContainsKey(relic))
                    {
                        Instance.RelicRemainingCountdowns[relic] = Instance.RelicCountdownValues[relic];
                    }
                    Instance.RelicRemainingCountdowns[relic] = relicCountdownStatus.RelicCountdown;
                    if (Instance.OwnedRelics.Contains(relic))
                    {
                        RelicManager.OnCountdownDecremented?.Invoke(relic, relicCountdownStatus.RelicCountdown);
                    }
                }

            }
        }
    }
}
