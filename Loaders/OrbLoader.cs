﻿using Battle.Attacks;
using HarmonyLib;
using PeglinUI.LoadoutManager;
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

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (this != Instance) Destroy(this);
        }

        public void Start()
        {
            StartCoroutine(LateStart());
        }

        // We are delaying to make sure that this gets after the orb pool is made
        private IEnumerator LateStart()
        {
            yield return new WaitForSeconds(1.0f);
            RegisterOrbs();
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
                OrbPool pool = Resources.FindObjectsOfTypeAll<OrbPool>().FirstOrDefault();
                if (pool != null)
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
