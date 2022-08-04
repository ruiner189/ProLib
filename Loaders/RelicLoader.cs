using ProLib.Relics;
using Relics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ProLib.Loaders
{
    public class RelicLoader : MonoBehaviour
    {
        private static bool _relicsRegistered = false;
        public delegate void RelicRegister(RelicLoader loader);
        public static RelicRegister Register = delegate (RelicLoader loader) { };
        public static RelicLoader Instance;

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (this != Instance) Destroy(this);
        }

        public void Start()
        {
            if (!_relicsRegistered)
            {
                RegisterCustomRelics();
            }
            StartCoroutine(DelayedStart());
        }

        public IEnumerator DelayedStart()
        {
            yield return new WaitForEndOfFrame();
            AddRelicsToPools();
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

            if(globalRelics != null)
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
    }
}
