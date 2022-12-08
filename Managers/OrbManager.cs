using Battle.Attacks;
using HarmonyLib;
using PeglinUI.LoadoutManager;
using ProLib.Extensions;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static PachinkoBall;
using Random = UnityEngine.Random;
using Priority = ProLib.Components.Priority;
using Data.Deck;
using MoreMountains.Tools;
using System.Reflection;
using ProLib.Orbs;

namespace ProLib.Managers
{
    public class OrbManager : MonoBehaviour
    {
        private OrbPool _allOrbs;
        private Dictionary<OrbRarity, OrbPool> _pools = new Dictionary<OrbRarity, OrbPool>();
        private Dictionary<OrbRarity, List<GameObject>> _deckManagerPools = new Dictionary<OrbRarity, List<GameObject>>();
        private HashSet<Assembly> _assembliesToRegister = new HashSet<Assembly>();

        public delegate void OrbRegister(OrbManager loader);
        public static OrbRegister Register = delegate (OrbManager loader) { };
        public static OrbManager Instance;
        public GameObject OrbPrefab;
        public GameObject ShotPrefab;

        private DeckManager _deckManager;

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (this != Instance) Destroy(this);
        }

        public void Start()
        {
            GetBlankPrefab();
            StartCoroutine(LateStart());
        }

        // We are delaying to make sure that this gets after the orb pool is made
        private IEnumerator LateStart()
        {
            yield return new WaitForSeconds(1.0f);
            int attempts = 10;
            while (_deckManager == null && attempts > 0)
            {
                attempts--;
                _deckManager = Resources.FindObjectsOfTypeAll<DeckManager>().FirstOrDefault();
                yield return new WaitForEndOfFrame();
            }

            if (_deckManager != null)
            {
                RegisterOrbs();
            }
            else
            {
                Plugin.Log.LogWarning("Failed to find DeckManager. Orbs failed to register.");
            }
        }

        private void GetBlankPrefab()
        {
            OrbPrefab = GameObject.Instantiate(Resources.Load<GameObject>("$Prefabs/Orbs/StoneOrb-Lvl1"));
            OrbPrefab.transform.SetParent(Plugin.PrefabHolder.transform);
            OrbPrefab.HideAndDontSave();

            ShotPrefab = GameObject.Instantiate(OrbPrefab.GetComponent<ProjectileAttack>()._shotPrefab);
            ShotPrefab.transform.SetParent(Plugin.PrefabHolder.transform);
            ShotPrefab.HideAndDontSave();
        }

        private void RegisterOrbs()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            GetOrbPools();

            _orbsToRegister = new List<GameObject>();
            Register?.Invoke(this);
            RegisterAssemblies();

            _allOrbs.AvailableOrbs = _allOrbs.AvailableOrbs.Union(_orbsToRegister).ToArray();

            Dictionary<OrbRarity, List<GameObject>> orbRarities = new Dictionary<OrbRarity, List<GameObject>>();
            foreach (GameObject gameObject in _orbsToRegister)
            {
                PachinkoBall ball = gameObject.GetComponent<PachinkoBall>();
                if (ball != null && _pools.ContainsKey(ball.orbRarity))
                {
                    if (!orbRarities.ContainsKey(ball.orbRarity)) orbRarities[ball.orbRarity] = new List<GameObject>();
                    orbRarities[ball.orbRarity].Add(gameObject);
                }
            }

            foreach (KeyValuePair<OrbRarity, List<GameObject>> pair in orbRarities)
            {
                _pools[pair.Key].AvailableOrbs = _pools[pair.Key].AvailableOrbs.Union(pair.Value).ToArray();
                _deckManagerPools[pair.Key]?.AddRange(pair.Value);
            }

            stopWatch.Stop();
            Plugin.Log.LogInfo($"Orbs Registered! Took {stopWatch.ElapsedMilliseconds}ms");
            _orbsToRegister = null;
        }

        private void RegisterAssemblies()
        {
            foreach (Assembly assembly in _assembliesToRegister)
            {
                IEnumerable<Type> types = assembly.GetTypes().Where(type => typeof(ModifiedOrb).IsAssignableFrom(type));
                foreach (Type type in types)
                {
                    MethodInfo register = type.GetMethod("Register", AccessTools.all);
                    if (register != null && register.GetParameters().Length == 0 && register.IsStatic)
                    {
                        register.Invoke(null, new object[] { });
                    }
                }
            }
        }

        private void GetOrbPools()
        {
            foreach (OrbPool pool in Resources.FindObjectsOfTypeAll<OrbPool>())
            {
                if (pool.name == "AvailableOrbs")
                {
                    _allOrbs = pool;
                }
                else if (pool.name == "PeglinCommonOrbPool")
                {
                    AddOrbPool(OrbRarity.COMMON, pool, _deckManager.CommonOrbPool);
                }
                else if (pool.name == "PeglinUncommonOrbPool")
                {
                    AddOrbPool(OrbRarity.UNCOMMON, pool, _deckManager.UncommonOrbPool);
                }
                else if (pool.name == "PeglinRareOrbPool")
                {
                    AddOrbPool(OrbRarity.RARE, pool, _deckManager.RareOrbPool);
                }
                else if (pool.name == "PeglinScenarioOrbPool")
                {
                    AddOrbPool(OrbRarity.SPECIAL, pool, _deckManager.SpecialOrbPool);
                }
            }

            if (_allOrbs == null)
            {
                Plugin.Log.LogWarning("Could not find orb pool to inject custom orbs");
                return;
            }
        }

        private List<GameObject> _orbsToRegister;

        /// <summary>Searches the current assembly for the static method Register() with classes that extend ModifiedOrb</summary>
        /// <remarks>This method can fail to use the correct assembly when being inlined. It calls StackTrace.GetFrame(1) which can point to the wrong method/assembly. If you are unsure or run into problems, use <code>RegisterAll(Assembly.GetExecutingAssembly())</code> instead.</remarks>
        public void RegisterAll()
        {
            MethodBase method = new StackTrace().GetFrame(1).GetMethod();
            Assembly assembly = method.ReflectedType.Assembly;
            RegisterAll(assembly);
        }

        public void RegisterAll(Assembly assembly)
        {
            _assembliesToRegister.Add(assembly);
        }

        public void AddOrbToPool(GameObject orb)
        {
            if (_orbsToRegister != null)
            {
                _orbsToRegister.Add(orb);
            }
            else
            {
                Plugin.Log.LogError("Orb injection is being illegally accessed");
            }
        }

        public void AddOrbPool(OrbRarity rarity, OrbPool pool, List<GameObject> list)
        {
            _pools[rarity] = pool;
            _deckManagerPools[rarity] = list;
        }

        public static int GetPriority(GameObject orb)
        {
            Priority priority = orb.GetComponent<Priority>();
            return priority ? priority.Weight : Priority.NORMAL;
        }

        #region Harmony Patches

        [HarmonyPatch(typeof(PersistentPlayerData), nameof(PersistentPlayerData.InitFromSaveFile))]
        [HarmonyPostfix]
        private static void PatchInitSave(ref PersistentPlayerData __result)
        {
            if (Plugin.AllItemsUnlocked)
            {
                OrbPool[] pools = Resources.FindObjectsOfTypeAll<OrbPool>();
                HashSet<String> set = new HashSet<String>(__result.UnlockedOrbs);
                foreach (OrbPool pool in pools)
                {
                    foreach (GameObject obj in pool.AvailableOrbs)
                    {
                        if (obj == null) continue;
                        Attack attack = obj.GetComponent<Attack>();
                        if (attack != null)
                            set.Add(attack.locNameString);
                    }
                }
                if (__result.UnlockedOrbs != null)
                {
                    __result.UnlockedOrbs.Clear();
                    __result.UnlockedOrbs.AddRange(set);
                }
            }
        }

        [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.ShuffleCompleteDeck))]
        [HarmonyPrefix]
        private static bool PatchShuffleDeck(DeckManager __instance, bool fromComplete)
        {
            GameObject[] currentDeck = fromComplete ? DeckManager.completeDeck.ToArray() : __instance.battleDeck.ToArray();
            GameObject[] shuffledDeck = currentDeck
                .OrderBy(orb => Random.Range(0f, 1f))
                .OrderBy(orb => -GetPriority(orb)).ToArray();

            __instance.shuffledDeck.Clear();
            if (fromComplete)
            {
                if (__instance.battleDeck == null)
                {
                    __instance.battleDeck = new List<GameObject>();
                }
                if (__instance.battleDeckOrbContainerInstance == null)
                {
                    __instance.battleDeckOrbContainerInstance = UnityEngine.Object.Instantiate<BattleDeckOrbContainer>(__instance.battleDeckOrbContainerPrefab);
                }
                Transform transform = __instance.battleDeckOrbContainerInstance.transform;
                transform.MMDestroyAllChildren();
                __instance.battleDeck.Clear();
                foreach (GameObject orb in shuffledDeck)
                {
                    GameObject newOrb = UnityEngine.Object.Instantiate<GameObject>(orb, transform);
                    __instance.battleDeck.Add(newOrb);
                    __instance.PushOrbToShuffleDeck(newOrb, fromComplete);
                }
            }
            else
            {
                foreach (GameObject orb in shuffledDeck)
                {
                    __instance.PushOrbToShuffleDeck(orb, fromComplete);
                }
            }
            DeckManager.onDeckShuffled(__instance.shuffledDeck.Count);
            return false;
        }
    }

    #endregion
}
