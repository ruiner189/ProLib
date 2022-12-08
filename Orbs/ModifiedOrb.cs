using Battle.Attacks;
using Cruciball;
using HarmonyLib;
using I2.Loc;
using PeglinUI;
using Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleController;

namespace ProLib.Orbs
{
    [HarmonyPatch]
    public abstract class ModifiedOrb
    {
        public static readonly List<ModifiedOrb> AllModifiedOrbs = new List<ModifiedOrb>();

        private readonly String _name;
        public bool LocalVariables = false;

        /// <summary>
        /// Modifies an orb already in the game. Can modifiy both vanilla and modded orbs.
        /// For <paramref name="name"/> see <see cref="OrbNames"/>
        /// </summary>
        public ModifiedOrb(String name)
        {
            _name = name;
            AllModifiedOrbs.Add(this);
        }

        /// <summary>
        /// Whether this modification should take affect.
        /// </summary>
        /// <returns> Modification active </returns>
        public abstract bool IsEnabled();

        /// <summary>
        /// Gets all modifications for an orb.
        /// </summary>
        /// <param name="name"></param> The name of the orb being modified
        /// <param name="includeAll"></param> Include disabled modifications
        /// <returns>A list of all the modifications</returns>
        public static List<ModifiedOrb> GetOrbs(String name, bool includeAll = false)
        {
            if (includeAll)
                return AllModifiedOrbs.FindAll(orb => orb.GetName() == name);
            else
                return AllModifiedOrbs.FindAll(orb => orb.GetName() == name && orb.IsEnabled());
        }

        /// <summary>
        /// Gets the name of the orb
        /// </summary>
        /// <returns>The name of the orb</returns>
        public String GetName()
        {
            return _name;
        }

        public virtual void SetLocalVariables(LocalizationParamsManager localParams, GameObject orb, Attack attack) { }
        public virtual void OnDiscard(RelicManager relicManager, BattleController battleController, GameObject orb, Attack attack) { }
        public virtual void OnAddedToDeck(DeckManager deckManager, CruciballManager cruciballManager, GameObject orb, Attack attack) { }
        public virtual void OnRemovedFromBattleDeck(DeckManager deckManager, CruciballManager cruciballManager, GameObject orb, Attack attack) { }
        public virtual void OnRemovedFromDeck(DeckManager deckManager, GameObject orb, Attack attack) { }
        public virtual void OnDeckShuffle(BattleController battleController, GameObject orb, Attack attack) { }
        public virtual void OnEnemyTurnEnd(BattleController battleController, GameObject orb, Attack attack) { }
        public virtual void OnBattleStart(BattleController battleController, GameObject orb, Attack attack) { }
        public virtual void ShotWhileInHolster(RelicManager relicManager, BattleController battleController, GameObject attackingOrb, GameObject heldOrb) { }
        public virtual void OnDrawnFromDeck(BattleController battleController, GameObject orb, Attack attack) { }
        public virtual void OnShotFired(BattleController battleController, GameObject orb, Attack attack) { }
        public virtual void ChangeDescription(Attack attack, RelicManager relicManager) { }
        public virtual int GetAttackValue(CruciballManager cruciballManager, Attack attack)
        {
            return int.MinValue;
        }

        public virtual int GetCritValue(CruciballManager cruciballManager, Attack attack)
        {
            return int.MinValue;
        }

        protected static void AddToDescription(Attack attack, String desc, int position = -1)
        {
            if (attack.locDescStrings == null || attack.locDescStrings.Length == 0) return;
            if (position == -1) position = attack.locDescStrings.Length;
            bool containsDesc = false;
            foreach (String s in attack.locDescStrings)
            {
                if (s == desc)
                {
                    containsDesc = true;
                    break;
                }
            }
            if (!containsDesc)
            {
                String[] newDesc = new String[attack.locDescStrings.Length + 1];
                int i = 0;
                for (int j = 0; j < newDesc.Length; j++)
                {
                    if (j == position)
                        newDesc[j] = desc;
                    else
                    {
                        newDesc[j] = attack.locDescStrings[i];
                        i++;
                    }

                }

                attack.locDescStrings = newDesc;
            }
        }

        protected static void ReplaceDescription(Attack attack, String desc, int position)
        {
            if (attack.locDescStrings.Length > position)
                attack.locDescStrings[position] = desc;
            else
                AddToDescription(attack, desc);
        }

        protected static void ReplaceDescription(Attack attack, String[] desc)
        {
            attack.locDescStrings = desc;
        }

        #region Harmony Patches

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.ShotFired))]
        [HarmonyPostfix]
        private static void PatchShotFired(BattleController __instance)
        {
            if (BattleController._battleState == BattleState.NAVIGATION) return;
            Attack attack = __instance._activePachinkoBall.GetComponent<Attack>();
            if (attack != null)
            {
                List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                foreach(ModifiedOrb orb in orbs)
                {
                   orb.OnShotFired(__instance, __instance._activePachinkoBall, attack);
                }
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.AttemptOrbDiscard))]
        [HarmonyPrefix]
        private static void PatchAttemptOrbDiscard(BattleController __instance, bool __runOriginal)
        {
            if (BattleController._battleState == BattleState.NAVIGATION || !__runOriginal) return;
            if (__instance._activePachinkoBall != null && __instance._activePachinkoBall.GetComponent<PachinkoBall>().available && !DeckInfoManager.populatingDisplayOrb && !GameBlockingWindow.windowOpen && __instance.NumShotsDiscarded < __instance.MaxDiscardedShots)
            {
                Attack attack = __instance._activePachinkoBall.GetComponent<Attack>();
                if (attack != null)
                {
                    List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                    foreach (ModifiedOrb orb in orbs)
                    {
                        orb.OnDiscard(__instance._relicManager, __instance, __instance._activePachinkoBall, attack);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.Description), MethodType.Getter)]
        [HarmonyPrefix]
        private static bool PatchDescription(Attack __instance)
        {
            if (__instance._relicManager == null) return true;

            List<ModifiedOrb> orbs = GetOrbs(__instance.locNameString);
            foreach (ModifiedOrb orb in orbs)
            {
                orb.ChangeDescription(__instance, __instance._relicManager);
                if (orb.LocalVariables)
                {
                    LocalizationParamsManager localParams = __instance.GetComponent<LocalizationParamsManager>();
                    if (localParams == null) localParams = __instance.gameObject.AddComponent<LocalizationParamsManager>();
                    if (localParams != null)
                        orb.SetLocalVariables(localParams, __instance.gameObject, __instance);
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.SoftInit))]
        [HarmonyPostfix]
        private static void PatchSoftInit(Attack __instance, CruciballManager ____cruciballManager)
        {
            List<ModifiedOrb> orbs = GetOrbs(__instance.locNameString);
            foreach (ModifiedOrb orb in orbs)
            {
                int damage = orb.GetAttackValue(____cruciballManager, __instance);
                if (damage != int.MinValue)
                    __instance.DamagePerPeg = damage;
                int crit = orb.GetCritValue(____cruciballManager, __instance);
                if (crit != int.MinValue)
                    __instance.CritDamagePerPeg = crit;
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.SetId))]
        [HarmonyPostfix]
        private static void PatchFixMirrorOrb(Attack __instance)
        {
            DeckManager deckManager = Resources.FindObjectsOfTypeAll<DeckManager>().FirstOrDefault();
            RelicManager relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
            CruciballManager cruciballManager = Resources.FindObjectsOfTypeAll<CruciballManager>().FirstOrDefault();
            __instance.SoftInit(deckManager, relicManager, cruciballManager);
        }

        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.AddOrbToDeck))]
        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.AddOrbToDeckSilent))]
        [HarmonyPostfix]
        private static void PatchAddOrbToDeck(DeckManager __instance, GameObject orbPrefab)
        {
            Attack attack = orbPrefab.GetComponent<Attack>();
            if (attack != null)
            {
                List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                foreach (ModifiedOrb orb in orbs)
                {
                    orb.OnAddedToDeck(__instance, attack._cruciballManager, orbPrefab, attack);
                }
            }
        }

        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.RemoveOrbFromBattleDeck))]
        [HarmonyPostfix]
        private static void PatchRemoveOrbFromBattleDeck(DeckManager __instance, GameObject orb)
        {
            Attack attack = orb.GetComponent<Attack>();
            if (attack != null)
            {
                List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                foreach (ModifiedOrb modifiedOrb in orbs)
                {
                    modifiedOrb.OnRemovedFromBattleDeck(__instance, attack._cruciballManager, orb, attack);
                }
            }
        }

        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.RemoveSpecifiedOrbFromDeck))]
        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.SoftRemoveSpecifiedOrbFromDeck))]
        [HarmonyPostfix]
        private static void PatchRemoveOrbFromDeck(DeckManager __instance, GameObject orb)
        {
            Attack attack = orb.GetComponent<Attack>();
            if (attack != null)
            {
                List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                foreach (ModifiedOrb modifiedOrb in orbs)
                {
                    modifiedOrb.OnRemovedFromDeck(__instance, orb, attack);
                }
            }
        }

        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.RemoveRandomOrbFromDeck))]
        [HarmonyPrefix]
        private static void PatchPreRemoveRandomOrbFromDeck(ref List<GameObject> __state)
        {
            __state = new List<GameObject>(DeckManager.completeDeck);
        }

        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.RemoveRandomOrbFromDeck))]
        [HarmonyPostfix]
        private static void PatchPostRemoveRandomOrbFromDeck(DeckManager __instance, List<GameObject> __state)
        {
            if (__state == null || DeckManager.completeDeck == null) return;
            foreach (GameObject orb in DeckManager.completeDeck)
            {
                __state.Remove(orb);
            }

            foreach (GameObject orb in __state)
            {
                Attack attack = orb.GetComponent<Attack>();
                if (attack != null)
                {
                    List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                    foreach (ModifiedOrb modifiedOrb in orbs)
                    {
                        modifiedOrb.OnRemovedFromDeck(__instance, orb, attack);

                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.ShuffleDeck))]
        [HarmonyPostfix]
        private static void PatchShuffleDeck(BattleController __instance)
        {

            foreach (GameObject orb in DeckManager.completeDeck)
            {
                Attack attack = orb.GetComponent<Attack>();
                if (attack != null)
                {
                    List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                    foreach (ModifiedOrb modifiedOrb in orbs)
                    {
                        modifiedOrb.OnDeckShuffle(__instance, orb, attack);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.EnemyTurnComplete))]
        [HarmonyPostfix]
        private static void PatchEnemyTurnComplete(BattleController __instance)
        {
            foreach (GameObject orb in DeckManager.completeDeck)
            {
                Attack attack = orb.GetComponent<Attack>();
                if (attack != null)
                {
                    List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                    foreach (ModifiedOrb modifiedOrb in orbs)
                    {
                        modifiedOrb.OnEnemyTurnEnd(__instance, orb, attack);

                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.Start))]
        [HarmonyPostfix]
        public static void PatchBattleStart(BattleController __instance)
        {
            if (DeckManager.completeDeck != null)
            {
                foreach (GameObject orb in DeckManager.completeDeck)
                {
                    Attack attack = orb.GetComponent<Attack>();
                    if (attack != null)
                    {
                        List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                        foreach (ModifiedOrb modifiedOrb in orbs)
                        {
                            modifiedOrb.OnBattleStart(__instance, orb, attack);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleController), nameof(BattleController.DrawBall))]
        [HarmonyPostfix]
        public static void PatchDrawBall(BattleController __instance)
        {
            Attack attack = __instance._activePachinkoBall.GetComponent<Attack>();
            if (attack != null)
            {
                List<ModifiedOrb> orbs = GetOrbs(attack.locNameString);
                foreach (ModifiedOrb orb in orbs)
                {
                    orb.OnDrawnFromDeck(__instance, __instance._activePachinkoBall, attack);
                }
            }
        }
        #endregion
    }
}
