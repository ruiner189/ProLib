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

namespace ProLib.Loaders
{
    public class OrbLoader : MonoBehaviour
    {
        private OrbPool _allOrbs;
        public delegate void OrbRegister(OrbLoader loader);
        public static OrbRegister Register = delegate (OrbLoader loader) { };
        public static OrbLoader Instance;
        public GameObject OrbPrefab;
        public GameObject ShotPrefab;

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
            RegisterOrbs();
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

            _allOrbs = Resources.FindObjectsOfTypeAll<OrbPool>().FirstOrDefault();

            if (_allOrbs == null)
            {
                Plugin.Log.LogWarning("Could not find orb pool to inject custom orbs");
                return;
            }

            _orbsToRegister = new List<GameObject>();

            Register(this);

            _allOrbs.AvailableOrbs = _allOrbs.AvailableOrbs.Union(_orbsToRegister).ToArray();

            stopWatch.Stop();
            Plugin.Log.LogInfo($"Orbs Registered! Took {stopWatch.ElapsedMilliseconds}ms");
            _orbsToRegister = null;
        }

        private List<GameObject> _orbsToRegister;
        public void AddOrbToPool(GameObject orb)
        {
            if(_orbsToRegister != null)
            {
                _orbsToRegister.Add(orb);
            } else {
                Plugin.Log.LogError("Orb injection is being illegally accessed");
            }
        }

        [HarmonyPatch(typeof(PersistentPlayerData), nameof(PersistentPlayerData.InitFromSaveFile))]
        public static class UnlockOrbs
        {
            public static void Postfix(ref PersistentPlayerData __result)
            {
                if (Plugin.AllItemsUnlocked)
                {
                    OrbPool[] pools = Resources.FindObjectsOfTypeAll<OrbPool>();
                    foreach (OrbPool pool in pools)
                    {
                        foreach (GameObject obj in pool.AvailableOrbs)
                        {
                            Attack attack = obj.GetComponent<Attack>();
                            HashSet<String> set = new HashSet<String>(__result.UnlockedOrbs);
                            if (attack != null)
                                set.Add(attack.locNameString);
                            __result.UnlockedOrbs = set.ToList();
                        }
                    }

                }
            }
        }
    }
}
