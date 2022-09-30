using ProLib.Relics;
using Relics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace ProLib.Loaders
{
    public class RelicLoader : MonoBehaviour
    {
        private static bool _relicsRegistered = false;
        public delegate void RelicRegister(RelicLoader loader);
        public static RelicRegister Register = delegate (RelicLoader loader) { };
        public static RelicLoader Instance;

        public RelicManager relicManager;

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
            while(relicManager == null && attempts > 0){
                yield return new WaitForEndOfFrame();
                attempts--;
                relicManager = Resources.FindObjectsOfTypeAll<RelicManager>().FirstOrDefault();
            }

            if(relicManager != null)
            {
                if (!_relicsRegistered)
                {
                    RegisterCustomRelics();
                }
                AddRelicsToPools();

            } else
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
